# AGENTS.md This file provides guidance to CodeBuddy when working with code in this repository.

## Project

`SchoolEndORM` is a hand-written ORM framework for .NET 10 / C# 14 (a graduation project, see `README.md`). The repo root `global.json` pins the SDK, and `Directory.Build.props` declares the common target framework and language version once for all three projects. Solution `SchoolEndORM.sln` contains three projects:

| Project | Type | Purpose |
| --- | --- | --- |
| `ORMFramework` | library, `net10.0` | the ORM itself (log4net 3.x + `Microsoft.Extensions.DependencyInjection` 10.x) |
| `ORMFramework.Examples` | console exe, `net10.0` | runnable demo against MySQL (`MySql.Data` 9.x), entity classes + `Config.xml` |
| `ORMFramework.Test` | MSTest 4.x, `net10.0` | unit tests for parser / SQL generation / config / object evaluation |

## Runtime and SDK baseline

- Target framework `net10.0`, language version **C# 14** — declared once in `Directory.Build.props`; `Nullable=enable`, `ImplicitUsings=enable`, `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true` and `GenerateDocumentationFile=false` stay unchanged.
- `global.json` pins SDK `10.0.100` (10.0.1xx feature band) with `rollForward: latestFeature` and `allowPrerelease: false`; it resolves to the installed .NET 10 SDK (here `10.0.403`). `dotnet --version` from the repo root must print `10.x`. Do not pin an exact patch, and do not use `disable` / `latest` / `preview`.
- **Minimum runtime: .NET 10 (BREAKING).** Downstream consumers with only the .NET 8 runtime must upgrade; there is no `net8.0` compatibility target.
- Aligned dependency major lines: `Microsoft.Extensions.DependencyInjection` 10.x, `Microsoft.NET.Test.Sdk` 18.x, `MSTest.TestAdapter` / `MSTest.TestFramework` 4.x, `coverlet.collector` 6.x, `MySql.Data` 9.x, `log4net` 3.x. `dotnet restore SchoolEndORM.sln` must be free of NU1701 (framework asset fallback) and NU190x (known vulnerability), and every package must provide `net10.0` compatible assets.
- Test host: VSTest-compatible `dotnet test` through the explicit `Microsoft.NET.Test.Sdk` reference (no `MSTest.Sdk`, no MTP-native project file). `MSTestAnalysisMode` stays at its default — never `Recommended`.

## Commands

```bash
dotnet restore SchoolEndORM.sln
dotnet build SchoolEndORM.sln                      # all three projects
dotnet build ORMFramework/ORMFramework.csproj      # library only
dotnet build SchoolEndORM.sln -warnaserror           # static gate: zero warnings / zero errors
dotnet format --verify-no-changes SchoolEndORM.sln   # formatting gate

dotnet test ORMFramework.Test/ORMFramework.Test.csproj
dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~StatementAnalystTest
dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter TestGetSession   # single test method
dotnet test ORMFramework.Test/ORMFramework.Test.csproj --collect:"XPlat Code Coverage"

dotnet run --project ORMFramework.Examples          # needs a reachable MySQL, waits on Console.ReadKey()
dotnet publish ORMFramework.Examples -c Release -o dist   # `dist/` in repo is such an output; git-ignored
```

There is no dedicated linter; `dotnet build` warnings (from the .NET analyzers enabled in `Directory.Build.props`) are the static feedback. `dotnet format` uses the repo's `.editorconfig`, but avoid mass reformatting. The four fixed verification gates for every commit are `dotnet restore SchoolEndORM.sln`, `dotnet format --verify-no-changes SchoolEndORM.sln`, `dotnet build SchoolEndORM.sln -warnaserror`, and `dotnet test ORMFramework.Test/ORMFramework.Test.csproj`; the three `dotnet test` commands above (plain, `--filter FullyQualifiedName~StatementAnalystTest`, and `--collect:"XPlat Code Coverage"`) must keep working verbatim.

Caveats before running things:
- `ORMFramework.Examples/Program.cs` loads `Config.xml` from `Path.Combine(AppContext.BaseDirectory, "Config.xml")`; the csproj copies `Config.xml` to the output directory (`CopyToOutputDirectory=PreserveNewest`).
- `Config.xml` is committed with a live-looking MySQL connection string; `dotnet run` will try to reach that server.

## Architecture

### Layered flow

```
ISessionFactory (config + wiring)
    └─ ISession               facade; converts calls into events dispatched to listeners
         ├─ IPersistenceContext   connection + mappings + ISQLGenerator holder
         ├─ ISessionCache         per-session first level cache = unit of work
         │      └─ GlobalCache    static process-wide second level cache + locking
         ├─ ISQLGenerator         object/expression -> SQL text
         └─ IDbDriverFactory      ADO.NET provider abstraction
```

`Session` never talks to the database directly for entity work. `Search/Insert/Update/Delete/Submit` build an event object (`SelectEvent`, `UpdateEvent`, `ActionEvent`, `CommandEvent` in `Event/`) carrying the session cache, and pass it to the corresponding listener(s) from `Listener/`. Listeners write their answer back into `@event.Result`. Insert/update/delete/submit listeners are held in `List<...>` on `Session`, so extra listeners can be appended for cross-cutting behaviour; select and command are single. `Session` also exposes raw escape hatches `ExecuteQuery` / `ExecuteNonQuery` handled by `DefaultCommandListener`.

### Two factories (both live)

- `SessionFactory` — manual wiring: builds `PersistenceContext(driverFactory, mappings)` + `new SQLGenerator()` + `new Session(...)`.
- `SessionFactoryIoc` — the one the Examples use. It resolves `ISession` from `Ioc/DefaultComponentProvider` (a thin wrapper over `Microsoft.Extensions.DependencyInjection`) and then *post-configures* the graph: connection string, provider name, mappings, and `SQLGenerator.PersistenceContext`. This post-configuration exists because DI picks `PersistenceContext(IDbDriverFactory, ISQLGenerator)` (the mapping-dictionary ctor is not resolvable), so mappings must be injected afterwards. If you add a constructor or a new dependency to `PersistenceContext`/`Session`, check both factories.

`Ioc/ComponentProviderBuilder` holds the interface→implementation table; `DefaultComponentProvider` holds a separate interface→`ServiceLifetime` table. A key missing from the lifetime dictionary defaults to `Transient`. `ISubmitListener` is in neither table — submit comes from `Session`'s default constructor chain. Sessions/contexts/caches/generators are `Transient`; listeners are `Singleton` (hence listeners must stay stateless — all state travels in the event).

### Query language (three parallel implementations of the same operators)

Queries are C#-like predicate strings, not SQL: `session.Search<Student>("StudentId=='abc'")`, `"Id==1"`, `"a==1 && b like 'x%'"`.

`Statment/StatementAnalyst` lexes the string into `StatElement`s (Constant / Variable / Operator / Result) and produces a `QueryExpression` holding **both** the infix list (`NifixExpression`) and the postfix list (`SuffixExpression`) via an operator-priority table. Consumers differ:

- `SQLGenerator.GetSelectSQL` walks the **infix** list and maps `Operator` → SQL text using `_sqlOperators` to build a `where (...)` clause.
- `Cache/ObjectCalculator.IsTargetObject` evaluates the **postfix** list against an in-memory object using reflection, so cached objects can be filtered without a round trip.

Consequence: adding or changing an operator requires coordinated edits in `Statment/Operator.cs` (enum order matters — it indexes the tables), `StatementAnalyst._operatorChars` / `_priorityTable`, `SQLGenerator._sqlOperators` (index-aligned with the enum), and `ObjectCalculator.ProcessOperator`. Tests `StatementAnalystTest`, `SQLGeneratorTest`, `ObjectCalculatorTest` cover exactly these three sides.

### Caching, transactions and concurrency

`Cache/SessionCache` is the unit of work: `_objList` (object → `CacheEntity`), `_objTable` (Type → entities), plus `_insertList/_updateList/_deleteList`. `Search` first filters locally with `ObjectCalculator`, then delegates to `GlobalCache` passing the ids it already has so they are not returned twice (`ignoreIds`, translated into extra `where`-key exclusions by `GlobalCache.ReadIgnoreKeys`). `Update`/`Delete` acquire a write lock on the global entity immediately; failures call `RollBack()` which restores entity values from `GlobalCache`. `Commit()` opens one ADO.NET transaction and applies delete → update → insert, using `SQLGenerator.GetDeleteSQL/GetUpdateSQL/GetInsertSQL`; `GetUpdateSQL` builds its `where` from the *old* (global-cache) snapshot, which is the optimistic-concurrency check. After the DB commit succeeds, the same changes are pushed into `GlobalCache`.

`Cache/GlobalCache` is a `static` process-wide second level cache (capacity `_cacheMaxCount`, eviction by reference count). It implements a hand-rolled per-entity read/write lock: `GlobalCacheEntity.Lock` + `ReadCount` + `WriteLockThreadId` + a `Queue<Thread> BlockThreads`, driven by `Monitor.Enter/Pulse/Exit` in unbalanced patterns (`ProcessDeletedObject` enters the monitor, callers exit it) and by blocking on `Thread.Sleep(Timeout.Infinite)` + waking via `Thread.Interrupt()`; `ThreadInterruptedException` is used as normal control flow (retry). Values handed out are shallow clones (`CloneObject`, basic-typed properties only) and version stamps are `DateTime.UtcNow.Ticks`, so `SessionCache.RefreshCacheEntity` can detect staleness. **Treat this file as fragile**: enter/exit pairing and interrupt handling are non-obvious; prefer surgical changes and keep the existing convention rather than "fixing" it to `lock`/`ReaderWriterLockSlim` without a broader plan. Multi-threaded behaviour is exercised manually by `Program.TestMultitransaction`, not by unit tests.

### Mapping and lazy loading

`Configuration/ConfigManager` parses `Config.xml` by hand with `XmlTextReader` (element order matters: `SessionFactory` then `Mappings`; unknown elements inside them are hard errors). It produces `Configuration` → `EntityMapping` (`ClassName`, `TableName`, `Keys[]`) with `Relations` keyed by **property name**; `One-to-One`, `One-to-Many`, `Many-to-One` become `EntityRelation`, `Many-to-Many` becomes `ManyToManyRelation` (join table + reference-class columns). The same file also holds the `log4net` section, loaded separately by `LogHelper.InitLog(configFilePath)` (called at the end of `Initialize`).

Relations are resolved in `SessionCache.GetReferenceClasses` from the foreign key values that `GlobalCache` captured while reading rows:
- `*-to-One`: builds a small predicate string and searches session cache then global cache, setting the property directly (eager).
- `*-to-Many`: creates a `LoadList<T>` **through reflection** (`typeof(LoadList<>).MakeGenericType(...)`, using `nameof` for its `ForeignKeyName`/`ForeignKeyValues`/`SessionCache` properties), and assigns it to the property. `LoadList<T>` is a lazy `IList<T>` that materialises on first access by running a generated `fk==v1 || fk==v2` predicate through its own `DefaultSelectListener`. For many-to-many it first queries the join table with raw SQL to collect the far-side keys.

So: `LoadList<T>` must keep its namespace/name, collection navigation properties must be declared as `IList<T>` (see `ORMFramework.Examples/EntityClasses/*.cs`), entities need a public parameterless constructor and public properties (both `CloneObject` and SQL generation reflect over properties), and every mapped class must appear in `Config.xml` under its **full type name**.

### Database provider

`DefaultDbDriverFactory` wraps `DbProviderFactories`: the `<ProviderName>` value from config is an assembly-qualified factory type (`MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data`) which it registers on first use (cached in a static `ConcurrentDictionary`) and then uses to create connections/commands/adapters. Any ADO.NET provider works as long as the referencing project brings the package; the ORM library itself has no DB driver dependency. Generated SQL is plain string concatenation (no parameters) with `'...'` quoting for char/string/`DateTime` (`yyyy-MM-dd HH:mm:ss`), centralised in `ORMFramework/SQL/SqlLiteral.Format`.

## Notes for changes

- Namespaces do not always match folders: `SQLGenerator/` files are in namespace `ORMFramework.SQL`, `Session/`, `SessionFactory/`, `Statment/`(sic) types sit in `ORMFramework` or `ORMFramework.Statment`. Follow the existing namespace of the folder you edit rather than the folder name.
- Existing spelling quirks are load-bearing in config/APIs: `Statment`, `KeyColum`, `ReferenceColum`, `NifixExpression`, `RelationType.MantToMany`. Do not rename them casually — `ConfigManager` matches attribute names literally.
- Tests are MSTest; new tests should keep `[TestClass]`/`[TestMethod]` and stay offline (parser, SQL text, config, object evaluation are all testable without a database).
- Domain exceptions live in `ORMFramework/Exceptions/` (`OrmException` base + `QuerySyntaxException` / `OrmConfigurationException` / `ConcurrencyException`); throw the specific type rather than `new Exception(...)`.
- `Session` and `SessionCache` have no finalizers; their `Dispose()` is idempotent (guarded by a `_disposed` flag).
- The repo also carries a workspace rule `CloudStudioRules` describing deployment of this project to CloudStudio; read it before doing anything deployment-related.
