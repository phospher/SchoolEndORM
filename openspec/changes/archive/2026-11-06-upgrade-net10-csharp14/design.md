# Design

## Context

见 `proposal.md` 的 Why。当前状态与约束（均由只读检查确认，非假设）：

- 三个项目共用 `Directory.Build.props` 中的 `net8.0` / `LangVersion=12.0` / `Nullable=enable` / `EnableNETAnalyzers` / `EnforceCodeStyleInBuild`；三个 `.csproj` 只保留各自特有的输出类型、包引用与项目引用。已存在 `.editorconfig`（Allman、file-scoped namespace、`_camelCase`、无 `this.`）与 `docs/baselines/sql-snapshot.txt` 回归基线。
- 上一变更 `modernize-csharp-syntax` 已把 file-scoped namespace、隐式 using、集合表达式、主构造函数、`FrozenSet`/`SearchValues`、自定义异常、`SqlLiteral` 抽取等 C# 12 现代化做完，并留下 `StatementAnalystTest` / `SQLGeneratorTest` / `ObjectCalculatorTest` / `ConfigManagerTest` / `LoadListTest` / `SessionCacheTest` 的回归网。因此本次**不是**重做一遍 C# 12 改造，而是增量采用 C# 13/14 新特性。
- 三条结构性约束仍然成立：(1) `Operator` 枚举顺序被 `_sqlOperators` 与 `_priorityTable` 按 `(int)` 下标索引，禁止重排；(2) `SessionFactory` 手工装配与 `SessionFactoryIoc` 的 DI + 事后补配并存，任何构造形态变化都要两条路径都验证；(3) `Cache/GlobalCache.cs` 是跨方法不平衡 `Monitor` + `Thread.Interrupt` 的脆弱区，无自动化并发覆盖。
- 已确认的 C# 14 兼容性事实：全仓库（`ORMFramework` / `ORMFramework.Examples` / `ORMFramework.Test`）无名为 `field` 或 `extension` 的标识符，也无 `nameof(field)`、`.Reverse(`、`is not ... or ...` 写法。本机只有 .NET 8 SDK 8.0.133，未安装 .NET 10 SDK。
- 权威依据：[What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)、[C# language versioning](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)（net10.x 默认 C# 14）、[`field` 关键字特性规格](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/field-keyword)（表达式体属性与仅有 getter 的属性均可使用 `field`）、[.NET 10 编译器破坏性变更](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2010)（CS9258/CS9272、Span 重载解析）、[MSTest v3→v4 迁移](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-migration-v3-v4)、[MSTest 分析器规则表](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/usage-rules)。

## Goals / Non-Goals

**Goals:**

- 让 `net10.0` / C# 14 / .NET 10 SDK / 对齐的依赖基线成为可由 `dotnet restore`、`dotnet build -warnaserror`、`dotnet test` 验证的事实。
- 让测试套件符合 MSTest 4 的断言、结构与分析器规范，而不只是"能编译"。
- 在"语义等价 + 可审阅的小规模改写"的边界内，把 C# 14 特性用上，且不引入任何可观察行为变化。
- 让每个提交只承载一类变换，配合既有回归网可 revert、可 `git bisect run dotnet test`。

**Non-Goals（设计层面的边界，proposal 已列范围不再重复）:**

- 不新增与业务语义相关的抽象层或接口；允许一处纯转发的 `extension` 块与测试 helper，但不在 ORM 语义敏感路径为使用特性而制造调用点。
- 不启用 `MSTestAnalysisMode=Recommended`，不把 Info 级分析器规则批量升为 `-warnaserror` 错误。
- 不改动 `Cache/GlobalCache.cs` 的任何语句（含 `Monitor` / `Thread` / `Interrupt`）；本变更对它的唯一可能触碰是"新分析器告警的最小修复或有理由的抑制"。
- 不改变 `docs/baselines/sql-snapshot.txt` 的任何一行；出现差异一律视为回归并回滚，而不是更新基线。
- 不建立并发压力测试、不做 `IAsyncDisposable`、不把 `DataSet` 换成 `DbDataReader`。

## Decisions

### D1: 保留显式 `LangVersion=14.0`，不依赖 TFM 默认值

`net10.0` 的默认语言版本已是 C# 14（`language-versioning` 文档），但 `build-configuration` 规格要求语言版本在仓库根统一声明一次，且上一变更已建立这一约定。

- **采用**：`Directory.Build.props` 把 `<LangVersion>12.0</LangVersion>` 改为 `14.0`，与 `TargetFramework` 同处一个 `PropertyGroup`。
- **被否**：删掉 `LangVersion` 完全依赖默认值。理由：规格的"统一声明"约束、以及不同 SDK Feature Band 下默认值可能漂移；显式值让 `dotnet --version` 与语言版本的对应关系可审核。
- **被否**：`LangVersion=latest` / `preview`。理由：会把未来 C# 15 语法悄悄带进 LTS 项目，且与"固定 SDK 主版本"（D2）意图冲突。

### D2: 用 `global.json` 固定 SDK 主版本，而非不固定

- **采用**：根目录 `global.json` 指定 `10.0.1xx` Feature Band 的 SDK 版本、`rollForward: latestFeature`、`allowPrerelease: false`。`latestFeature` 允许同一主版本内的补丁/Feature Band 更新，避免把具体补丁号钉死导致 CI 立刻失败。
- **被否**：不写 `global.json`。理由：本机只有 .NET 8 SDK，协作者/CI 可能装了多个 SDK，`dotnet build` 会挑到不同版本，使 `-warnaserror` 结果不可复现。
- **被否**：`rollForward: disable` 精确钉死补丁。理由：安全补丁发布后仓库会立刻无法构建。
- 前置条件：实施第一步必须安装 .NET 10 SDK（`dotnet --list-sdks` 出现 `10.x`），否则三道验证门槛全部无法执行。

### D3: 依赖按 .NET 10 全量对齐，测试套件按 MSTest 4 规范重构

用户已确认"全量对齐"而非保守升级；并在 explore 阶段追加确认"单元测试要重构到符合 MSTest 4 规范"，**替换**最初设想的"最小签名适配"。

| 包 | 当前 | 目标 | 适配原则 |
| --- | --- | --- | --- |
| `Microsoft.Extensions.DependencyInjection` | 7.0.0 | 10.x | 与 TFM 同主版本；`ComponentProviderTest` 验证两条工厂路径仍可解析 |
| `Microsoft.NET.Test.Sdk` | 17.5.0 | 18.x | 与 net10 测试宿主对齐 |
| `MSTest.TestAdapter` / `MSTest.TestFramework` | 3.6.0 | 4.x | 测试套件按 v4 规范重构（下表），不是最小签名适配 |
| `coverlet.collector` | 3.2.0 | 6.x | 保证 `--collect:"XPlat Code Coverage"` 仍可用 |
| `MySql.Data` | 9.1.0 | 9.x 最新稳定 | 验证 `MySqlClientFactory` 的 `DbProviderFactories.RegisterFactory` 路径仍编译 |
| `log4net` | 3.0.4 | 3.x 最新稳定 | 验证 `LogManager.CreateRepository` / `XmlConfigurator.Configure` / `GetLogger` 调用面 |

MSTest 4 规范在本仓库的确切命中（只读核查，非推测）：

```
  MSTEST 4 CONFORMANCE MAP
  ===================================================================
  REQUIRED (compile)   Assert.ThrowsException -> ThrowsExactly  13
  -------------------------------------------------------------------
  ANALYZER-DRIVEN      StringAssert.Contains  -> Assert.Contains  2
                       CollectionAssert.Contains -> Assert.Contains 4
                       IsInstanceOfType<T> now returns T           2
  -------------------------------------------------------------------
  CONVENTION           sealed on test classes                    10
                       [DataRow] on [TestMethod] (NOT DataTestMethod)
                       4 truth-table / snapshot clusters
                       culture mutation + [DoNotParallelize]       1
  -------------------------------------------------------------------
  OPTIONAL SWITCH      MSTestAnalysisMode=Recommended -> Info->Warning
  ===================================================================
```

- **关键决策 1（测试入口）**：MSTest 4 引入 Microsoft.Testing.Platform (MTP)。选择"让 `dotnet test` 与 `AGENTS.md` 里所有命令（`--filter`、`--collect`）继续按原样工作"的那套宿主配置（MTP 的 dotnet test 桥接或 VSTest 兼容模式），由实施时实际运行结果决定开关，不预先假定。仓库显式引用 `Microsoft.NET.Test.Sdk`（不是 `MSTest.Sdk`），因此 v4 迁移文档里"MTP 模式下不再自动带 Test.Sdk、vstest.console 找不到测试"那条**不适用**，这里只是验证而非设计开关。
- **关键决策 2（断言 API）**：13 处 `Assert.ThrowsException<T>` → `Assert.ThrowsExactly<T>` 是**编译必需**（v4 已删除该 API，MSTEST0039 亦要求迁移），放在提交 #1 内，否则测试项目升级后无法编译。其余规范化放提交 #2：6 处 `StringAssert` / `CollectionAssert` → `Assert.Contains`（MSTEST0046 / MSTEST0068）、`Assert.IsInstanceOfType<T>` 改用返回的强类型实例、10 个测试类加 `sealed`。
- **关键决策 3（数据驱动）**：`ObjectCalculatorTest` 5 个真值表方法（约 40 条 `IsTrue/IsFalse`）、`StatementAnalystTest` 4 个快照方法 + 7 个异常方法、`SqlLiteralTest` 9 个单断言方法、`ConfigManagerTest` 5 条 TableName 断言，收敛为 `[TestMethod] + [DataRow]`。**不使用 `[DataTestMethod]`**（MSTEST0044 已反对，v4 中 `[TestMethod]` 直接支持数据行）。收益：断言意图由行数据表达、失败定位到具体用例、且为 C# 14 集合表达式 / span helper 提供低风险落点。代价：用例计数与 `TestCase.Id` 重基线；本仓库无 CI / Azure DevOps 测试历史，影响有限。
- **关键决策 4（分析器模式）**：保持 `MSTestAnalysisMode` 默认，**不**设为 `Recommended`。理由：它会把十余条 Info 规则升为 Warning，而本仓库 `-warnaserror` 是硬门槛，等于一次性把测试风格整治塞进一次框架升级。只修 v4 升为 Warning 的 12 条规则与已删除 API 引入的告警。
- **关键决策 5（culture）**：`SqlLiteralTest` 的 `CultureInfo.CurrentCulture` 修改加 `[DoNotParallelize]`（MSTEST0076），保留 try/finally 恢复。
- **被否**：保留 MSTest 3.x 只升 DI（用户已否决保守升级）；一次性把测试项目重写为 MTP 原生项目文件（改变测试工程结构）；只做 13 处改名就停（用户已否决"最小适配"）；启用 `MSTestAnalysisMode=Recommended`（理由见决策 4）。
- 版本精确到哪个补丁由 `dotnet restore` 时的最新稳定版确定；任何 NU1701/NU190x 都不是"可接受的告警"，必须换版本或换包。

### D4: C# 13/14 特性采用矩阵（核心决策，边界已放宽）

用户确认放宽最初的"必须有自然落点"。**放开的是这一条**，其余三条边界不变：

```
  D4: WHAT "RELAXED" MEANS
  =============================================================

  before                              after
  ------                              -----
  needs a natural site      ------->  small, reviewable,
                                       behavior-equivalent
                                       rewrite is allowed
  semantic equivalence      ------->  STILL REQUIRED
  no new abstraction        ------->  one allowed: extension blocks
  GlobalCache / SQL text /  ------->  STILL FROZEN
  public API spelling

  freed by the relaxation: extension blocks, partial members,
  lambda modifiers, more `field`, null-conditional assignment
  still no plausible site: user-defined compound assignment,
                           file-based app directives
```

"采用"必须落在具体、可审阅、行为等价的改写上；"不采用"必须给出理由，而不是遗漏。测试项目是新增特性的首选落点（见 D9）。

| 特性 | 结论 | 落点 / 理由 |
| --- | --- | --- |
| `field` 关键字（semi-auto 属性） | **采用 + 有限扩面** | 原 4 处显式后备字段：`EntityMapping.Keys` / `Relations`（`set => field = value ?? []`，初值用属性初始化器 `= []`）、`PersistenceContext.Connection`（`get` 中 `field is null || field.State == Closed` 时重建）、`LoadList<T>.Items`（`=> field ??= LoadItems()`，特性规格明确表达式体属性可用 `field`）。放宽后有限扩面到 `CacheEntity.Value` / `ForeignKeys` 与两个 `SessionFactory` 的 `Configuration`——**只做机械替换，不加运行时 null 守卫、不碰 `required` 属性**（加守卫属于行为变化）。公开属性名、反射可见性、赋值语义都不变。 |
| `nameof` 非绑定泛型 | **条件采用** | `SessionCache.cs` 有 4 处 `nameof(LoadList<object>.ForeignKeyValues/SessionCache/ForeignKeyName)`。先做一个最小编译探针验证 `nameof(LoadList<>.X)` 是否合法；合法才替换并跑 `LoadListTest`，不合法则保留现状并在提交信息记录原因。 |
| `Span<T>` / `ReadOnlySpan<T>` 一等公民转换 | **采用（受限）** | `StatementAnalyst` / `ObjectCalculator` 的首尾引号剥离与字符扫描、`LoadList` / `SQLGenerator` 的 `StringBuilder` 拼接，改用 `AsSpan()` 切片、`Append(ReadOnlySpan<char>)`、`SearchValues<char>.Contains`。只在不分配子字符串且文本输出完全等价处改写；逐处用序列快照与 SQL 快照校验。 |
| **扩展成员（`extension` 块）** | **采用（本次主要新增落点）** | `SQL/SqlLiteral.cs` 的 `Format(object?)` / `Comparison(string, object?)` 改为 `object?` 与 `string` 上的扩展成员，更新约 5 个文件 / 15 个调用点；再为 `StringBuilder` 增加一个 SQL 片段扩展方法，收拢 `LoadList` / `SessionCache` / `SQLGenerator` 中重复的 `Append(...).Append("==").Append(...)` 序列。两者都是纯转发、无状态，由 `SqlLiteralTest` / `SQLGeneratorTest` / `LoadListTest` 与 SQL 快照完整覆盖。放宽"不新增抽象层"后允许这一处。 |
| **partial 构造 / 事件** | **采用（受限）** | `Configuration/ConfigManager.cs`（188 行）按解析阶段拆为 partial 并使用 partial 构造；`StatementAnalyst.cs`（513 行）优先级表下标耦合太强、`GlobalCache.cs` 已冻结，**都不拆**。 |
| **lambda 参数修饰符 / 空条件赋值 / 更广的 span 用法** | **采用（测试沙盒）** | 落在 `FakeDb*` 假件、`StatementAnalystTest.Render`、`double.TryParse` 委托等低风险位置；这些位置即使放宽也没有生产行为风险，且回归网最密。 |
| `System.Threading.Lock`（C# 13） | **不采用** | 唯一落点是 `GlobalCache` 手写锁，改写需先有并发回归测试，属 `docs/modern-csharp-migration-plan.md` §12.1 独立提案。本变更明确不动。 |
| 用户自定义复合赋值、file-based app 指令、`params` 集合、`\e`、隐式索引初始化器 | **不采用** | 即使放宽也找不到可审阅的落点：无运算符重载类型、无 file-based app、无 `params` 数组 helper、无转义字符需求；为凑齐特性去新建类型会改变公开类型列表，超出"小规模、可审阅"的边界。 |
| `nameof(LoadList<>)` 之外的枚举 / 索引对齐改动 | **不采用（红线）** | `_sqlOperators` / `_priorityTable` 按下标索引，禁止重排。 |

**替代方案（被否）**：为覆盖"尽可能使用最新语法"而在 ORM 语义敏感路径（`GlobalCache` / `StatementAnalyst` 核心状态机 / `SQLGenerator`）里制造落点，或放宽"语义等价"。理由：前者扩大回归面且无收益，后者把一次框架升级变成行为变更，与用户确认的边界和既有红线直接冲突。亦否决"把全部 C# 14 特性都用满"——仍逐特性记录不采用理由，而不是遗漏。

### D5: 先基线、再基础设施、再测试网、后语法，一组一提交

上一变更已证明"单一关注点提交 + 三道门槛"可行，本变更沿用：

1. 记录升级前基线：`dotnet build`、`dotnet test`、`docs/baselines/sql-snapshot.txt` 的副本（只读，不改仓库）。
2. 提交 #1：SDK 安装 + `global.json` + `Directory.Build.props`（TFM/LangVersion）+ 三个 csproj 依赖升级 + `.editorconfig` 最小增量 + **MSTest 4 编译必需的 13 处断言改名**（否则测试项目无法编译）。**必须是序列第一项**：后续所有 C# 14 语法都依赖它，回滚它需连带回滚之后全部提交。
3. 门槛：`dotnet restore`（无 NU 警告）、`dotnet build -warnaserror`、`dotnet test`、`dotnet format --verify-no-changes`。**先把新 SDK 与 MSTest 4 带来的新告警全部处理完**，再进入下一步，避免两类问题混在一个 diff 里。
4. **提交 #2（新增）**：MSTest 4 测试套件规范化（D3 决策 2–5、D9 数据驱动）。**必须先于任何 C# 14 语法改写**——这张网正是后面所有语法提交的安全网；该提交单独跑全量测试 + 快照逐字节比对，确认断言语义未变。
5. 提交 #3–#7：按 D4 矩阵分组（`field` → `nameof` / Span → 扩展成员 → partial / 测试沙盒），每组独立提交，逐组比对 SQL 快照 / 中缀后缀快照 / 真值表。
6. 收尾：文档同步 + 全量门槛 + 人工连库验证（如环境允许）。

替代方案（被否）：把依赖升级、测试重构与语法改写合并成一个大提交；或先改语法再改测试网。理由：前者三类问题互相掩盖、`git bisect` 失效；后者让测试重构自身引入的断言错误与语法回归混在一起。

### D6: `field` 改写的语义核对点

- `EntityMapping.Keys` / `Relations`：属性初始化器直接写后备字段、不经过 setter（特性规格明确），因此 `= []` / `= new Dictionary<...>()` 保留为初始化器；`set => field = value ?? []` 保留"赋 null 后仍返回空集合"的既有语义。用 `ConfigManagerTest` 的映射断言验证。
- `PersistenceContext.Connection`：`field` 是编译器合成字段，原先 `_connection` 为 `IDbConnection?`；改写后 getter 内 `field is null || field.State == ConnectionState.Closed` 的短路顺序不变，且"共享连接不被使用方释放"（`session-lifecycle` 规格）不变。用 `SessionTest` / `SessionCacheTest` 验证。
- `LoadList<T>.Items`：`private List<T> Items => field ??= LoadItems();` 与原来 `_items ??= LoadItems()` 行为一致；`LoadListTest` 断言"懒加载只触发一次"与谓词文本不变。
- 有限扩面（`CacheEntity.Value` / `ForeignKeys`、两个 `SessionFactory.Configuration`）：只做 `= null!` / 自动属性 → `field` 半自动属性的机械替换，**不引入新的运行时校验**；用 `ConfigManagerTest` / `SessionCacheTest` / `ComponentProviderTest` 验证两条工厂路径。
- 补充核对：C# 14 把 `field` 作为上下文关键字，若类型里存在同名成员会触发 CS9258（警告）/ CS9272（错误）。已确认全仓库无此标识符；实施时再跑一次 `rg '\b(field|extension)\b'` 作为门槛。

### D7: 新分析器与编译器破坏性变更的处置

.NET 10 SDK 与 MSTest 4 会带进新诊断与新的重载解析规则，处理原则：

- 先 `dotnet build -warnaserror` 收集全部新告警；能就地修复且不改行为的直接修（例如 Span 重载歧义加 `.AsSpan()`、冗余 `or` 模式加括号）。
- 确属"与红线冲突"的告警才在 `.editorconfig` 抑制，且必须写注释说明原因（沿用现有 `CA1840` / `CA1716` 的写法）。
- MSTest 4 把 12 条分析器规则由 Info 升为 Warning，并把 `TreatDiscoveryWarningsAsErrors` 默认设为 true；按 D3 决策 4 **不启用 `MSTestAnalysisMode=Recommended`**，只修这批实际出现的告警，不批量抬升规则集。
- 已知风险点：CS9258/CS9272（`field`/`extension` 标识符）、`Enumerable.Reverse` 被解析为 `MemoryExtensions.Reverse`、MSTest 4 的 `Assert.AreEqual` 在集合表达式 / `[DataRow]` 实参下出现 Span 重载歧义、`is not ... or ...` 冗余模式告警。当前仓库零命中，升级依赖后重新扫描。
- `dotnet format --verify-no-changes SchoolEndORM.sln` 与 `-warnaserror` 同为硬门槛，任一不过不进入下一组。

### D8: 可观察行为零变化的判定标准

以下任一项出现差异即判为回归并回滚该提交，**不允许**通过修改期望值/基线来"通过"：

- `docs/baselines/sql-snapshot.txt` 四类 SQL 语句文本；
- `StatementAnalystTest` 的中缀 / 后缀序列快照；
- `ObjectCalculatorTest` 真值表、`ConfigManagerTest` 映射断言、`LoadListTest` 谓词文本与"只加载一次"；
- 异常类型层次（`OrmException` 派生）、幂等 `Dispose`、`Config.xml` 解析结果；
- 公开 API 名称与拼写、程序集 / 类型全名（反射契约）。

### D9: 测试项目承载新增 C# 14 特性，且必须先于语法提交完成

- **采用**：把测试项目（10 个测试类 + `Fakes/`）作为"放宽自然落地"后的主要落点：`[DataRow]` 数据驱动本身是 MSTest 4 规范（D3 决策 3），同时天然承载集合表达式、`ReadOnlySpan<char>` 辅助方法、`field` 假件、空条件赋值、lambda 参数修饰符等 C# 14 特性，无需在生产代码里制造落点。`StatementAnalystTest.Render` 改 span 风格、`FakeDb*` 用 `field` 与空条件赋值，都由既有快照与 `SessionTest` / `LoadListTest` 守卫。
- **顺序**：测试套件重构单独立为提交 #2，紧跟提交 #1、先于任何 C# 14 语法改写（D5 步骤 4）。理由：这张回归网是后续所有语法提交的安全网；若先改语法再改网，测试重构引入的断言错误会与语法回归混在一起，`git bisect run dotnet test` 失效。该提交单独跑全量测试 + 快照逐字节比对。
- **被否**：把扩展成员 / partial / lambda 修饰符直接铺进 `ORMFramework` 的解析与缓存路径。理由：这些路径要么下标强耦合（`StatementAnalyst`），要么无自动化并发覆盖（`GlobalCache`），收益不足以抵消回归风险。
- **被否**：为凑齐全部 C# 14 特性而新建运算符重载类型或 file-based app。理由：改变公开类型列表 / 工程结构，且没有可审阅的行为收益，超出"小规模、行为等价"的放宽边界。

## Risks / Trade-offs

- **[本机无 .NET 10 SDK，三道门槛无法执行]** → 提交 #1 第一步安装 SDK 并用 `dotnet --list-sdks` / `dotnet --version` 确认；若环境无法安装，实施阶段应停下并向用户报告，不做"未验证即宣称完成"的改动。
- **[MSTest 4 / MTP 改变测试宿主，导致 `dotnet test --filter` 或 `--collect` 失效]** → 以 `AGENTS.md` 中列出的命令全部按原样通过为验收；按实际运行结果选择桥接开关，必要时保留 VSTest 兼容模式；`ComponentProviderTest` 与 `ConfigManagerTest` 作为 DI / 配置路径的哨兵。
- **[数据驱动改变用例计数与 `TestCase.Id`，且 `--filter` 在 MTP 下语义可能不同]** → 提交 #2 单独比对用例总数与 1.2 基线，三条 `dotnet test` 命令逐一按原样验证；本仓库无 CI / Azure DevOps 测试历史，重基线影响有限。
- **[`MSTestAnalysisMode=Recommended` 与 `-warnaserror` 相撞，产生告警风暴]** → D3 决策 4 明确不启用；只修 v4 升为 Warning 的 12 条规则与已删除 API，其余 Info 规则保持默认严重度。
- **[依赖大版本升级带来 API 破坏]** → `log4net` / `MySql.Data` 只做最小签名适配，不重构 `LogHelper`；`DI` 10.x 用两条工厂路径的既有测试验证；`dotnet restore` 出现 NU 警告即换版本，不抑制。
- **[C# 14 编译器破坏性变更静默改变语义]** → D7 的扫描与 `-warnaserror` 门槛；Span 改写逐处做快照比对；对 `arr.Reverse()` 一类解析变化保持显式调用。
- **[MSTest 4 `Assert.AreEqual` 与 C# 14 Span 重载在 `[DataRow]` 实参下出现歧义]** → 提交 #2 先 `dotnet build -warnaserror` 收集，必要时加显式类型实参或 `.AsSpan()`；不通过修改期望值绕过。
- **[扩展成员改写扩散到约 15 个调用点 / 5 个文件]** → 限定为纯转发、无状态；`SqlLiteralTest` / `SQLGeneratorTest` / `LoadListTest` 与 `docs/baselines/sql-snapshot.txt` 逐字节比对，任何文本差异即回滚。
- **[`field` 关键字的可空性分析产生新 CS86xx 告警]** → 改写后立刻 `dotnet build -warnaserror`；确实需要时用属性初始化器 / 显式标注消除，不用 `!` 硬压。
- **[Span 改写引入不可见的输出差异（culture、引号、切片边界）]** → 限定在"切片与 Append"层面，不碰 `SqlLiteral.Format` 的格式化逻辑；SQL 快照逐字节比对。
- **[范围从 31 项膨胀到 47 项，而 .NET 8 于 11/11/2026 EOL]** → 把 TFM / 依赖 / MSTest 4 编译必需修复集中在提交 #1，测试网规范化在提交 #2，语法扩面从提交 #3 起各组可独立 revert；若需把"更多 C# 14"拆成后续变更，属另一次确认，不在本变更内擅自处理。
- **[`GlobalCache` 脆弱区被顺手改动]** → D5 明确该文件不在语法改写范围内；任何新告警只做最小修复或有理由抑制，不反转嵌套、不动 `Monitor`。
- **[TFM 提升是不可逆的 BREAKING]** → 在 `README.md` / `AGENTS.md` 明示最低运行时；回滚策略是整体 revert 提交 #1 及其后续，而不是保留部分 C# 14 代码。
- **[人工连库验证依赖可达 MySQL]** → 与上一变更一致，把 `TestMultitransaction` / `TestOneToMany` / `TestManyToMany` 列为"需本地 MySQL，环境不具备时在提交信息中标注未执行"，不以未执行的检查冒充通过。

## Migration Plan

按 D5 的提交序列执行；每次提交的验证门槛固定为：

```bash
dotnet restore SchoolEndORM.sln
dotnet format --verify-no-changes SchoolEndORM.sln
dotnet build SchoolEndORM.sln -warnaserror
dotnet test ORMFramework.Test/ORMFramework.Test.csproj
```

涉及 SQL / 解析 / 求值的提交额外比对 `docs/baselines/sql-snapshot.txt` 与三类快照；涉及依赖、测试宿主与数据驱动的提交额外验证 `dotnet test --filter FullyQualifiedName~StatementAnalystTest` 与 `dotnet test --collect:"XPlat Code Coverage"` 仍可用，且用例总数与基线一致。

提交序列：#1 基础设施与依赖（含 13 处编译必需断言改名）→ #2 MSTest 4 测试套件规范化 → #3 `field` → #4 `nameof` / Span → #5 扩展成员 → #6 partial 构造 / 测试项目 C# 14 沙盒 → 收尾文档与全量门槛。

**回滚**：每个提交独立可 `git revert`；出现回归用 `git bisect run dotnet test` 定位。提交 #1（SDK/TFM/依赖）是回滚边界——回滚它必须连带回滚全部依赖 C# 14 语法的后续提交。提交 #2 改写的是回归网本身，若其快照比对出现差异，必须先修回再继续，不得带病进入 #3。

**部署/发布**：`dotnet publish ORMFramework.Examples -c Release -o dist` 的产物目标框架随之变为 `net10.0`；`dist/` 为 git-ignored 输出，无需迁移。下游消费者需把运行时升级到 .NET 10。

## Open Questions

- 各依赖包的具体补丁版本、以及 MSTest 4 下 `dotnet test` 究竟启用 MTP 桥接还是 VSTest 兼容模式，由实施时 `dotnet restore` 与 `dotnet test` 的实际结果确定；这不改变规格、方案或任务拆分（任务已写成"以命令按原样通过为准"）。
- `nameof(LoadList<>.X)` 是否被编译器接受，由任务中的最小编译探针确定；任务已按"合法才替换、不合法则记录并保持现状"写好，两种结果都不改变方案。
- `[DataRow]` 承载 `Type` / `null` 实参的具体写法（异常类型、null 输入）由实施时的编译验证确定；任务已按"收敛为 `[DataRow]`、保留全部期望值"写好，写法差异不改变断言覆盖。
