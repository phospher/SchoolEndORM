## Why

`SchoolEndORM` 三个项目已经是 `net6.0`（已于 2024-11 EOL），但代码的语法风格整体停留在 C# 2.0–3.0：57/57 文件是块级 namespace，约 50 个文件带 VS2005 的样板 using，30 个手写 `get/set` 属性，28 处 `throw new Exception`，30 处 `ContainsKey` + 索引器双查找，2 个在终结器里访问托管对象的类，6 处按字符串硬编码的反射。缺少 `Directory.Build.props`、`.editorconfig`、分析器配置，所有风格约定无法机器验证，也没有回归测试网保护表达式解析/SQL 生成这三份互相耦合的实现。

本变更把语言与工程规范推进到 **C# 12 / .NET 8 LTS**，让 `dotnet format --verify-no-changes` + `dotnet build -warnaserror` + `dotnet test` 成为可执行的质量门槛，同时消除改造过程中必然撞上的既有缺陷。依据文档：`docs/modern-csharp-migration-plan.md`。

## What Changes

工程基础设施（方案第 3 章）

- 三个项目 `net6.0` → `net8.0`；新增仓库根 `Directory.Build.props`（TFM / `LangVersion=12.0` / `ImplicitUsings` / `Nullable` / `EnableNETAnalyzers` / `EnforceCodeStyleInBuild`）与 `.editorconfig`（Allman、file-scoped namespace、`_camelCase` 私有字段、无 `this.` 前缀等）。
- 清理 csproj 冗余项（`Microsoft.NETCore.Platforms` 包引用、`<None Remove="Ioc\" />`、`<Folder Include="Ioc\" />`、`<WarningLevel>4</WarningLevel>`、Debug 条件段）；升级 `MySql.Data` / `MSTest` / `log4net` 至 net8 兼容版本。
- **BREAKING**：目标框架提升到 `net8.0`，`net6.0`/`net7.0` 的下游消费者需同步升级。

语法现代化（方案第 4–7 章，语义等价为原则）

- file-scoped namespace、隐式 using + 清理冗余 using（含删除 .NET Framework 遗留的 `System.Configuration.Provider`）、目标类型 `new`、`readonly` 字段、`var`、就近声明、私有标识符命名与拼写修正。
- 手写属性 → 自动属性 / 表达式体成员；`ConfigManager`、`GlobalCacheResult`、`QueryExpression`、`DefaultComponentProvider` 改主构造函数；`LoadList<T>` 的 12 处 `if (_items == null) GetItems();` 守卫收敛为 `_items ??= LoadItems()`；`LogHelper` → `static class`；对无派生类型加 `sealed`；仅 `QueryExpression` 改 `record`。
- switch 表达式、布尔表达式折叠（`char.IsAsciiLetterOrDigit`）、属性模式 / 列表模式 / `is not` 声明模式、`ConfigManager.GetSystemConfiguration` 卫语句化。
- 集合表达式、`FrozenSet<Type>` / `SearchValues<char>` 替代线性查找、`TryGetValue`/`TryAdd` 消除双查找、插值字符串 + 显式 `InvariantCulture`、索引/范围运算符、`StringBuilder.Clear()`、`double.TryParse` 替代异常控流、适度 LINQ 与 `KeyValuePair` 解构。
- 新增 `ORMFramework/SQL/SqlLiteral.cs`，消除 5 份重复的 SQL 字面量格式化逻辑（本变更中唯一允许的结构性抽取）。

资源、异常与反射（方案第 8–9 章）

- `using` 声明替代手写 `Close()`；**BREAKING（行为）**：删除 `Session` 与 `SessionCache` 的终结器，`Dispose` 改为幂等。
- 新增 `ORMFramework/Exceptions/`（`OrmException` / `QuerySyntaxException` / `OrmConfigurationException` / `ConcurrencyException`），替换 28 处 `throw new Exception`，`throw ex;` → `throw;`；参数校验改用 .NET throw helper。**BREAKING**：调用方若按 `Exception` 之外的类型断言/捕获需调整（原类型仍是 `Exception` 子类，`catch (Exception)` 不受影响）。
- `Assembly.Load("ORMFramework").GetType("ORMFramework.LoadList\`1")` → `typeof(LoadList<>)` + `nameof`；解除"程序集必须叫 ORMFramework"的隐式约束；`PropertyInfo.GetValue(obj, null)` → 单参重载。

可空引用类型与测试（方案第 10–11 章）

- `Nullable` 从 `annotations` 切到 `enable`，按目录分批消警，公开签名显式标注可空。
- 删除 4 个测试文件的 VS2005 样板与死代码（`List2`、注释测试块、未用字段）；`ConfigManagerTest` 硬编码 Windows 路径改为 `TestData/Config.xml` 夹具；新增 `StatementAnalyst` 中缀/后缀快照、`SQLGenerator` SQL 文本快照、`ObjectCalculator` 真值表、`SqlLiteralTest`、`LoadListTest`。

既有缺陷（方案第 13 章，每项独立提交）

- 🔴 阻塞项先修：`SessionCache.Collect` 的 `foreach (Guid ...)` 类型错误（13.1）、`GetObjectsFromGlobalCache` 的 `ignoreIds` 空引用（13.2）。
- 其余按项修复：`GlobalCache.Collect` 遍历中删除、`SQLGenerator` 的多余 `)` 与尾随分隔符、null 值生成 `'NULL'`、`ObjectCalculator` 变量比较取属性名、culture 缺失、`StatementAnalyst` 尾随空格越界与重复 `Init()`、`DefaultDbDriverFactory` 非原子注册、IoC 生命周期表 `KeyNotFoundException` 与 `SetSession<T>` 键错误、`Examples/Program.cs` 硬编码路径 + `Config.xml` 输出复制。
- 删除死代码：`Cache/ObjectCache.cs`、`Configuration/EntityClass/DatabaseType.cs`、`SQLGenerator.GetObjectPropertyValue`、三大块注释导航属性逻辑。

明确不做（方案第 12 / 14 / 16.4 章）

- 不改 `GlobalCache` 的锁模型（不动任何 `Monitor`/`Thread` 语句、不反转含 `Monitor` 的嵌套）；不做异步化 / `DbDataReader` 替代 `DataSet`；不做参数化 SQL；不迁移 log4net → `Microsoft.Extensions.Logging`。
- 不改公开 API 拼写（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）、不改 `Statment` 命名空间与目录对齐、不改 `Config.xml` 解析方式与格式、不重排 `Operator` 枚举与 `_sqlOperators` / `_priorityTable`、不把实体或 `StatElement` / `CacheEntity` / `*Event` 改成 `record`、不改实体导航属性的 `IList<T>` 声明。

## Capabilities

### New Capabilities

- `build-configuration`: 目标框架、语言版本、隐式 using、可空上下文、分析器与代码风格门槛的可验证约定。
- `error-reporting`: ORM 抛出的异常类型契约（自定义异常层次、参数校验异常、堆栈保留）。
- `sql-generation`: 生成 SQL 的字面量格式化契约（culture 不变、`NULL` 处理、分隔符正确性）与生成文本的可回归性。
- `query-parsing`: 查询谓词串的词法/语法分析与内存求值契约（ASCII 标识符、culture 不变的运算符与常量识别、非法输入的报错行为、运算符真值表）。
- `session-lifecycle`: `ISession` / `ISessionCache` 的释放语义（幂等 `Dispose`、无终结器、缓存引用释放）。

### Modified Capabilities

（无：`openspec/specs/` 当前为空，本变更引入的规格均为新建。）

## Impact

- 代码：`ORMFramework` 46 个 .cs 全量涉及；`ORMFramework.Examples` 6 个 .cs；`ORMFramework.Test` 5 个 .cs。新增 `Directory.Build.props`、`.editorconfig`、`ORMFramework/Exceptions/*`（4 文件）、`ORMFramework/SQL/SqlLiteral.cs`、`ORMFramework.Test/TestData/Config.xml`、`SqlLiteralTest.cs`、`LoadListTest.cs`。删除 `Cache/ObjectCache.cs`、`Configuration/EntityClass/DatabaseType.cs`。
- 高风险文件：`Cache/GlobalCache.cs`（541 行，手写不平衡锁）、`Statment/StatementAnalyst.cs`（605 行，优先级表下标强耦合）、`Cache/SessionCache.cs`（440 行）——放到各阶段末尾，且必须先有回归测试。
- 依赖：`MySql.Data`、`MSTest`、`log4net` 升版；移除 `Microsoft.NETCore.Platforms`。
- API：`Config.xml` 格式 100% 兼容；公开成员名称不变；异常类型收窄（派生自 `Exception`）、终结器删除属于行为变更。
- 预计净减约 1200 行（框架代码 25%–30%），无功能变更。

## Assumptions

- 本变更覆盖方案第 0–8 阶段（含第 13 章缺陷修复），第 12 章与第 16.4 节的 8 项列为后续独立提案，不在本变更内。
- 按方案 16.1 的 26 项提交序列实现，每项提交需独立通过 16.2 的三道验证门槛。
- `ConfigManagerTest` 在本变更后应从"预期失败"转为通过；`AGENTS.md` 中相关记述需同步更新。
