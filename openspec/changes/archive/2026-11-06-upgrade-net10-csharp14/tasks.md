## 1. 环境与升级前基线

- [x] 1.1 记录当前 SDK 状态：运行 `dotnet --list-sdks`，确认本机仅有 `8.0.133`，把输出留存到 `/tmp/net10-sdks-before.txt`；该清单供 2.1 挑选 `global.json` 的 10.0.1xx 版本时对照（design D2 前置条件）
- [x] 1.2 安装 .NET 10 SDK（10.0.1xx Feature Band 稳定版，用官方安装器或 `dotnet-install.sh --channel 10.0.1xx`）；运行 `dotnet --list-sdks` 确认出现 `10.x` 条目，输出追加到 `/tmp/net10-sdks-before.txt`
- [x] 1.3 在仓库根运行 `dotnet --version`，确认输出 `10.x`；若 1.2 无法安装或本步仍输出 `8.x`，**立即停止并向用户报告**，不在未验证状态下继续（design Risks 首条：不允许"未验证即宣称完成"）
- [x] 1.4 记录回滚起点：运行 `git status --short` 与 `git log --oneline -1`，把 commit 哈希写入 `/tmp/net10-rollback-point.txt`；确认除 `.gitignore` 既有改动与本变更目录外无其他源码改动（design D5 步骤 1 要求干净工作树）
- [x] 1.5 记录升级前构建基线：`dotnet build SchoolEndORM.sln 2>&1 | tee /tmp/net10-before-build.txt`，确认全绿且三个项目产物落在 `bin/Debug/net8.0`
- [x] 1.6 记录升级前测试基线：`dotnet test ORMFramework.Test/ORMFramework.Test.csproj 2>&1 | tee /tmp/net10-before-test.txt`，确认全绿，并从输出摘录"通过用例总数"写入 `/tmp/net10-case-count-before.txt`（2.15 / 3.14 / 6.6 按此比对）
- [x] 1.7 复制 SQL 快照基线：`cp docs/baselines/sql-snapshot.txt /tmp/sql-snapshot-before.txt`，确认副本含 5 行（无 where 的 `select`、带 where 的 `select`、`insert`、`update`、`delete`）
- [x] 1.8 运行 `git diff --exit-code -- docs/baselines/sql-snapshot.txt`，退出码为 0，确认基线文件本身未被改动（design D8 白名单）
- [x] 1.9 运行 C# 14 兼容性扫描 `rg -n '\b(field|extension)\b|\.Reverse\(|is not .* or ' ORMFramework ORMFramework.Examples ORMFramework.Test --glob '*.cs' --glob '!**/obj/**' 2>&1 | tee /tmp/net10-csharp14-scan-before.txt`，确认零命中（无 `field`/`extension` 标识符、无 `.Reverse(`、无 `is not ... or ...`），作为 2.16 / 4.6 / 6.7 的升级后对照基准
- [x] 1.10 在 `/tmp/net10-csharp14-scan-before.txt` 末尾写入"零命中"结论与扫描日期；本机若无 `rg`，改用文末"通用约定"给出的 `grep -rnE` 等价命令，结果同样落盘
- [x] 1.11 基线组复核：确认 `/tmp/net10-sdks-before.txt`、`/tmp/net10-rollback-point.txt`、`/tmp/net10-before-build.txt`、`/tmp/net10-before-test.txt`、`/tmp/net10-case-count-before.txt`、`/tmp/sql-snapshot-before.txt`、`/tmp/net10-csharp14-scan-before.txt` 七个文件齐全且非空，再进入第 2 组；此后每组只比对、不改基线

## 2. .NET 10 基础设施与依赖基线（提交 #1，回滚边界）

- [x] 2.1 新建仓库根 `global.json`：`{"sdk": {"version": "<1.2 中实际安装的 10.0.1xx 版本>", "rollForward": "latestFeature", "allowPrerelease": false}}`；在仓库根运行 `dotnet --version` 确认输出 `10.x` 且版本由该文件解析（与 1.1/1.2 的 SDK 清单对照），`rollForward` 不用 `disable`、版本不钉死补丁号之外的值（design D2 采用/被否两栏）
- [x] 2.2 修改 `Directory.Build.props`：`<TargetFramework>net8.0</TargetFramework>` 改 `net10.0`、`<LangVersion>12.0</LangVersion>` 改 `14.0`，`Nullable` / `ImplicitUsings` / `EnableNETAnalyzers` / `AnalysisLevel` / `EnforceCodeStyleInBuild` / `GenerateDocumentationFile` 六项原值不动；运行 `dotnet build SchoolEndORM.sln`，确认三个项目产物落在 `bin/Debug/net10.0` 且 `*.runtimeconfig.json` 的 `tfm` 为 `net10.0`（此步允许旧包产生 NU1701 警告，2.5–2.9 消除）
- [x] 2.3 检查三个 `.csproj` 均不重复声明 `TargetFramework` / `LangVersion` / `ImplicitUsings` / `Nullable` / 分析器开关，且 `ORMFramework.Examples.csproj` 的 `<StartupObject>ORMFramework.Examples.Program</StartupObject>` 与 `Config.xml` 复制项、`ORMFramework.Test.csproj` 的 `TestData\Config.xml` 复制项均保留（对应 `build-configuration` 的"单个项目不重复声明公共属性"）
- [x] 2.4 检查 `global.json` 与 `Directory.Build.props` 匹配：SDK 主版本为 10、`TargetFramework=net10.0`、`LangVersion` 未被写成低于 14 的值（对应 `build-configuration` 的"SDK 与目标框架匹配"）
- [x] 2.5 升级 `ORMFramework/ORMFramework.csproj`：`Microsoft.Extensions.DependencyInjection` 7.0.0 → 10.x、`log4net` 3.0.4 → 3.x 最新稳定版；运行 `dotnet restore SchoolEndORM.sln` 确认无 NU1701 / NU190x，`dotnet build ORMFramework/ORMFramework.csproj` 通过且 `LogHelper/LogHelper.cs` 的 `LogManager.CreateRepository` / `XmlConfigurator.Configure` / `LogManager.GetLogger` 调用面仍编译（只做最小签名适配，不重构 `LogHelper`，design D3 适配原则）
- [x] 2.6 复核 2.5 的还原结果：`dotnet restore SchoolEndORM.sln` 输出中 `grep -E 'NU1701|NU190'` 为零命中；`ORMFramework/bin/Debug/net10.0/ORMFramework.runtimeconfig.json` 的 `tfm` 为 `net10.0`
- [x] 2.7 升级 `ORMFramework.Examples/ORMFramework.Examples.csproj`：`MySql.Data` 9.1.0 → 9.x 最新稳定版
- [x] 2.8 验证 2.7：`dotnet build ORMFramework.Examples` 通过，输出目录仍含 `Config.xml`、`<StartupObject>` 未丢失，且 `SessionFactory/DefaultDbDriverFactory.cs` 的 `DbProviderFactories.RegisterFactory` 路径仍编译（design D3 适配原则）
- [x] 2.9 升级 `ORMFramework.Test/ORMFramework.Test.csproj`：`Microsoft.NET.Test.Sdk` 17.5.0 → 18.x、`MSTest.TestAdapter` / `MSTest.TestFramework` 3.6.0 → 4.x、`coverlet.collector` 3.2.0 → 6.x（保留 `coverlet.collector` 的 `IncludeAssets` / `PrivateAssets` 子元素）
- [x] 2.10 确认预期红灯：运行 `dotnet build ORMFramework.Test/ORMFramework.Test.csproj -warnaserror`，**确认失败**，失败原因为 13 处 `Assert.ThrowsException` 在 MSTest 4 已删除（MSTEST0039 / v4 破坏性变更）；这是编译必需的预期失败，不是回归，不得用"绕过编译"的方式消除（design D3 关键决策 2）
- [x] 2.11 改 `ORMFramework.Test/StatementAnalystTest.cs:68,75,83,90,97,104,111`：7 处 `Assert.ThrowsException<T>` → `Assert.ThrowsExactly<T>`，逐处保留 `ex.ParamName == "value"` 与 `ex.Message` 断言（`") is expected"`、`"' is expected"`、`"Invaild Operator:=<"`、`"Invalid Vairable:1x"`）
- [x] 2.12 改 `ORMFramework.Test/ObjectCalculatorTest.cs:73,74,87,88,107` 的 5 处与 `ORMFramework.Test/SessionCacheTest.cs:120` 的 1 处 `Assert.ThrowsException<T>` → `Assert.ThrowsExactly<T>`，保留 `InvalidCastException` / `OrmConfigurationException` 类型与 `ex.Message` 子串断言
- [x] 2.13 运行 `dotnet build ORMFramework.Test/ORMFramework.Test.csproj -warnaserror` 确认通过；用 `rg -n 'Assert\.ThrowsException' ORMFramework.Test --glob '*.cs'` 确认 13 处全部改名、零残留
- [x] 2.14 选择并固定测试宿主：按 2.13 后 `dotnet test` 的实际运行结果决定启用 MTP 的 dotnet test 桥接还是 VSTest 兼容模式，判据是"三条命令按原样工作"；显式保留 `Microsoft.NET.Test.Sdk` 引用，**不**把测试项目重写为 MTP 原生项目文件、**不**引入 `MSTest.Sdk`（design D3 关键决策 1）
- [x] 2.15 定义并验证三条 `dotnet test` 命令：`dotnet test ORMFramework.Test/ORMFramework.Test.csproj`、`dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~StatementAnalystTest`、`dotnet test ORMFramework.Test/ORMFramework.Test.csproj --collect:"XPlat Code Coverage"` 按原样全绿，用例总数与 1.6 的 `/tmp/net10-case-count-before.txt` 一致；此三条命令为后续 3.14 / 6.6 的固定复跑集
- [x] 2.16 重新运行 1.9 的扫描收集 .NET 10 SDK 与 MSTest 4 带来的全部新告警：`dotnet build SchoolEndORM.sln -warnaserror`，把告警清单写入 `/tmp/net10-warnings.txt`，与 1.9 的零命中基线对照（design D7）
- [x] 2.17 就地修复 2.16 中可修且不改行为的告警：Span 重载解析歧义加显式 `.AsSpan()`、`Assert.AreEqual` 泛型推断问题加显式类型实参、冗余 `or` 模式加括号、CS9258 / CS9272 的 `field` / `extension` 标识符冲突；不改任何判定顺序与输出文本
- [x] 2.18 在 `.editorconfig` 中只抑制"与红线冲突"的告警，沿用既有 `CA1716` / `CA1720` / `CA1840` / `CA1859` 的写法为每条 `dotnet_diagnostic.*.severity = none` 加说明抑制理由的注释；**不**启用 `MSTestAnalysisMode=Recommended`、**不**批量抬升 Info 规则（design D3 关键决策 4 / D7）；运行 `rg -n 'MSTestAnalysisMode' ORMFramework.Test` 确认无 `Recommended`
- [x] 2.19 检查 `.editorconfig` 中所有 `severity = none` 条目（既有 4 条与 2.18 新增项）均带说明抑制理由的注释，且理由与项目红线一致（对应 `build-configuration` 的"抑制均有理由"场景）
- [x] 2.20 跑通两道静态门槛：`dotnet build SchoolEndORM.sln -warnaserror` 与 `dotnet format --verify-no-changes SchoolEndORM.sln` 均退出码 0、零警告（design Migration Plan 固定门槛）
- [x] 2.21 检查 6 个 `PackageReference` 在 `obj/project.assets.json` 中均提供 `net10.0` 兼容 target 资产，而非仅通过 `netstandard` 回退解析（对应 `build-configuration` 的"包资产覆盖 net10.0"场景）
- [x] 2.22 运行全量 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 与 1.6 基线比对，确认 `ConfigManagerTest`、`ComponentProviderTest`、`SQLGeneratorTest` 等结果一致、用例总数一致（DI 10.x 用两条工厂路径的既有测试验证，design D3）
- [x] 2.23 运行 `diff /tmp/sql-snapshot-before.txt docs/baselines/sql-snapshot.txt` 零差异，并 `git diff --exit-code -- docs/baselines/sql-snapshot.txt` 确认基线文件未被改动
- [x] 2.24 更新 `AGENTS.md`：目标框架 `net8.0` → `net10.0`、语言版本 C# 12 → C# 14，补 `global.json` 的 SDK 固定策略、最低运行时 .NET 10、依赖版本（DI 10.x / Test.Sdk 18.x / MSTest 4.x / coverlet 6.x / MySql.Data 9.x / log4net 3.x）；核对文档所列 `dotnet build` / `dotnet test` / `--filter` / `--collect` 命令与 2.5–2.23 实际执行的命令一致
- [x] 2.25 更新 `README.md`：在"安全边界"之外补一节明示最低运行时为 .NET 10、目标框架 `net10.0` / C# 14，并说明仅装 .NET 8 运行时的下游消费者必须同步升级（proposal 的 BREAKING 项）
- [x] 2.26 提交 `build: 升级到 net10.0 / C# 14 + SDK 固定 + 依赖对齐（含 MSTest 4 编译必需断言改名）`；确认这是唯一的基础设施提交，之后不再改动 TFM / 依赖版本；记录该提交哈希为回滚边界（revert 它必须连带 revert 之后全部提交，design D5）

## 3. MSTest 4 测试套件规范化（提交 #2，必须先于任何 C# 14 语法改写）

- [x] 3.1 改 `ORMFramework.Test/SessionCacheTest.cs:121-122`：2 处 `StringAssert.Contains` → `Assert.Contains`（MSTEST0046），保留 `"MissingProp"` 与 `entityType.FullName!` 两个子串断言；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~SessionCacheTest` 通过
- [x] 3.2 改 `ORMFramework.Test/ConfigManagerTest.cs:56-59`：4 处 `CollectionAssert.Contains` → `Assert.Contains`（MSTEST0068），保留 `RelationType.OneToOne` / `OneToMany` / `ManyToOne` / `MantToMany` 四个实参；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~ConfigManagerTest` 通过
- [x] 3.3 给 10 个测试类加 `sealed`：`SqlLiteralTest`、`SessionCacheTest`、`SessionTest`、`ObjectCalculatorTest`、`ConfigManagerTest`、`OrmExceptionTest`、`LoadListTest`、`SQLGeneratorTest`、`StatementAnalystTest`、`ComponentProviderTest`（均在 `ORMFramework.Test/*.cs`）；运行 `dotnet build SchoolEndORM.sln -warnaserror` 通过
- [x] 3.4 改 `ORMFramework.Test/ComponentProviderTest.cs:16,26` 的 2 处 `Assert.IsInstanceOfType<Session>(session)` / `Assert.IsInstanceOfType<CustomSession>(session)`，改用 v4 返回的强类型实例（不再丢弃返回值）；运行 `dotnet build SchoolEndORM.sln -warnaserror` 通过
- [x] 3.5 改 `ORMFramework.Test/OrmExceptionTest.cs:22` 的 `Assert.IsInstanceOfType(new QuerySyntaxException("x"), typeof(Exception))` 为泛型形式 `Assert.IsInstanceOfType<Exception>(...)`；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~OrmExceptionTest` 通过
- [x] 3.6 把 `ORMFramework.Test/ObjectCalculatorTest.cs` 的 5 个真值表方法（`IsTargetObject_IntComparisons_ReturnExpectedTruthTable`、`_StringComparisons_`、`_DateTimeComparisons_`、`_BoolComparisons_RecordCurrentBehavior`、`_CharComparisons_RecordCurrentBehavior`，共约 40 条 `IsTrue` / `IsFalse`）收敛为 `[TestMethod] + [DataRow(string query, bool expected)]`，逐行保留原期望值与 `// TODO(12.2)` 既有缺陷注释；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~ObjectCalculatorTest` 确认覆盖的断言条数与改造前一致
- [x] 3.7 把 `ORMFramework.Test/StatementAnalystTest.cs` 的 4 个快照方法（`GetQueryExpression_IdEquals1_`、`_AndLike_`、`_NotParenthesized_`、`_MixedExpression_`）收敛为 `[DataRow(query, expectedInfix, expectedSuffix)]`，逐行保留 4 组中缀 / 后缀期望字符串；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~StatementAnalystTest` 确认 8 条序列快照文本与改造前逐字节一致
- [x] 3.8 把 `StatementAnalystTest.cs` 的 7 个异常方法（`QueryString_Empty_`、`_WhitespaceOnly_`、`_Null_`、`_UnbalancedParenthesis_`、`_UnclosedQuote_`、`_UnrecognizedOperator_`、`_InvalidNumericConstant_`）收敛为 `[DataRow(query, Type, message)]`，保留 `ArgumentException` / `ArgumentNullException` / `QuerySyntaxException` 类型与 `"value"` / `") is expected"` / `"' is expected"` / `"Invaild Operator:=<"` / `"Invalid Vairable:1x"` 断言；`[DataRow]` 承载 `Type` / `null` 实参的写法按实际编译结果确定（design Open Question 3）；运行该 filter 全绿
- [x] 3.9 把 `ORMFramework.Test/SqlLiteralTest.cs` 的 9 个单断言方法（7 个 `Format_*` + 2 个 `Comparison_*`）收敛为 `[DataRow(input, expected)]`，保留 `"NULL"` / `"'abc'"` / `"'A'"` / `"'2024-01-02 03:04:05'"` / `"1"` / `"1.5"` / `"True"` / `"Name is null"` / `"Name='abc'"` 全部期望值；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~SqlLiteralTest` 全绿
- [x] 3.10 把 `ORMFramework.Test/ConfigManagerTest.cs` 的 `GetSystemConfiguration_Mappings_HaveExpectedClassAndTableNames` 中 5 条 TableName 断言收敛为 `[DataRow(className, tableName)]`（`Student` / `Department` / `Teacher` / `ContactMenu` / `Course` 五组）；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~ConfigManagerTest` 全绿
- [x] 3.11 确认全测试项目未使用 `[DataTestMethod]`（MSTEST0044）：`rg -n 'DataTestMethod' ORMFramework.Test --glob '*.cs'` 零命中，数据驱动全部落在 `[TestMethod] + [DataRow]`；3.6–3.10 中 `Type` / `null` 实参写法以 `dotnet build -warnaserror` 通过为准（design D3 关键决策 3）
- [x] 3.12 给 `SqlLiteralTest.cs` 中修改 `CultureInfo.CurrentCulture` 的 `Format_Double_ReturnsInvariantNumber_EvenUnderDeDeCulture` 用例加 `[DoNotParallelize]`（MSTEST0076），保留 try/finally 恢复；确认 `MSTestAnalysisMode` 保持默认、未设为 `Recommended`，只修 v4 升为 Warning 的 12 条规则引入的告警
- [x] 3.13 运行 `dotnet build SchoolEndORM.sln -warnaserror` 与 `dotnet format --verify-no-changes SchoolEndORM.sln` 均通过；必要时按 design D7 为 `[DataRow]` 实参下的 `Assert.AreEqual` Span 重载歧义加显式类型实参，不通过修改期望值绕过
- [x] 3.14 逐条复跑 2.15 的三条 `dotnet test` 命令，按原样全绿；核对用例总数与 1.6 的 `/tmp/net10-case-count-before.txt` 一致（v4 的 `TestCase.Id` 重基线不产生用例丢失，design Risks）
- [x] 3.15 运行全量 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 与 1.6 基线比对，`diff /tmp/sql-snapshot-before.txt docs/baselines/sql-snapshot.txt` 零差异；确认测试重构本身未改变任何断言语义后，才进入第 4 组（design D5 步骤 4 / D9：这张网是后续所有语法提交的安全网，若比对有差异必须先修回）
- [x] 3.16 提交 `test: 单元测试重构为 MSTest 4 规范（数据驱动 + sealed + 现代断言）`；该提交单独可 `git revert`，且是后续所有语法提交的回归网

## 4. C# 14 `field` 关键字改写（提交 #3）

- [x] 4.1 改 `ORMFramework/Configuration/EntityClass/EntityMapping.cs`：删除 `_keys` / `_relations` 两个私有字段，`Keys` / `Relations` 改为 `field` 半自动属性（属性初始化器 `= []` / `= new Dictionary<string, EntityRelation>()` 直接写后备字段，`set => field = value ?? [];` 与 `set => field = value ?? new Dictionary<string, EntityRelation>();` 保留"赋 null 后 get 仍返回空集合"）；运行 `dotnet build SchoolEndORM.sln -warnaserror` 无新增 CS86xx，且 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~ConfigManagerTest` 的映射断言通过（design D6 第 1 条）
- [x] 4.2 改 `ORMFramework/Session/PersistenceContext.cs`：删除 `_connection` 字段，`Connection` getter 改用 `field` 判定 `field is null || field.State == ConnectionState.Closed` 后重建，短路顺序与"共享连接不被使用方释放"语义不变；运行 `--filter FullyQualifiedName~SessionTest` 与 `--filter FullyQualifiedName~SessionCacheTest` 通过（design D6 第 2 条）
- [x] 4.3 改 `ORMFramework/Session/LoadList.cs`：删除 `_items` 字段，`Items` 改为 `private List<T> Items => field ??= LoadItems();`；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~LoadListTest` 通过（懒加载只触发一次、谓词文本 `fk=='v1' || fk=='v2'` 不变，design D6 第 3 条）
- [x] 4.4 有限扩面（机械替换，不加运行时 null 守卫、不碰 `required` 属性）：把 `ORMFramework/Cache/CacheEntity.cs` 的 `Value` / `ForeignKeys` 改为 `field` 半自动属性；运行 `--filter FullyQualifiedName~SessionCacheTest` 与 `--filter FullyQualifiedName~ConfigManagerTest` 通过
- [x] 4.5 有限扩面（同 4.4 边界）：把 `ORMFramework/SessionFactory/SessionFactory.cs` 与 `ORMFramework/SessionFactory/SessionFactoryIoc.cs` 的 `Configuration` 改为 `field` 半自动属性；运行 `--filter FullyQualifiedName~ComponentProviderTest` 通过，确认 `SessionFactory` 手工装配与 `SessionFactoryIoc` 的 DI + 事后补配两条路径均覆盖（design D6 第 4 条）
- [x] 4.6 运行 `rg -n '\bfield\b' ORMFramework ORMFramework.Examples ORMFramework.Test --glob '*.cs' --glob '!**/obj/**'`，确认除本次有意引入的关键字用法外无名为 `field` 的成员 / 局部变量，CS9258 / CS9272 零命中，并与 1.9 的零命中基线对照
- [x] 4.7 运行全量 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 与 `diff /tmp/sql-snapshot-before.txt docs/baselines/sql-snapshot.txt`，确认 SQL 快照零差异、中缀 / 后缀序列与真值表逐条一致
- [x] 4.8 在 `docs/modern-csharp-migration-plan.md` 追加一节记录 `field` 关键字的原 4 处落点（`EntityMapping.Keys` / `Relations`、`PersistenceContext.Connection`、`LoadList<T>.Items`）与有限扩面位置（`CacheEntity.Value` / `ForeignKeys`、两个 `SessionFactory.Configuration`）、"属性初始化器直接写后备字段、不经过 setter"的核对结论，以及"扩面只做机械替换、不引入运行时守卫"的边界；核对文档描述与 4.1–4.5 实际代码一致
- [x] 4.9 提交 `refactor: 采用 C# 14 field 关键字替换显式后备字段`

## 5. 其余 C# 14 特性：反射、Span、扩展成员、partial（提交 #4–#6）

- [x] 5.1 做最小编译探针：在 `ORMFramework.Test` 建临时文件写仅含 `private static readonly string Name = nameof(LoadList<>.ForeignKeyName);` 的类型，运行 `dotnet build ORMFramework.Test/ORMFramework.Test.csproj -warnaserror` 验证 C# 14 下 `nameof(LoadList<>.X)` 是否合法，结果写入 `/tmp/net10-nameof-probe.txt` 后立即删除临时文件；不合法则跳过 5.2，把不采用结论写入 5.7 的提交信息与 5.13 的文档（design D4 条件采用 / Open Question 2）
- [x] 5.2 若 5.1 合法，把 `ORMFramework/Cache/SessionCache.cs:97,98,102,114` 的 4 处 `nameof(LoadList<object>.ForeignKeyValues / SessionCache / ForeignKeyName)` 改为 `nameof(LoadList<>.X)`；运行 `dotnet build SchoolEndORM.sln -warnaserror` 通过，且 `--filter FullyQualifiedName~LoadListTest` 与 `--filter FullyQualifiedName~SessionCacheTest` 全绿（反射取到的属性名不变）
- [x] 5.3 在 `ORMFramework/Statment/StatementAnalyst.cs` 的首尾引号剥离 / 字符扫描处改用 `AsSpan()` 切片与 `SearchValues<char>.Contains`，避免分配子字符串；运行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~StatementAnalystTest`，确认中缀 / 后缀序列快照逐条一致（design D4 Span 行）
- [x] 5.4 在 `ORMFramework/Cache/ObjectCalculator.cs` 的首尾引号剥离 / 字符扫描处作同样的 `AsSpan()` 与 `SearchValues<char>.Contains` 改写；运行 `--filter FullyQualifiedName~ObjectCalculatorTest`，确认真值表逐条一致
- [x] 5.5 在 `ORMFramework/Session/LoadList.cs` 的 `LoadItems` 拼接处改用 `Append(ReadOnlySpan<char>)`（不改 `SqlLiteral.Format` 的格式化逻辑）；运行 `--filter FullyQualifiedName~LoadListTest` 通过（谓词文本不变）
- [x] 5.6 在 `ORMFramework/SQLGenerator/SQLGenerator.cs` 的 `StringBuilder` 拼接处改用 `Append(ReadOnlySpan<char>)`；运行 `--filter FullyQualifiedName~SQLGeneratorTest` 通过，且 `diff /tmp/sql-snapshot-before.txt docs/baselines/sql-snapshot.txt` 逐字节零差异
- [x] 5.7 提交 `refactor: nameof 非绑定泛型与 Span 一等公民转换`（若 5.1 不合法，在本提交信息中说明不采用理由，不产生空提交）
- [x] 5.8 扩展成员 I：把 `ORMFramework/SQL/SqlLiteral.cs` 的 `Format(object?)` / `Comparison(string, object?)` 改为 `extension` 块（分别在 `object?` 与 `string` 接收者上，纯转发、无状态），更新 `ORMFramework/SQLGenerator/SQLGenerator.cs`、`ORMFramework/Cache/SessionCache.cs`、`ORMFramework/Cache/GlobalCache.cs`、`ORMFramework/Session/LoadList.cs`、`ORMFramework.Test/SqlLiteralTest.cs` 约 5 个文件 / 15 个调用点；运行 `--filter FullyQualifiedName~SqlLiteralTest` 与 `--filter FullyQualifiedName~SQLGeneratorTest` 全绿，且 SQL 快照逐字节零差异（design D4 主要新增落点）
- [x] 5.9 扩展成员 II：为 `StringBuilder` 增加一个 SQL 片段扩展方法，收拢 `LoadList.cs:67-72`、`SessionCache.cs:69-71`、`SQLGenerator.cs` 中重复的 `Append(col).Append("==").Append(...)` 序列（接口见文末"跨任务接口约定"）；运行 `--filter FullyQualifiedName~LoadListTest` 通过（谓词文本不变）、SQL 快照零差异
- [x] 5.10 提交 `refactor: 采用 C# 14 扩展成员`
- [x] 5.11 partial 构造：把 `ORMFramework/Configuration/ConfigManager.cs`（188 行）按解析阶段拆为 partial 并使用 partial 构造（**不拆** `Statment/StatementAnalyst.cs` 与 `Cache/GlobalCache.cs`）；运行 `--filter FullyQualifiedName~ConfigManagerTest` 全绿，确认 `Config.xml` 的 `SessionFactory` → `Mappings` 解析顺序与未知元素报错行为不变（design D4 partial 行 / 红线）
- [x] 5.12 测试项目 C# 14 沙盒：`ORMFramework.Test/StatementAnalystTest.cs` 的 `Render` 改用 `ReadOnlySpan<char>` / `SearchValues<char>` 风格辅助方法，`ORMFramework.Test/Fakes/FakeDb.cs` 等 `FakeDb*` 假件用 `field` 与空条件赋值，`double.TryParse` 委托用 lambda 参数修饰符；运行 2.20 的两道静态门槛与全量 `dotnet test`，确认序列快照与 SQL 快照零差异（design D9：新增特性首选落点是测试项目）
- [x] 5.13 更新 `docs/modern-csharp-migration-plan.md`：追加 C# 14 特性采用矩阵（采用 / 条件采用 / 不采用及理由，含用户自定义复合赋值、file-based app 指令、`params` 集合、`\e`、`System.Threading.Lock` 仍不采用），标注 net8 → net10 升级完成状态，并确认 §16.4 的 8 项后续提案仍为"未实施"；核对文档与 `design.md` D4 及 5.1–5.12 实际代码一致
- [x] 5.14 提交 `refactor: partial 构造 + 测试项目 C# 14 沙盒 + 采用矩阵记录`

## 6. 全量集成验证与收尾

- [x] 6.1 依次运行四道门槛 `dotnet restore SchoolEndORM.sln`、`dotnet format --verify-no-changes SchoolEndORM.sln`、`dotnet build SchoolEndORM.sln -warnaserror`、`dotnet test ORMFramework.Test/ORMFramework.Test.csproj`，全部退出码 0 且无任何警告（design Migration Plan 固定门槛）
- [x] 6.2 与 1.6 / 1.7 基线做最终比对：SQL 文本快照逐字节一致、中缀 / 后缀序列快照与 `ObjectCalculatorTest` 真值表一致、`Config.xml` 解析断言一致；运行 `git diff --exit-code -- docs/baselines/sql-snapshot.txt` 确认基线文件未被改动（design D8：差异一律判回归并回滚，不允许改期望值）
- [x] 6.3 运行 `dotnet publish ORMFramework.Examples -c Release -o /tmp/schoolendorm-net10-dist`，确认产物 `ORMFramework.Examples.runtimeconfig.json` 的 `tfm` 为 `net10.0`、framework 版本为 `10.0.x`，且输出目录包含 `Config.xml`（design Migration Plan 部署项）
- [x] 6.4 检查三个项目编译产物中的 `*.runtimeconfig.json`：其 `tfm` 均为 `net10.0`、framework 版本为 `10.0.x`（对应 `build-configuration` 的"运行配置声明 net10.0"）；确认仅装 .NET 8 运行时的机器不会静默降级，BREAKING 已在 2.25 的 `README.md` 明示
- [x] 6.5 核对 `git log --oneline` 与 2.26 / 3.16 / 4.9 / 5.7 / 5.10 / 5.14 的提交序列一致、每个提交可独立 `git revert`；运行 `openspec validate "upgrade-net10-csharp14" --strict` 确认本变更规格校验通过
- [x] 6.6 验证 MSTest 4 规范落地：`rg -n 'Assert\.ThrowsException|StringAssert|CollectionAssert|DataTestMethod' ORMFramework.Test --glob '*.cs'` 零命中、10 个测试类均为 `sealed`、`MSTestAnalysisMode` 未设为 `Recommended`；重跑 2.15 的三条 `dotnet test` 命令并确认用例总数与 1.6 基线一致
- [x] 6.7 重跑 1.9 的 C# 14 兼容性扫描并与 `/tmp/net10-csharp14-scan-before.txt` 对照：除本次有意引入的 `field` / `extension` 关键字用法外无意外命中；运行 `rg -n '\b(field|extension)\b' ORMFramework ORMFramework.Examples ORMFramework.Test --glob '*.cs' --glob '!**/obj/**'` 确认 CS9258 / CS9272 零命中
- [x] 6.8 确认 `ORMFramework/Cache/GlobalCache.cs` 在本次升级中除"最小告警修复或有理由抑制"外无任何语句改动：`git diff -- ORMFramework/Cache/GlobalCache.cs` 为空或仅含注释 / 抑制，不含任何 `Monitor` / `Thread` / `Interrupt` 变更（design D4 / D5 红线与 Risks）
- [x] 6.9 确认原 47 项任务（已按文末"原任务映射"展开为更细粒度任务）全部完成、`docs/baselines/sql-snapshot.txt` 未被改动，并运行 `openspec status --change upgrade-net10-csharp14` 确认 `4/4 artifacts complete`
- [ ] 6.10 （需可达 MySQL；环境不具备时在提交信息中标注未执行，不以未执行的检查冒充通过）临时取消 `ORMFramework.Examples/Program.cs` 中 `TestMultitransaction` / `TestOneToMany` / `TestManyToMany` 的注释并运行 `dotnet run --project ORMFramework.Examples`，确认并发与懒加载行为与升级前一致，随后恢复注释状态（design Risks 末条） （未执行：配置的 MySQL 为远程主机，运行写事务属外部破坏性副作用且 Program.cs 阻塞于 Console.ReadKey()；按 design Risks 记为未执行，未冒充通过，见 ledger R28）

## 7. Workflow follow-up

- [ ] 7.1 实现并验证通过后，由用户显式启动归档流程，把 `build-configuration` 的 delta 同步进 `openspec/specs/build-configuration/spec.md`（不擅自归档）
- [x] 7.2 归档前复核 `README.md` / `AGENTS.md` 中的最低运行时与依赖记述已随本次变更更新（与 2.24 / 2.25 的改动一致）

---

### 计划元信息

**Goal:** 让 `net10.0` / C# 14 / 固定 .NET 10 SDK / 对齐的依赖基线成为可由 `dotnet restore`、`dotnet build -warnaserror`、`dotnet test` 验证的事实，把测试套件重构到 MSTest 4 规范，并在"语义等价 + 可审阅的小规模改写"边界内采用 C# 14 特性。

**Architecture:** `Directory.Build.props` 一处声明 TFM / 语言版本，`global.json` 固定 SDK；三个 `.csproj` 只保留各自特有的输出类型、包引用与项目引用。提交序列为"升级前基线 → 基础设施 → 测试网 → 分组语法改写 → 收尾"，每组独立可 `git revert`；SQL 文本快照、中缀 / 后缀序列快照、`ObjectCalculator` 真值表、`ConfigManager` 配置断言四张网逐字节 / 逐元素比对作为零回归判据。

**Tech Stack:** .NET 10 SDK（10.0.1xx）、C# 14、MSTest 4、`Microsoft.NET.Test.Sdk` 18.x、`coverlet.collector` 6.x、`Microsoft.Extensions.DependencyInjection` 10.x、`MySql.Data` 9.x、`log4net` 3.x；测试宿主为 MTP 桥接或 VSTest 兼容模式，由 2.14 的实际运行结果决定。

**Spec:** `openspec/changes/upgrade-net10-csharp14/proposal.md`、`design.md`、`specs/build-configuration/spec.md`；回归基线 `docs/baselines/sql-snapshot.txt`；上一变更经验 `docs/modern-csharp-migration-plan.md`。执行者需同时读 proposal / design / 本文件；遇到设计取舍以 `design.md` 的 D1–D9 决策为准，不猜测。

### 全局约束（Global Constraints）

**C1.** 目标框架 `net10.0`、语言版本 `14.0`，MUST 在仓库根 `Directory.Build.props` 统一声明一次；`Nullable=enable`、`ImplicitUsings=enable`、`EnableNETAnalyzers=true`、`AnalysisLevel=latest-recommended`、`EnforceCodeStyleInBuild=true`、`GenerateDocumentationFile=false` 保持不变。

**C2.** `global.json` MUST 指定 10.0.1xx Feature Band 版本、`rollForward: latestFeature`、`allowPrerelease: false`；最低运行时要求为 .NET 10，仅装 .NET 8 的下游消费者必须同步升级（BREAKING）。

**C3.** 6 个 `PackageReference` MUST 使用声明支持 `net10.0` 的稳定版本；`dotnet restore SchoolEndORM.sln` MUST 不产生 NU1701（框架资产回退）或 NU190x（已知漏洞）警告，出现即换版本或换包，不抑制。

**C4.** `dotnet build SchoolEndORM.sln -warnaserror`、`dotnet format --verify-no-changes SchoolEndORM.sln`、`dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 三道门槛 MUST 全绿、零警告；`--filter FullyQualifiedName~StatementAnalystTest` 与 `--collect:"XPlat Code Coverage"` 按 `AGENTS.md` 原样可用。

**C5.** **不**启用 `MSTestAnalysisMode=Recommended`、**不**引入新分析器规则集、**不**把测试项目重写为 MTP 原生项目文件；只修 v4 升为 Warning 的 12 条规则与已删除 API 引入的告警。

**C6.** 语义等价硬边界：四类 SQL 文本、中缀 / 后缀序列快照、`ObjectCalculator` 真值表、`ConfigManager` 映射断言、`LoadList` 懒加载只触发一次、异常类型层次（`OrmException` 派生）、幂等 `Dispose`、`Config.xml` 解析结果、公开 API 名称与拼写 100% 不变；`docs/baselines/sql-snapshot.txt` 任何一行都不改。

**C7.** 红线：`Cache/GlobalCache.cs` 的 `Monitor` / `Thread.Interrupt` 锁模型；`Operator` 枚举顺序与 `_sqlOperators` / `_priorityTable` 索引对齐；拼串 SQL（不参数化）；`Config.xml` 格式与 `ConfigManager` 解析方式；`Statment` 命名空间；公开 API 拼写（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）；实体 public 无参构造与 `IList<T>` 导航属性声明。

**C8.** 提交粒度：每个提交只承载一类变换，提交信息带 `build:` / `test:` / `refactor:` / `docs:` 前缀；提交序列与回滚边界见"通用约定"。

### 评审关注点（Review Focus）

| 规格隐含、但最可能咬人的输入 / 条件 | 合理预期 | 钉住它的任务 |
| --- | --- | --- |
| 机器上装有多个 SDK，或只装了 10.0.2xx | `dotnet --version` 仍由 `global.json` 选到 10.x 主版本，不挑最新、不失败 | 2.1 的 `dotnet --version` 与 1.1/1.2 SDK 清单对照；6.4 的 runtimeconfig 复核 |
| 某包在 `obj/project.assets.json` 仅提供 `netstandard` 资产 | 判为回退兼容，换版本或换包，绝不抑制 NU1701 | 2.5 / 2.7 / 2.9 的 `dotnet restore` 零 NU 警告；2.21 的资产检查 |
| MSTest 4 切换测试宿主后 `--filter` / `--collect` 语义变化 | 三条命令按原样全绿、用例总数不减 | 2.15 定义并首验、3.14 与 6.6 复跑三条 `dotnet test` 命令 |
| 源码里出现名为 `field` / `extension` 的成员或局部变量 | 编译期 CS9258 / CS9272 必须零命中，改写前先扫描 | 1.9 零命中基线；4.6 与 6.7 的 `rg` 复核 |
| Span / 扩展成员改写产生不可见的文本差异（culture、引号、切片边界） | `docs/baselines/sql-snapshot.txt` 逐字节不变，差异即回滚而不是改基线 | 5.6 / 5.8 / 5.9 的 SQL 快照比对；6.2 最终比对 |

### 通用约定

**每次提交的验证门槛**（design Migration Plan，2.20 / 3.13 / 5.12 / 6.1 引用）：

```bash
dotnet restore SchoolEndORM.sln
dotnet format --verify-no-changes SchoolEndORM.sln
dotnet build SchoolEndORM.sln -warnaserror
dotnet test ORMFramework.Test/ORMFramework.Test.csproj
```

涉及 SQL / 解析 / 求值的提交额外做 `diff /tmp/sql-snapshot-before.txt docs/baselines/sql-snapshot.txt` 与序列快照 / 真值表比对；涉及依赖、测试宿主与数据驱动的提交额外复跑下列三条命令，用例总数与 1.6 基线一致。

**三条 `dotnet test` 命令**（2.15 首次定义，3.14 / 6.6 复跑）：

```bash
dotnet test ORMFramework.Test/ORMFramework.Test.csproj
dotnet test ORMFramework.Test/ORMFramework.Test.csproj --filter FullyQualifiedName~StatementAnalystTest
dotnet test ORMFramework.Test/ORMFramework.Test.csproj --collect:"XPlat Code Coverage"
```

**本机无 `rg` 时的等价扫描**（1.9 / 4.6 / 6.7 / 3.11 / 6.6 通用）：把 `rg -n 'PATTERN' DIR --glob '*.cs' --glob '!**/obj/**'` 换成 `grep -rnE 'PATTERN' DIR --include='*.cs' | grep -v '/obj/'`，排除 `obj/` 生成物。

**提交序列与回滚边界**（design D5）：#1 = 2.26（SDK / TFM / 依赖 + 13 处编译必需断言改名）→ #2 = 3.16（MSTest 4 测试网规范化）→ #3 = 4.9（`field`）→ #4 = 5.7（`nameof` / Span）→ #5 = 5.10（扩展成员）→ #6 = 5.14（partial / 测试沙盒 / 采用矩阵）→ 收尾 6.1–6.10。每个提交可独立 `git revert`；出现回归用 `git bisect run dotnet test` 定位。回滚 #1 必须连带回滚其后全部提交；#2 的快照比对出现差异必须先修回再进入 #3。

**`docs/baselines/sql-snapshot.txt` 白名单**：任何提交都不得修改该文件任何一行；出现差异一律判为回归并回滚，不允许通过更新期望值 / 基线来"通过"（design D8）。`Cache/GlobalCache.cs` 白名单：本次只允许"最小告警修复或有理由抑制"，禁止改动任何 `Monitor` / `Thread` / `Interrupt` 语句、禁止反转嵌套、禁止改 `ConcurrentDictionary`、禁止 LINQ 化锁内循环；`Statment/StatementAnalyst.cs` 只做快照守卫下的局部改写，不拆 partial。

**不采用清单**（design D4，5.13 记录理由）：`System.Threading.Lock`、用户自定义复合赋值、file-based app 指令、`params` 集合、`\e`、隐式索引初始化器、枚举 / 索引对齐改动、`MSTestAnalysisMode=Recommended`；理由均为"无可审阅落点 / 需独立并发回归测试 / 超出本次范围"。

### 跨任务接口约定

`SqlLiteral.Format(object?) -> string` 与 `SqlLiteral.Comparison(string, object?) -> string` 在 5.8 之前保持 `internal static class SqlLiteral` 的静态方法调用面；5.8 起改为 `object?` 与 `string` 接收者上的 `extension` 块，调用点形如 `value.Format()` 与 `column.Comparison(value)`，均为纯转发、无状态（design D4）。

5.9 为 `StringBuilder` 新增的扩展方法统一命名为 `AppendComparison(this StringBuilder builder, string column, object? value)`，签名须同时服务 `LoadList.cs:67-72`、`SessionCache.cs:69-71` 与 `SQLGenerator.cs` 三处调用点；5.5 / 5.6 的 `Append(ReadOnlySpan<char>)` 改写先于 5.9 完成，5.9 只收拢调用点、不改拼接顺序。

`nameof(LoadList<>.X)`（5.2，条件采用）与 `nameof(LoadList<object>.X)` 必须返回同一属性名字符串，5.1 的探针结果决定 5.2 是否执行；`private List<T> Items => field ??= LoadItems();`（4.3）是 5.5 改写 `LoadItems` 内部拼接的前提，4.3 未落地前不得动 `LoadList` 的 `StringBuilder`。

### 原任务映射

| 原任务 | 新任务 |
| --- | --- |
| 1.1 | 1.1, 1.2, 1.3 |
| 1.2 | 1.4, 1.5, 1.6, 1.11 |
| 1.3 | 1.7, 1.8 |
| 1.4 | 1.9, 1.10 |
| 2.1 | 2.1, 2.4 |
| 2.2 | 2.2, 2.3 |
| 2.3 | 2.5, 2.6, 2.21 |
| 2.4 | 2.7, 2.8 |
| 2.5 | 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 2.15 |
| 2.6 | 2.16, 2.17, 2.18, 2.19, 2.20 |
| 2.7 | 2.22, 2.23 |
| 2.8 | 2.24, 2.25 |
| 2.9 | 2.26 |
| 3.1 | 3.1, 3.2 |
| 3.2 | 3.3, 3.4, 3.5 |
| 3.3 | 3.6 |
| 3.4 | 3.7, 3.8, 3.9, 3.10, 3.11 |
| 3.5 | 3.12, 3.13 |
| 3.6 | 3.14 |
| 3.7 | 3.15 |
| 3.8 | 3.16 |
| 4.1 | 4.1 |
| 4.2 | 4.2 |
| 4.3 | 4.3 |
| 4.4 | 4.4, 4.5 |
| 4.5 | 4.6, 4.7 |
| 4.6 | 4.8 |
| 4.7 | 4.9 |
| 5.1 | 5.1 |
| 5.2 | 5.2 |
| 5.3 | 5.3, 5.4 |
| 5.4 | 5.5, 5.6 |
| 5.5 | 5.7 |
| 5.6 | 5.8 |
| 5.7 | 5.9 |
| 5.8 | 5.10 |
| 5.9 | 5.11 |
| 5.10 | 5.12 |
| 5.11 | 5.13 |
| 5.12 | 5.14 |
| 6.1 | 6.1 |
| 6.2 | 6.2 |
| 6.3 | 6.3, 6.4 |
| 6.4 | 6.5 |
| 6.5 | 6.6, 6.7 |
| 6.6 | 6.8, 6.9 |
| 6.7 | 6.10 |
| Workflow ① | 7.1 |
| Workflow ② | 7.2 |
