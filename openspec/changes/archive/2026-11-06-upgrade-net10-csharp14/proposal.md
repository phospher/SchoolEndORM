# Proposal

## Why

`SchoolEndORM` 当前三个项目统一以 `net8.0` / C# 12 构建（见根目录 `Directory.Build.props`），这是 2026-08 归档变更 `modernize-csharp-syntax` 的结果。已 GA 并处于 LTS 支持期的 .NET 10 把默认语言版本推进到 C# 14（`field` 关键字、扩展成员、空条件赋值、`nameof` 非绑定泛型、`Span<T>` 一等公民转换等），而本仓库仍把 `LangVersion` 钉在 `12.0`，既用不上这些特性，也拿不到 .NET 10 的分析器、运行时与依赖安全更新。

同时依赖基线停留在 2023–2024 年（`Microsoft.Extensions.DependencyInjection` 7.0.0、`Microsoft.NET.Test.Sdk` 17.5.0、`MSTest` 3.6.0、`coverlet.collector` 3.2.0、`MySql.Data` 9.1.0、`log4net` 3.0.4），与 `net10.0` 不对齐。当前开发机仅安装 .NET 8 SDK（8.0.133，`dotnet --list-sdks` 已确认），升级必须显式安装并固定 .NET 10 SDK 才能验证。

## What Changes

工程与运行时基线

- 三个项目 `net8.0` → `net10.0`、`LangVersion` 12.0 → 14.0，仍集中在 `Directory.Build.props` 一处声明；`Nullable=enable`、隐式 using、`EnableNETAnalyzers` / `EnforceCodeStyleInBuild` 保持不变。
- 新增仓库根 `global.json` 固定 .NET 10 SDK 版本与 `rollForward` 策略，使 `dotnet build` 在不同机器上可复现。**BREAKING**：目标框架提升到 `net10.0`，以 .NET 8 为运行时的下游消费者必须同步升级。
- 第三方依赖全量对齐 .NET 10：`Microsoft.Extensions.DependencyInjection` → 10.x、`Microsoft.NET.Test.Sdk` → 18.x、`MSTest.TestAdapter` / `MSTest.TestFramework` → 4.x、`coverlet.collector` → 6.x、`MySql.Data` → 9.x 最新稳定版、`log4net` → 3.x 最新稳定版；`dotnet restore` 不得产生 NU1701（回退兼容）或 NU190x（已知漏洞）警告。MSTest 4 与 v3 **不二进制兼容**，测试项目随之按 MSTest 4 规范重构（见下）；测试宿主（MTP / VSTest）按实际运行结果选配置，以 `dotnet test` 及 `AGENTS.md` 中的 `--filter` / `--collect` 命令按原样全绿为准。
- `.editorconfig` / `Directory.Build.props` 按 .NET 10 新增分析器规则做最小增量：新增告警必须修复，或带理由地抑制并在注释中说明；`dotnet format --verify-no-changes` 与 `dotnet build -warnaserror` 必须保持全绿。

单元测试重构（MSTest 4 规范）

- **BREAKING（测试源码）**：MSTest 4 删除了 `Assert.ThrowsException`，本仓库 13 处改用 `Assert.ThrowsExactly` / `Assert.Throws`；`StringAssert.Contains` / `CollectionAssert.Contains` 共 6 处改 `Assert.Contains`（MSTEST0046 / MSTEST0068）；`Assert.IsInstanceOfType<T>` 改用其返回的强类型实例。
- 10 个测试类加 `sealed`；4 簇复制粘贴的真值表 / 序列快照 / 异常断言改为 `[TestMethod] + [DataRow]` 数据驱动（**不使用**已被 MSTEST0044 反对的 `[DataTestMethod]`）；`SqlLiteralTest` 的进程级 culture 变更加 `[DoNotParallelize]` 保护（MSTEST0076）。
- 只修 v4 升为 Warning 的分析器规则与已删除 API，**不**启用 `MSTestAnalysisMode=Recommended`（会把十余条 Info 规则升级为 `-warnaserror` 错误，超出本次范围）。
- 验证数据驱动与宿主：`dotnet test`、`dotnet test --filter FullyQualifiedName~...`、`dotnet test --collect:"XPlat Code Coverage"` 按原样全绿，用例总数不减少，四类 SQL 文本与三类快照逐字节 / 逐元素不变。

C# 14 语法现代化（语义等价为硬边界；"自然落地"已按用户确认放宽）

放宽的只是"必须有自然落点"：允许为采用 C# 14 特性做小规模、可审阅、行为等价的改写；**语义等价、零可观察行为变化、既有红线**三者不变。测试项目是新增特性的首选落点（低风险、回归网最密）。

- `field` 关键字：原 4 处显式后备字段（`EntityMapping.Keys` / `Relations`、`PersistenceContext.Connection`、`LoadList<T>.Items`）之外，有限扩面到 `CacheEntity.Value` / `ForeignKeys` 与两个 `SessionFactory` 的 `Configuration`——只做机械替换，**不加运行时 null 守卫、不碰 `required` 属性**。已确认全仓库无名为 `field` 的标识符，也无 `nameof(field)` 用法。
- `nameof` 非绑定泛型：先用最小编译验证确认 `nameof(LoadList<>.X)` 合法性，合法则把 `SessionCache.cs` 中 4 处 `nameof(LoadList<object>.X)` 收敛；不合法则保持现状并在 `design.md` 记录原因。
- `Span<T>` / `ReadOnlySpan<T>` 一等公民转换：`StatementAnalyst` / `ObjectCalculator` 的引号剥离与字符扫描、`LoadList` / `SQLGenerator` 的 `StringBuilder` 拼接，改用 `AsSpan()` 切片、`Append(ReadOnlySpan<char>)`、`SearchValues<char>`；前提是中缀/后缀序列与生成的 SQL 文本逐元素、逐字节不变。
- **扩展成员（`extension` 块，本次主要新增落点）**：把 `SQL/SqlLiteral.cs` 的 `Format(object?)` / `Comparison(string, object?)` 改为 `object?` 与 `string` 上的扩展成员，更新约 5 个文件 / 15 个调用点；再为 `StringBuilder` 增加一个 SQL 片段扩展方法，收拢 `LoadList` / `SessionCache` / `SQLGenerator` 中重复的 `Append` 序列。两者均为纯转发，由 `SqlLiteralTest` / `SQLGeneratorTest` / SQL 快照覆盖。
- **partial 构造 / 事件**：把 `Configuration/ConfigManager.cs` 按解析阶段拆为 partial 并使用 partial 构造；脆弱的 `StatementAnalyst.cs`（513 行）与冻结的 `GlobalCache.cs` **不拆**。
- **lambda 参数修饰符、空条件赋值、更广的 span 用法**：落在测试 helpers 与 `FakeDb*` 假件、`double.TryParse` 委托、`StatementAnalystTest.Render` 等低风险位置，不进入 ORM 语义敏感路径。
- 仍逐特性记录"不采用"结论：用户自定义复合赋值、file-based app 指令（即使放宽也无合理落点）、`System.Threading.Lock`（唯一落点是 `GlobalCache`，需并发回归测试，属独立后续提案）。
- 沿用上一变更确立的红线：`GlobalCache` 的 `Monitor` / `Thread.Interrupt` 锁模型、`_sqlOperators` / `_priorityTable` 索引对齐、拼串 SQL（不参数化）、`Config.xml` 格式与解析方式、公开 API 拼写（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）、`Statment` 命名空间、实体 public 无参构造与 `IList<T>` 导航属性声明。

安全网与文档

- 升级前记录基线（`dotnet build`、`dotnet test`、`docs/baselines/sql-snapshot.txt`），升级后逐项比对：SQL 文本快照、中缀/后缀序列快照、`ObjectCalculator` 真值表、`ConfigManager` 配置断言、`LoadList` 懒加载只触发一次，全部保持一致；测试套件重构提交单独跑一遍同样的比对，确认断言语义未变后再进入语法提交。
- 同步 `AGENTS.md`、`README.md`、`docs/modern-csharp-migration-plan.md` 中的目标框架 / 语言版本记述，并在后者标注 net8 → net10 的完成状态、C# 14 特性采用矩阵与仍然后置的提案清单。

明确不做

- 不做异步化 / `DbDataReader` 替代 `DataSet`、不做参数化 SQL、不迁移 log4net → `Microsoft.Extensions.Logging`、不改公开 API 拼写、不重写 `GlobalCache` 锁模型、不改 `Config.xml` 格式（均为 `docs/modern-csharp-migration-plan.md` §16.4 的独立后续提案）。
- 不启用 `MSTestAnalysisMode=Recommended`、不引入新的分析器规则集、不把测试项目重写为 MTP 原生项目文件；不为"用满 C# 14 特性"制造生产代码的调用点或抽象层。

## Capabilities

### New Capabilities

（无：除构建基线外，本次升级均为行为等价的语法、测试与依赖改写，不引入新的可观察行为契约。语法采用边界与验收在 `design.md` / `tasks.md` 中落地，由 `build-configuration` 的零警告构建门槛与既有回归基线约束。）

### Modified Capabilities

- `build-configuration`: 目标框架 / 语言版本由 `net8.0` / C# 12 改为 `net10.0` / C# 14；新增 SDK 版本固定、依赖与 `net10.0` 对齐、最低运行时要求、新分析器告警处置四项约束。

## Impact

- 代码：根目录 `Directory.Build.props`、新增 `global.json`、三个 `.csproj`、`.editorconfig`；`ORMFramework` 约 8–12 个文件涉及 C# 14 改写（`EntityMapping.cs`、`CacheEntity.cs`、`PersistenceContext.cs`、`LoadList.cs`、`SessionCache.cs`、`SqlLiteral.cs`、`ConfigManager.cs`、`StatementAnalyst.cs`、`ObjectCalculator.cs`、`SQLGenerator.cs`、`SessionFactory*.cs`）；`ORMFramework.Test` 全部 10 个测试文件与 `Fakes/` 假件（约 12 个文件）；`AGENTS.md`、`README.md`、`docs/modern-csharp-migration-plan.md`。任务规模由 31 项增至 47 项，测试项目与 helper 层承载了大部分新增语法密度。
- 依赖：6 个包升级。MSTest 4 与 v3 不二进制兼容，删除 `Assert.ThrowsException`、把 12 条分析器规则升为 Warning、并把 `TreatDiscoveryWarningsAsErrors` 默认设为 true；`Microsoft.Extensions.DependencyInjection` 10.x 用两条工厂路径的既有测试验证。
- 测试形态：4 簇断言改为 `[DataRow]` 数据驱动，用例计数与 `TestCase.Id` 会重基线（本仓库无 CI / Azure DevOps 测试历史，影响有限）；需验证 `--filter` / `--collect` 在所选宿主下按原样工作。
- API：公开成员名称、`Config.xml` 格式、异常层次、`Dispose` 语义、生成的 SQL 文本与谓词求值结果 100% 不变。唯一 BREAKING 是 TFM 提升到 `net10.0`（测试源码的断言 API 变更属于框架源码内改动，不面向下游消费者）。
- 环境：当前机器只有 .NET 8 SDK，实施前必须安装 .NET 10 SDK；`ORMFramework.Examples` 依赖可达 MySQL，连库类的手工验证（`TestMultitransaction`、`TestOneToMany` / `TestManyToMany`）仍需人工执行。
- 高风险文件：`Cache/GlobalCache.cs`（手写不平衡锁）沿用上一变更的白名单，本变更不触碰任何 `Monitor` / `Thread` 语句；`Statment/StatementAnalyst.cs` 只做快照守卫下的局部改写。
