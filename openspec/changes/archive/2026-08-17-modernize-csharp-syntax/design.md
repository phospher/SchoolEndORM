## Context

见 `proposal.md` 的 Why。技术约束来自 `docs/modern-csharp-migration-plan.md` 与 `AGENTS.md`，其中三条塑造了本设计：

1. **三份并行的运算符实现**：`Statment/Operator.cs` 的枚举顺序同时被 `SQLGenerator._sqlOperators`（按 `(int)Operator` 下标索引）与 `StatementAnalyst._priorityTable`（10×10 双下标）依赖，`Cache/ObjectCalculator` 再按后缀序列独立求值。任何运算符相关改动必须四处协同，且枚举顺序不可变。
2. **两条工厂装配路径**：`SessionFactory`（手工装配）与 `SessionFactoryIoc`（DI + 事后补配）并存，且 DI 选中的是 `PersistenceContext(IDbDriverFactory, ISQLGenerator)` 重载。任何构造函数形态的改动（主构造函数、`sealed`、`required`）必须同时在两条路径上验证。
3. **`Cache/GlobalCache.cs` 是脆弱区**：跨方法不平衡的 `Monitor.Enter/Exit`、以 `Thread.Sleep(Infinite)` + `Thread.Interrupt()` 实现阻塞唤醒、`ThreadInterruptedException` 当作正常控制流。目前唯一的并发验证是被注释掉的 `Program.TestMultitransaction`，没有自动化覆盖。

此外，测试现状不足以充当安全网：5 个测试文件中大半是 VS2005 样板与注释，`ConfigManagerTest` 依赖 Windows 绝对路径而必然失败，`SessionCache.GetReferenceClasses` 的反射路径零覆盖。

## Goals / Non-Goals

**Goals:**

- 建立"先安全网、再机械变换、最后语义敏感变换"的执行次序，使每一步都能被 `dotnet format` / `dotnet build -warnaserror` / `dotnet test` 三道门槛验证。
- 让每次提交只承载一类变换，从而可 `git revert`、可 `git bisect run dotnet test` 定位。
- 把隐式约束（程序集名、类型全名字符串、属性名字符串）转成编译期可验证的形式。

**Non-Goals（设计层面的边界，proposal 已列的范围不再重复）:**

- 不引入新的抽象层、不新增接口去"解耦"现有 Session/Listener/Event 结构；本变更唯一允许的结构性抽取是 SQL 字面量格式化。
- 不追求覆盖率数字本身，回归测试只覆盖被改造触及的语义敏感面（解析、SQL 文本、内存求值、配置、懒加载）。
- 不在本变更内建立并发压力测试；因此也不改锁逻辑。

## Decisions

### D1: 安全网先行，测试是第 3 号任务而非最后一号

方案第 11.4 节的回归测试必须先于第 3/4/6 阶段（switch 表达式、布尔折叠、模式匹配、字面量抽取）完成。理由：这些变换是"人工重写表达式"，编译器不能证明语义等价，只有基线快照能。

具体安全网形态：

- **序列快照**：对固定查询串断言中缀/后缀元素序列（种类 + 值 + 顺序）。
- **SQL 文本快照**：用内存构造的 `IPersistenceContext` + 手写 `EntityMapping`，断言四类语句的完整字符串，无需数据库。
- **真值表**：`IsTargetObject` × 运算符 × 类型（int/string/DateTime/bool/char）的逐项断言。

替代方案（被否）：只靠 `dotnet build` + 现有测试。已知现有测试不覆盖 `SQLGenerator` 的 insert/update/delete 与 `ObjectCalculator` 的多数分支，`SQLGenerator` 已有的 13.6/13.7/13.8 缺陷正说明缺乏文本级断言。

### D2: 阻塞缺陷先修，其余缺陷与语法提交严格分离

13.1（`foreach (Guid objectId in _objList.Keys)` 遍历 `Dictionary<object, ...>`）与 13.2（`ignoreIds` 为 null 时立即 `Add`）必须在语法改造前修：前者一旦 `var` 化就编译失败，后者在 NRT 打开后必然告警。其余缺陷（13.3–13.20）安排在对应语法阶段之后，各自独立提交。

理由：如果"顺手修 bug"混进语法提交，`git bisect` 定位回归时无法区分是语法改错还是修 bug 改错。

例外：两处"语法改造天然附带修正"允许合并，但必须在提交信息中注明——列表模式 `is ['\'', .., '\'']` 顺带消除空串越界（7.5）、`TryGetValue` 顺带消除 `KeyNotFoundException` 风险。

### D3: `Nullable` 分两步走：先 `annotations`，最后 `enable`

第 0 阶段 `Directory.Build.props` 设 `<Nullable>annotations</Nullable>`（允许写 `?` 但不产生告警），最后一个阶段才切 `enable` 并按目录分 7 个子提交消警（Statment → Configuration → Ioc → SQLGenerator → Listener/Event → Session → Cache）。

理由：一次性 `enable` 会产生数百个告警，与"零警告构建"门槛直接冲突，导致中间阶段无法验证。分批消警使每个子提交的 diff 可 review。

反射返回值（`GetProperty(name)` 返回 `PropertyInfo?`，约 20 处）**不用 `!` 一律消警**：无 null 检查的位置改为 `?? throw new OrmConfigurationException(...)`，这正是 `error-reporting` 规格中"配置引用了不存在的属性时给出可读错误"的落地方式。

### D4: 字面量格式化抽取为唯一的结构性重构

5 份重复的 `if (o is string || o is char) ... else if (o is DateTime) ...` 抽取为 `internal static class SqlLiteral`，同时把 `CurrentCulture` 改为不变文化、把 null 从 `'NULL'` 改为 SQL 空值语义。

理由：这一处抽取同时消除 5 份重复、5 处 culture 缺陷与 1 处 null 缺陷，是收益/风险比最高的重构；而它本身是纯函数，可被 `SqlLiteralTest` 完整覆盖（含 `de-DE` culture 断言）。

替代方案（被否）：5 处各自改插值字符串。会保留重复，且 null 语义修正要改 5 遍，容易漏。

注意：null 语义修正会改变生成文本，因此 SQL 快照基线必须在此提交中同步更新，且该提交只做这一件事。

### D5: 集合与查找表的现代化边界

- `Operator` 枚举、`_sqlOperators`（11 元素）、`_priorityTable`（10×10 `int[,]`）的**元素顺序、个数、维度全部冻结**；`_priorityTable` 只重排版为每行一行 + 行列对应的 `Operator` 注释。已知 `Operator` 有 12 个值而表是 10×10，靠 `ProcessOperatorStack` 提前分流括号规避——**不扩表**，只加注释说明该约束。
- 一维查找表改集合表达式；`_basicType` / `_stringType` 改 `FrozenSet<Type>`；`_operatorChars` 改 `SearchValues<char>`。
- `GlobalCache._basicType` 与 `SQLGenerator._basicType` 内容不同（前者含 `string`）是有意的行为差异，**不合并**，只各自把线性查找换成 `Contains`。

### D6: 反射去字符串化采用 `typeof(LoadList<>)` + `nameof`，不引入 `ILoadList` 接口

`Assembly.Load("ORMFramework").GetType("ORMFramework.LoadList\`1")` → `typeof(LoadList<>).MakeGenericType(itemType)`，三个属性名改 `nameof(LoadList<object>.Xxx)`。

方案 9.1 提到的"更彻底做法"（加 `internal interface ILoadList` 消除反射设值）本变更**不做**：它改变类型的实现接口列表，属结构改动，且 `LoadList<T>` 路径当前零测试覆盖。先在本变更中补上 `LoadListTest`，接口化留作后续提案。

### D7: `record` / `sealed` / 主构造函数的适用判定

- `record` 只用于 `QueryExpression`。`StatElement`（`ProcessNotOperator` 会写 `element.Value`）、`CacheEntity` / `GlobalCacheEntity`（可变，且后者是 `Monitor` 锁对象——值相等语义与"引用即锁"冲突）、`*Event`（Listener 通过 `Result` 回写）、全部实体类（`SessionCache._objList` 以实体对象为字典键）一律不改。
- `sealed` 只加到无派生类型；`EntityRelation`、`CacheEntity`、`ActionEvent` 有派生类，不加。
- 主构造函数只用于"构造函数仅做字段赋值"的类型；`Session` 的 3 个链式构造函数不改（链式 `: this(...)` 与主构造混用反而更难读）；`PersistenceContext` 两个重载都必须保留（DI 解析依赖，见 Context 第 2 条）。

### D8: 资源释放按"所有权"逐处判定

`using` 只加在**由本方法创建**的资源上。从 `IPersistenceContext.Connection` 取得的连接是共享的（上下文缓存并在 Closed 时重建），加 `using` 会改变语义——`DefaultCommandListener.OnExecute` 中只对 `DataSet` / `IDbCommand` 加 `using`，连接不加；`SessionCache.ProcessQueryCommand` / `GlobalCache.ReadDataFromDatabase` 自己 `GetDbConnection()` 的连接则加。这一区分逐处核对，对应 `session-lifecycle` 规格的"共享连接不被使用方释放"。

删除 `Session` / `SessionCache` 的终结器是行为变更，独立提交并在提交信息说明：进程退出时不再自动归还二级缓存引用计数——但原终结器也不保证运行，实际影响为零。

### D9: `GlobalCache.cs` 的允许操作白名单

对该文件只做以下变换：删冗余 using、file-scoped namespace、`var`、`TryGetValue`、插值字符串 + 不变文化、`catch (X)` 去掉未用变量、`KeyValuePair` 解构、`GetValue(obj)` 单参重载、线性查找改 `FrozenSet.Contains`。

**禁止**：改动任何 `Monitor` / `Thread` / `Interrupt` 语句、反转任何含 `Monitor` 的嵌套、把 `Dictionary` 改 `ConcurrentDictionary`、LINQ 化锁内循环。

例外：13.5（`Collect` 在 `foreach` 中 `Remove`）必须修，因为它是确定性的 `InvalidOperationException`。修法固定为"先收集待删键、再删除"的两段式，不引入 LINQ，且不触碰该方法中的锁语句。

### D10: 排版与风格的一次性提交

`this.` 前缀统一为"不加"（多数文件已如此），代价是 `DefaultDbDriverFactory.cs`、`SessionFactoryIoc.cs`、`PersistenceContext.cs`、`Ioc/*.cs` 产生大块纯格式 diff。这些 diff 与私有字段命名修正合并为**一个独立提交**，不与任何语义变换混合，便于 review 时整体跳过。

## Risks / Trade-offs

- **[语义敏感的表达式重写引入静默回归]**（`StatementAnalyst` 5 个状态转移判定的布尔折叠、`ObjectCalculator` 的 switch 表达式）→ D1 的三类基线快照先行；这些提交排在测试提交之后，回归可 `git bisect run dotnet test` 定位。
- **[`GlobalCache` 的机械变换意外影响锁语义]**（例如 `TryGetValue` 改写恰好跨越 `Monitor.Enter` 与 `Exit` 之间）→ D9 白名单 + 逐处核对 monitor 出入口；涉及该文件的提交额外手工执行 `Program.TestMultitransaction`（需取消注释 + 本地 MySQL），比对两线程读写锁交互输出。
- **[SQL 文本变化被误判为回归]**：D4 的 null 语义与不变文化格式化会真实改变输出 → 该提交单独进行并同步更新快照基线，提交信息列出所有预期变化的文本形态。
- **[NRT 打开后 `required` 属性与反射契约冲突]**：`EntityRelation` / `Configuration` 用 `required` 最优雅，但实体类必须保留 public 无参构造（`Activator.CreateInstance`）→ `required` 只用于由 `ConfigManager` 以对象初始化器构造的配置类型，实体类一律不用 `required`、不用 `init`。
- **[依赖升级带来行为变化]**：`log4net` 3.x 有 API 变动，而 `LogHelper` 存在固定仓库名重复创建的缺陷（13.15）→ 升级仅保证编译与现有调用面可用；`LogHelper` 的重构留待后续提案，本变更只做 `static class` 化与 `Path.Combine`。
- **[`ConfigManagerTest` 从"预期失败"转为通过]**：`AGENTS.md` 当前把它记为预期失败 → 修好后必须同步更新 `AGENTS.md`，否则后续协作者会按旧描述忽略真实失败。
- **[提交数量多（26 项）导致执行拖长]** → 接受。这是"单一关注点提交"原则的成本，换来的是可 revert / 可 bisect；前 4 项之后每项都可独立停下。

## Migration Plan

按方案 16.1 的 26 项提交序列执行，关键次序约束：

1. 工程基础设施（net8 + `Directory.Build.props` + `.editorconfig`，`Nullable=annotations`）。
2. 两个阻塞缺陷（13.1、13.2）。
3. 回归测试安全网（含 `ConfigManagerTest` 路径夹具化）。
4. 机械化变换（file-scoped namespace → using 清理 → `new`/`readonly`/`var` → 风格与命名统一）。
5. 声明现代化（自动属性 → 主构造/`sealed`/`static class` → `LoadList` 守卫收敛）。
6. 控制流与模式匹配（switch 表达式 → `SqlLiteral` 抽取 → 模式匹配 → `ConfigManager` 卫语句化）。
7. 集合/字符串/API（`TryGetValue` → 集合表达式与 `FrozenSet`/`SearchValues` → 插值字符串 → LINQ/解构/`TryParse`）。
8. 资源与异常（`using` 声明 + 移除终结器 → 自定义异常类型）。
9. 反射去字符串化 → 切换 `Nullable=enable`（按目录 7 个子提交）。
10. 测试样板与死代码清理 → 其余既有缺陷逐项修复 → 同步更新 `AGENTS.md` 与 README（含"不防注入"声明）。

每次提交的验证门槛：`dotnet format --verify-no-changes SchoolEndORM.sln`、`dotnet build SchoolEndORM.sln -warnaserror`、`dotnet test`。涉及 SQL 生成的提交额外做文本快照逐字节比对；涉及 `GlobalCache` 的提交额外手工执行多事务示例。

**回滚**：每个提交独立可 `git revert`；表达式重写类提交若出现回归，用 `git bisect run dotnet test` 定位。工程基础设施提交（net8 升级）如需回滚，需连带回滚所有依赖 C# 12 语法的后续提交，因此它必须是序列第一项且此后不再变动。
