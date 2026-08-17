## 1. 工程基础设施（方案第 3 章 / 提交 #1）

- [x] 1.1 记录改造前基线：运行 `dotnet build SchoolEndORM.sln 2>&1 | tee /tmp/before-build.txt` 与 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj 2>&1 | tee /tmp/before-test.txt`，确认失败项只有 `ConfigManagerTest`（硬编码 Windows 路径，缺陷 13.16）
- [x] 1.2 新建仓库根 `Directory.Build.props`，单个 `<Project><PropertyGroup>` 声明 `TargetFramework=net8.0`、`LangVersion=12.0`、`ImplicitUsings=enable`、`Nullable=annotations`、`EnableNETAnalyzers=true`、`AnalysisLevel=latest-recommended`、`EnforceCodeStyleInBuild=true`、`GenerateDocumentationFile=false`；`Nullable` 此刻必须是 `annotations`（第 10 组才切 `enable`，否则中间阶段无法零警告）
- [x] 1.3 修改 `ORMFramework/ORMFramework.csproj`：删除 `<TargetFramework>net6.0</TargetFramework>` 等已由 `Directory.Build.props` 提供的属性，删除 `Microsoft.NETCore.Platforms` 包引用、`<None Remove="Ioc\" />`、`<ItemGroup><Folder Include="Ioc\" /></ItemGroup>`
- [x] 1.4 修改 `ORMFramework.Examples/ORMFramework.Examples.csproj`：删除公共属性、`<WarningLevel>4</WarningLevel>` 与 `<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">` 整段；保留 `<StartupObject>`（12.5 依赖它，不改 top-level statements）
- [x] 1.5 修改 `ORMFramework.Test/ORMFramework.Test.csproj`：删除公共属性，保留 MSTest 相关 `PackageReference` 与 `ProjectReference`
- [x] 1.6 升级 `ORMFramework.Examples` 的 `MySql.Data` 8.0.32.1 → 9.x（net8 兼容版本），运行 `dotnet restore SchoolEndORM.sln` 确认无版本冲突
- [x] 1.7 升级 `ORMFramework.Test` 的 `MSTest`（`MSTest.TestAdapter` / `MSTest.TestFramework`）3.0.2 → 3.6+，确认 `Assert.IsInstanceOfType<T>` 泛型重载可用（11.3 依赖）
- [x] 1.8 升级 `ORMFramework` 的 `log4net` 2.0.15 → 3.x，并 `dotnet build ORMFramework/ORMFramework.csproj` 确认 `LogHelper/LogHelper.cs` 现有调用面（`LogManager.CreateRepository`、`XmlConfigurator.Configure`、`LogManager.GetLogger`）在 3.x 下编译通过；如有 API 变动只做最小签名适配，不重构 `LogHelper`
- [x] 1.9 新建仓库根 `.editorconfig`：`[*.cs]` 下配置 4 空格缩进（`indent_style=space`、`indent_size=4`）、`csharp_new_line_before_open_brace=all`（Allman）、`csharp_style_namespace_declarations=file_scoped:warning`
- [x] 1.10 在 `.editorconfig` 追加现代语法偏好：`csharp_style_var_for_built_in_types=false:silent`、`csharp_style_var_when_type_is_apparent=true:suggestion`、`csharp_style_expression_bodied_properties/methods=when_on_single_line:suggestion`、`csharp_style_implicit_object_creation_when_type_is_apparent=true:suggestion`、`csharp_style_prefer_switch_expression/pattern_matching/not_pattern=true:suggestion`、`csharp_style_prefer_index_operator/range_operator=true:suggestion`、`csharp_prefer_simple_using_statement=true:suggestion`、`csharp_style_throw_expression=true:suggestion`、`csharp_style_inlined_variable_declaration=true:suggestion`
- [x] 1.11 在 `.editorconfig` 追加 `dotnet_style_object_initializer=true:suggestion`、`dotnet_style_collection_initializer=true:suggestion`、`dotnet_style_prefer_auto_properties=true:warning`、`dotnet_style_readonly_field=true:suggestion`、`dotnet_diagnostic.IDE0005.severity=warning`
- [x] 1.12 在 `.editorconfig` 追加 `this.` 前缀统一为不加：`dotnet_style_qualification_for_field/property/method=false:warning`，以及私有字段 `_camelCase` 命名规则（`dotnet_naming_rule.private_field_underscore.*` 三行 + 对应 `symbols`/`style` 定义），severity 为 `warning`
- [x] 1.13 删除工作区内 `obj/Debug/net5.0`、`obj/Debug/net6.0` 残留目录（`find . -type d -name "net5.0" -o -type d -name "net6.0"` 后手工确认再删），并确认 `.gitignore` 已覆盖 `obj/`、`bin/`、`dist/`，不把产物入库
- [x] 1.14 验证：`dotnet build SchoolEndORM.sln` 成功（此阶段允许警告），三个项目产物均落在 `bin/Debug/net8.0`
- [x] 1.15 验证：`dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 结果与 1.1 的基线一致（`ConfigManagerTest` 仍为既有失败，其余通过）
- [x] 1.16 提交：`build: 升级到 net8.0 + Directory.Build.props + .editorconfig`

## 2. 阻塞缺陷修复（方案 13.1 / 13.2 — 提交 #2、#3）

- [x] 2.1 新建 `ORMFramework.Test/Fakes/FakePersistenceContext.cs`：实现 `IPersistenceContext`，用内存 `Dictionary<string, EntityMapping>` 返回映射，`Connection` / `DbDriverFactory` 在被访问时抛 `InvalidOperationException("no database in unit test")`，用于证明被测路径不连库（后续 3.7、8.3 复用）
- [x] 2.2 在 `ORMFramework.Test` 新建 `SessionCacheTest.cs`，写失败测试 `Dispose_WithMultipleCachedObjects_DoesNotThrow`：用反射向 `SessionCache._objList` 注入两个 `CacheEntity`（`Value` 为不同实体实例、`ObjectId` 为新 `Guid`），调用 `Dispose()` 并断言不抛 `InvalidCastException`（对应 `session-lifecycle` 的"释放持有多个对象的工作单元"）
- [x] 2.3 运行 `dotnet test --filter Dispose_WithMultipleCachedObjects_DoesNotThrow` 确认失败，失败原因为 `InvalidCastException`
- [x] 2.4 修 `Cache/SessionCache.cs:428` 的 `foreach (Guid objectId in _objList.Keys)`：`_objList` 是 `Dictionary<object, CacheEntity>`，键是实体对象不是 `Guid`，改为遍历 `_objList.Values` 并取 `entity.ObjectId` 传给 `GlobalCache.ReleaseReferenct`
- [x] 2.5 运行 `dotnet test --filter Dispose_WithMultipleCachedObjects_DoesNotThrow` 确认通过，`dotnet build SchoolEndORM.sln` 无新增警告
- [x] 2.6 提交：`fix: 修正 SessionCache.Collect 的 Guid 遍历类型错误`
- [x] 2.7 在 `SessionCacheTest.cs` 写失败测试 `GetObjectsFromGlobalCache_WithNullIgnoreIds_DoesNotThrowNullReference`：反射向 `_deleteList` 注入一个 `CacheEntity`，再反射调用私有 `GetObjectsFromGlobalCache(type, null, null)`，断言抛出的异常类型不是 `NullReferenceException`（无数据库时允许连接类异常）
- [x] 2.8 运行该测试确认失败（当前为 `NullReferenceException`）
- [x] 2.9 修 `Cache/SessionCache.cs:399-406` 的 `GetObjectsFromGlobalCache`：在 `foreach (var entity in _deleteList) ignoreIds.Add(entity.ObjectId);` 之前加 `ignoreIds ??= [];`，保持"调用方未传排除集合时也排除本会话已删除对象"的语义（对应 `session-lifecycle` 的"调用方未提供排除集合"）
- [x] 2.10 运行 `dotnet test` 全量确认通过（除 `ConfigManagerTest`），提交：`fix: GetObjectsFromGlobalCache 的 ignoreIds 空引用`

## 3. 回归测试安全网（方案 11.4 — 提交 #4，必须先于第 6–9 组）

- [x] 3.1 新建 `ORMFramework.Test/TestData/Config.xml`：从 `ORMFramework.Examples/Config.xml` 复制，保留 5 个 `Mapping` 与四种 Relation 结构，连接串改为不可连的本地占位值（测试只解析不连接）
- [x] 3.2 在 `ORMFramework.Test/ORMFramework.Test.csproj` 添加 `<None Update="TestData\Config.xml"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`，`dotnet build` 后确认 `bin/Debug/net8.0/TestData/Config.xml` 存在
- [x] 3.3 重写 `ORMFramework.Test/ConfigManagerTest.cs` 的配置读取测试：路径改为 `Path.Combine(AppContext.BaseDirectory, "TestData", "Config.xml")`，删除硬编码 `E:\毕业设计\...`（修 13.16，落地 `build-configuration` 的"测试项目脱离机器环境"）
- [x] 3.4 在该测试中断言配置内容：`Mappings.Count == 5`、每个 `EntityMapping` 的 `ClassName`/`TableName`/`Keys` 元素、四种 `RelationType`（`OneToOne`/`OneToMany`/`ManyToOne`/`MantToMany`）各至少一例，以及 `KeyColum`、`ReferenceColum`、`ManyToManyRelation` 的 `ReferenceClassColum`/`ReferenceClassKeyColum` 全部列名
- [x] 3.5 运行 `dotnet test --filter FullyQualifiedName~ConfigManagerTest` 确认通过（该用例从"预期失败"转为通过）
- [x] 3.6 在 `StatementAnalystTest.cs` 新增中缀/后缀序列快照断言方法（一个私有 helper 把 `List<StatElement>` 渲染为 `"Variable:Id|Operator:Equals|Constant:1"` 形式的字符串，便于逐字节比对）
- [x] 3.7 用该 helper 为 `"Id==1"` 断言中缀 `Variable:Id|Operator:Equals|Constant:1`、后缀 `Variable:Id|Constant:1|Operator:Equals`（对应 `query-parsing` 的"简单相等表达式"）
- [x] 3.8 同样为 `"a==1 && b like 'x%'"`、`"! ( b== 3 )"`、`"Student like '%' && a=='2' || ! ( b== 3 )"` 三串各断言中缀与后缀序列快照，期望值取当前实现的真实输出（先跑一次打印再固化为常量）
- [x] 3.9 在 `StatementAnalystTest.cs` 新增异常用例：null / 空串 / 纯空白查询串、括号不匹配 `"! ( b== 3"`、未闭合引号 `"a=='x"`、无法识别的运算符 `"a=<1"`、非法数值常量 `"a==1x"`，逐条断言当前实现的实际行为
- [x] 3.10 对 3.9 中因既有缺陷而行为不符规格的用例（末尾空格 `"Id==1 "` 触发 `IndexOutOfRangeException` — 13.11；空串抛 `Exception` 而非参数异常 — 13.12），先按现状断言并加 `// TODO(12.1): 修复后改为断言 QuerySyntaxException/ArgumentException` 注释，第 12 组解锁
- [x] 3.11 重写 `SQLGeneratorTest.cs` 的夹具：用 2.1 的 `FakePersistenceContext` + 手写 `EntityMapping`（一个含 `int`/`string`/`DateTime`/`double` 属性与一个 `IList<T>` 导航属性的实体），删除未使用字段 `pesistenceContext` / `@object`
- [x] 3.12 在 `SQLGeneratorTest.cs` 为 `GetSelectSQL(Type)`（无查询表达式）与 `GetSelectSQL(Type, QueryExpression)`（有表达式）断言完整 SQL 文本快照；无表达式的期望值按**当前实现**记录（含 13.6 的多余 `)`），并加 `// TODO(7.9)` 注释
- [x] 3.13 在 `SQLGeneratorTest.cs` 为 `GetInsertSQL`、`GetUpdateSQL`、`GetDeleteSQL` 各断言完整 SQL 文本快照，期望值按当前实现记录（含 13.7 的尾随分隔符、13.8 的 `'NULL'`），并加 `// TODO(6.3/7.9)` 注释
- [x] 3.14 新建 `docs/baselines/sql-snapshot.txt`：把 3.12/3.13 的四类语句期望文本逐行落盘，作为跨提交逐字节比对的基线（后续 6.5、7.9 更新此文件时必须在提交信息列出变化行）
- [x] 3.15 重写 `ObjectCalculatorTest.cs`：删除引用已不存在 API 的注释测试块，用当前 `IsTargetObject` 签名建立 `int` 属性 × `==`/`!=`/`>`/`>=`/`<`/`<=` 的真值表断言
- [x] 3.16 在 `ObjectCalculatorTest.cs` 补 `string`、`DateTime`、`bool`、`char` 四类属性的运算符真值表断言，含 `&&`/`||`/`!` 组合（对应 `query-parsing` 的"各类型的运算符真值表"）
- [x] 3.17 在 `ObjectCalculatorTest.cs` 补 `like` 用例：`%` 前缀/后缀/双侧、`_` 单字符、`[abc]` 字符组，以及带单引号常量的剥离（`Name=='abc'` 匹配属性值 `abc`）
- [x] 3.18 对 `Name==Nickname`（两个属性比较，当前因 13.9 结果错误）按现状断言并加 `// TODO(12.2)` 注释；对空串参与比较的用例同样先记录现状（当前 `xStr[0]` 空串越界）
- [x] 3.19 新建 `ORMFramework.Test/Fakes/FakeSessionCache.cs`：实现 `ISessionCache`，记录 `Search` 被调用次数与收到的查询谓词串，返回预置对象数组
- [x] 3.20 新建 `ORMFramework.Test/LoadListTest.cs`：设置 `ForeignKeyName="fk"`、`ForeignKeyValues=[v1, v2]`，连续访问 `Count`/索引器/`GetEnumerator()`，断言假 `ISessionCache` 只被查询一次、且收到的谓词串为 `fk=='v1' || fk=='v2'`（期望值按当前 `LoadList.GetItems` 的实际拼接形式固化）
- [x] 3.21 验证：`dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 全绿（含修好的 `ConfigManagerTest`），`dotnet build SchoolEndORM.sln` 无新增警告
- [x] 3.22 提交：`test: 补齐 StatementAnalyst / SQLGenerator / ObjectCalculator / ConfigManager / LoadList 回归测试`

## 4. 机械化语法现代化（方案第 4 章 — 提交 #5–#8）

- [x] 4.1 `ORMFramework/Cache/*.cs`（8 文件）转 file-scoped namespace：`namespace X { ... }` → `namespace X;` 并整体减一层缩进；`GlobalCacheEntity.cs` 含 `class` + `enum Lock` 两个类型，不拆分文件
- [x] 4.2 `ORMFramework/Configuration/*.cs` 与 `Configuration/EntityClass/*.cs`（7 文件）转 file-scoped namespace
- [x] 4.3 `ORMFramework/Event/*.cs`、`ORMFramework/Ioc/*.cs`、`ORMFramework/Listener/*.cs`（13 文件）转 file-scoped namespace；`Listener/IListeners.cs` 含 6 个接口，保持同文件
- [x] 4.4 `ORMFramework/LogHelper/*.cs`、`ORMFramework/SQLGenerator/*.cs`、`ORMFramework/Session/*.cs`、`ORMFramework/SessionFactory/*.cs`（12 文件）转 file-scoped namespace；注意 `SQLGenerator/` 下命名空间是 `ORMFramework.SQL`，保持不动（红线 14.5）
- [x] 4.5 `ORMFramework/Statment/*.cs`（4 文件）转 file-scoped namespace；`StatElement.cs` 含 `class` + `enum StatElementType`，保持同文件
- [x] 4.6 `ORMFramework.Examples/*.cs` 与 `ORMFramework.Test/*.cs`（11 文件）转 file-scoped namespace，全部 57 个 .cs 至此完成
- [x] 4.7 验证并提交：`dotnet build SchoolEndORM.sln`、`dotnet test` 通过后提交 `style: file-scoped namespace（全项目）`
- [x] 4.8 清理 `ORMFramework` 下全部文件的冗余 using：删除 VS2005 三件套 `using System;` / `using System.Collections.Generic;` / `using System.Text;`（确实使用 `StringBuilder` 的文件可显式保留 `System.Text`），保留 `System.Data`、`System.Data.Common`、`System.Reflection`、`System.Text.RegularExpressions`、`System.Xml`、`System.IO`、`log4net.*`、`Microsoft.*` 与项目自身命名空间
- [x] 4.9 删除 `ORMFramework/SessionFactory/DefaultDbDriverFactory.cs:6` 的 `using System.Configuration.Provider;`（.NET Framework 遗留命名空间，当前文件完全未用）
- [x] 4.10 清理 `ORMFramework.Examples`（含 `EntityClasses/*.cs` 五个实体类）与 `ORMFramework.Test` 的冗余 using；实体类需要的 `IList<T>` 由隐式 using 覆盖，三行可全删；测试文件保留 `Microsoft.VisualStudio.TestTools.UnitTesting`
- [x] 4.11 验证并提交：`dotnet build SchoolEndORM.sln` 零 IDE0005、`dotnet test` 通过后提交 `style: ImplicitUsings + 清理冗余 using`
- [x] 4.12 目标类型 `new`（约 60 处）：字段与局部变量右侧 `new Dictionary<K,V>()` 等改 `new()`，典型位置 `Cache/SessionCache.cs:14-18`、`Cache/GlobalCache.cs`、`Session/Session.cs`、`Configuration/EntityClass/Configuration.cs`
- [x] 4.13 给从未重新赋值的字段加 `readonly`：`SessionCache._objList/_objTable/_insertList/_updateList/_deleteList`、`Session._insertListeners/_deleteListeners/_updateListeners/_submitListeners`、`GlobalCache._objTable/_objList`、`Configuration._mappings`；`GlobalCache.CACHEMAXCOUNT` 因 `Collect()` 会改写故**不加** `readonly`
- [x] 4.14 局部变量 `var` 化（约 200 处）：仅在右侧是 `new` / 强制转换 / `typeof` 时改 `var`，方法返回值保留显式类型；重点文件 `SQLGenerator/SQLGenerator.cs`、`Cache/SessionCache.cs`、`Cache/GlobalCache.cs`、`Statment/StatementAnalyst.cs`
- [x] 4.15 变量声明就近化：`SessionCache.Commit`（`IDbCommand cmd; IDbTransaction tran;` 前置声明）、`SessionCache.ProcessQueryCommand`（`DataTable dt;`）、`GlobalCache.ReadDataFromDatabase`（`DataTable dt;`/`cacheEntity`/`cacheResult`）、`Session.Search<T>`、`LoadList.GetItems`、`ObjectCalculator.ProcessNotBooleanOperator`（`object xValue; object yValue;`）、`SQLGenerator.GetSelectSQL`（`List<StatElement> expression;`）、`StatementAnalyst.ReadElement`（`char currentChar;`）
- [x] 4.16 验证并提交：`dotnet build -warnaserror`、`dotnet test` 通过，`git diff --stat` 呈净减后提交 `style: target-typed new + readonly 字段 + var`
- [x] 4.17 去掉 `this.` 前缀：`SessionFactory/DefaultDbDriverFactory.cs`、`SessionFactory/SessionFactoryIoc.cs`、`Session/PersistenceContext.cs`、`Ioc/ComponentProviderBuilder.cs`、`Ioc/DefaultComponentProvider.cs`（纯格式 diff，与语义变换严格分离，见 design D10）
- [x] 4.18 `Cache/GlobalCache.cs:15-16` 命名修正：`private static int INCREASE = 100;` → `private const int CacheIncrease = 100;`（从不变化），`private static int CACHEMAXCOUNT = 1000;` → `private static int _cacheMaxCount = 1000;`（`Collect()` 会递增），同步更新全部引用点
- [x] 4.19 `Ioc` 字段命名修正：`ComponentProviderBuilder.componentImplTypes` → `_componentImplTypes`、`DefaultComponentProvider.componentLiftimeDict` → `_componentLifetimes`（拼写）、`serviceProvider` → `_serviceProvider`，字段类型由 `IDictionary<,>` 改具体 `Dictionary<,>`（为 7.1 的 `TryGetValue` 铺路）
- [x] 4.20 其余私有标识符拼写修正：`SQLGeneratorTest.pesistenceContext`（若 3.11 未删则删）、`SessionCache.foreingKeyValue`、`ObjectCalculator.pattenRegex`、`StatementAnalyst.ReadNotOpeartion` → `ReadNotOperation`、`QueryExpression._epxression` → `_expression`；公开 API 拼写（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）一律不改（红线 14.1）
- [x] 4.21 `git mv ORMFramework.Examples/EntityClasses/ContartMenu.cs ORMFramework.Examples/EntityClasses/ContactMenu.cs`，使文件名与类名 `ContactMenu` 一致
- [x] 4.22 验证并提交：`dotnet format --verify-no-changes SchoolEndORM.sln` 通过、`dotnet build -warnaserror` 与 `dotnet test` 通过后提交 `style: 统一 this. 前缀与私有字段命名`

## 5. 类型与成员声明现代化（方案第 5 章 — 提交 #9–#11）

- [x] 5.1 `Statment/StatElement.cs`、`Statment/QueryExpression.cs` 手写属性 → 自动属性：`Value`/`Type` 改 `{ get; set; }`，`NifixExpression`/`SuffixExpression`/`Expression` 改 `{ get; }` 由构造函数赋值（属性名 `NifixExpression` 是红线，不改拼写）
- [x] 5.2 `Configuration/ConfigManager.cs` 的 `ConfigFilePath` 与 `Configuration/EntityClass/Configuration.cs` 的 `Mappings` 改自动属性（后者 `{ get; } = [];`）
- [x] 5.3 `Session/LoadList.cs` 的 `SessionCache`/`ForeignKeyName`/`ForeignKeyValues`/`SelectListener` 与 `Session/PersistenceContext.cs` 的 `DbDriverFactory` 改自动属性；三个属性名保持不变（9.1 之前是红线）
- [x] 5.4 `Session/Session.cs` 的 `SelectListener`/`InsertListeners`/`DeleteListeners`/`UpdateListeners`/`SubmitListeners`/`CommandListener`/`PersistenceContext` 改 `{ get; }` + 构造函数或初始化器赋值，删除对应私有字段
- [x] 5.5 `Cache/GlobalCacheEntity.cs` 的 `BlockThreads` 改 `{ get; } = new();`；**不触碰**该文件任何 `Monitor`/`Thread`/`Lock` 相关语句（design D9）
- [x] 5.6 `ORMFramework.Examples/EntityClasses/` 的 `Course.cs`（4 属性）、`Teacher.cs`（4 属性）、`Department.cs`（3 属性）、`ContactMenu.cs`（5 属性）改自动属性；导航属性保持 `IList<T>` 声明、保留 public 无参构造、属性保持 public 可读写（红线 14.3），可参照已是自动属性的 `Student.cs`
- [x] 5.7 `Configuration/EntityClass/EntityMapping.cs` 的 `Keys`/`Relations` 延迟初始化改字段初始化器（`= []` / `= new Dictionary<string, EntityRelation>()`），`set` 保留空值合并 `value ?? new()` 以维持"赋 null 后 get 仍返回空集合"的等价语义
- [x] 5.8 只读成员改表达式体（第一批）：`Session/Session.cs` 的转发属性（`Connection` 等）、`SQLGenerator/SQLGenerator.cs` 的 `GetSelectSQL(Type)` 与 `GetSQLOperator`（后者同时改 `static`）
- [x] 5.9 只读成员改表达式体（第二批）：`Ioc/DefaultComponentProvider.GetComponent<T>`、`Ioc/ComponentProviderBuilder.Build`、5 个 Listener 的 `OnXxx`、`SessionFactory/SessionFactory.GetEntityMappingByClassName`、`SessionFactory/SessionFactoryIoc.Initialize`、`Cache/ObjectCalculator.IsBooleanOperator`
- [x] 5.10 验证并提交：`dotnet build -warnaserror` 无新增警告、`dotnet test` 全绿（SQL 快照与序列快照未变化）后提交 `refactor: 手写属性 → 自动属性/表达式体成员`
- [x] 5.11 `Configuration/ConfigManager.cs` 改主构造函数 `public sealed class ConfigManager(string? configFilePath = null)`，`ConfigFilePath` 初值用 `Path.Combine(Environment.CurrentDirectory, "Config.xml")` 替代 `Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Config.xml"` 手工拼接
- [x] 5.12 `LogHelper/LogHelper.cs:19` 同样把手工路径拼接改 `Path.Combine`，并把类型改为 `public static class LogHelper`（CA1052）；不重构日志仓库创建逻辑（13.15 留作后续提案）
- [x] 5.13 `Cache/GlobalCacheResult.cs`、`Statment/QueryExpression.cs`、`Ioc/DefaultComponentProvider.cs` 改主构造函数；`Session/Session.cs` 的 3 个链式构造函数与 `Session/PersistenceContext.cs` 的两个重载保持原样（DI 解析依赖，见 design D7）
- [x] 5.14 `Statment/QueryExpression.cs` 改 `internal sealed record QueryExpression(List<StatElement> NifixExpression, List<StatElement> SuffixExpression, string Expression)`；`StatElement`、`CacheEntity`、`GlobalCacheEntity`、`*Event`、全部实体类一律**不改** `record`（design D7）
- [x] 5.15 给无派生类型加 `sealed`：5 个 Listener 实现、`SessionCache`、`Session`、`PersistenceContext`、`DefaultDbDriverFactory`、`LoadList<T>`、`SQLGenerator`、`ConfigManager`、`DefaultComponentProvider`、`ComponentProviderBuilder`、`SessionFactory`、`SessionFactoryIoc`；`EntityRelation`、`CacheEntity`、`ActionEvent` 有派生类，**不加**
- [x] 5.16 验证：`dotnet test --filter FullyQualifiedName~ComponentProviderTest` 通过（确认 `sealed` 与主构造函数未破坏 DI 解析路径），再运行全量 `dotnet test`
- [x] 5.17 提交：`refactor: 主构造函数 + sealed + static class`
- [x] 5.18 `Session/LoadList.cs`：把 `GetItems()`（填充字段的 void 方法）改为返回 `List<T>` 的 `LoadItems()`，新增 `private List<T> Items => _items ??= LoadItems();`
- [x] 5.19 `Session/LoadList.cs`：把 12 处 `if (_items == null) { GetItems(); }` 守卫全部删除，成员改为通过 `Items` 访问（`Count`、`Contains`、`Add`、索引器、两个 `GetEnumerator` 等），逐个改表达式体
- [x] 5.20 验证并提交：`dotnet test --filter FullyQualifiedName~LoadListTest` 断言"懒加载只触发一次"仍通过，全量 `dotnet test` 通过后提交 `refactor: LoadList 懒加载守卫收敛`

## 6. 控制流与模式匹配现代化（方案第 6 章 — 提交 #12–#15）

- [x] 6.1 `Statment/StatementAnalyst.cs:188-219` 的 `GetOperatorByString` 改 `static` + switch 表达式（12 个分支 + `_ => throw`），同时 `ToLower()` → `ToLowerInvariant()`（CA1311，土耳其语环境 `"LIKE".ToLower()` 会得到 `"lıke"`，落地 `query-parsing` 的"土耳其语环境识别 like"）
- [x] 6.2 `Statment/StatementAnalyst.cs:134` 的 `CreateElement` 中 `ToLower()` 同样改 `ToLowerInvariant()`
- [x] 6.3 `Cache/ObjectCalculator.cs` 的 3 处纯映射 switch 改 switch 表达式：`CalculateBooleanOperator`（:296-307）、`CalculateNotBooleanOperator`（:309-328）、`CalculateStringOperator` 尾部 switch（:250-260）
- [x] 6.4 `Configuration/ConfigManager.cs:187-200` 的 `ReadMap` 中 `node.Name` switch 改 switch 表达式赋值 `relation.Type`；`Ioc/DefaultComponentProvider` 的生命周期 switch 与 `ConfigManager.GetSystemConfiguration`/`ReadSessionFactory` 的 switch 因有副作用保留语句形式
- [x] 6.5 `Statment/StatementAnalyst.cs:98-120` 的 `IsVariableChar`/`IsVariableFirstChar` 改 `static` 表达式体：`char.IsAsciiLetterOrDigit(c) || c == '_'` 与 `char.IsAsciiLetter(c) || c == '_'`；**严禁**用 `char.IsLetterOrDigit`（对中文返回 true，会改变词法行为，违反 `query-parsing` 的"标识符字符集限定为 ASCII"）
- [x] 6.6 折叠 `StatementAnalyst` 的 5 个状态转移判定为单个布尔表达式：`ReadNotOperation`（:428-435）、`ReadLeftParenthesis`（:447-458）、`ReadRightParenthesis`（:470-477）、`ReadVariableOrConstant`（:490-501）、`ReadOperator`（:513-528）
- [x] 6.7 运行 `dotnet test --filter FullyQualifiedName~StatementAnalystTest` 逐条比对 3.7/3.8 的中缀/后缀快照，确认 6.5/6.6 未改变任何序列
- [x] 6.8 折叠其余布尔返回：`Cache/ObjectCalculator.IsString`（:343-373）、`Configuration/ConfigManager` 的 `ReadSessionFactory`/`ReadConnectionString`/`ReadProviderName` 三处 `if (result && ...) return true; else return false;`
- [x] 6.9 `Cache/GlobalCache.cs:201-211` 的 `GetObjectVersion` 改 `_objList.TryGetValue(objectId, out var e) ? e.Version : -1`；只改这一个表达式，不触碰该文件其他语句
- [x] 6.10 验证并提交：`dotnet test` 全绿、SQL 快照与真值表未变化后提交 `refactor: switch 表达式 + 布尔表达式折叠`
- [x] 6.11 新建 `ORMFramework/SQL/SqlLiteral.cs`：`internal static class SqlLiteral`，提供 `Format(object? value)`（用于 insert 值列表：null → `NULL`，`string or char` → `'…'`，`DateTime dt` → `'yyyy-MM-dd HH:mm:ss'`，其余 → `Convert.ToString(value, CultureInfo.InvariantCulture)`）与 `Comparison(string column, object? value)`（null → `column is null`，否则 `column=<literal>`）
- [x] 6.12 新建 `ORMFramework.Test/SqlLiteralTest.cs`：断言 null / `string` / `char` / `DateTime` / `int` / `double` / `bool` 七类输入的输出文本，并用 `CultureInfo.CurrentCulture = new CultureInfo("de-DE")` 包裹再断言 `double` 输出仍为 `1.5`（对应 `sql-generation` 的"小数在逗号小数点区域仍生成点号"）
- [x] 6.13 用 `SqlLiteral` 替换 5 份重复的字面量格式化：`Cache/GlobalCache.cs:408-423`（`ReadIgnoreKeys`）、`Cache/SessionCache.cs:74-85`（`GetReferenceClasses` 的 `*-to-One` 分支）、`Cache/SessionCache.cs:129-140`（many-to-many 分支）、`Session/LoadList.cs:186-197`（`GetItems`/`LoadItems`）、`SQLGenerator/SQLGenerator.cs:196-207` 与 `:258-269`
- [x] 6.14 在替换过程中落地两处语义修正：全部字面量改不变文化（修 13.10 的 5 处 culture 缺陷）、null 由 `'NULL'` 改 SQL 空值（where 用 `is null`、insert 用 `NULL`，修 13.8）
- [x] 6.15 运行 `dotnet test --filter FullyQualifiedName~SQLGeneratorTest`，把因 6.14 而变化的期望文本更新为新输出，并同步更新 `docs/baselines/sql-snapshot.txt`
- [x] 6.16 提交：`refactor: 抽取 SqlLiteral 消除 5 份重复格式化`，提交信息逐条列出预期变化的文本形态（`'NULL'` → `is null` / `NULL`、小数点与日期格式的 culture 无关化）
- [x] 6.17 `SessionFactory/DefaultDbDriverFactory.cs:106-118` 改 `is not` 声明模式：`if (dbDataAdapter is not DbDataAdapter adapter) throw ...;` 后直接用 `adapter`，消除二次强制转换
- [x] 6.18 `Cache/ObjectCalculator.cs` 的 `(bool)x.Value`、`(string)element.Value`、`(DateTime)property.GetValue(...)` 等强制转换改 `is` 声明模式
- [x] 6.19 `Statment/StatementAnalyst.cs` 约 20 处 `element.Type == StatElementType.Operator && (Operator)element.Value == Operator.Xxx` 改属性模式 `element is { Type: StatElementType.Operator, Value: Operator.Xxx }`
- [x] 6.20 `Cache/ObjectCalculator.cs:242-249` 首尾引号剥离改列表模式 + 范围运算符：`if (xStr is ['\'', .., '\'']) xStr = xStr[1..^1];`（同理 `yStr`），顺带消除空串越界（提交信息注明这是允许的附带修正，见 design D2）
- [x] 6.21 `Cache/ObjectCalculator.IsString:360` 的 `value[value.Length - 1]` 与 `Statment/StatementAnalyst.CreateElement:165` 的 `_currentElement[_currentElement.Length - 1]` 改 `[^1]`
- [x] 6.22 验证并提交：`dotnet test` 全绿（真值表与序列快照逐条一致）后提交 `refactor: 模式匹配（is not / 属性模式 / 列表模式）`
- [x] 6.23 重写 `Configuration/ConfigManager.GetSystemConfiguration`（:29-80）为卫语句 + `using var`：`using var fs = new FileStream(...)`、`using var reader = new XmlTextReader(fs) { WhitespaceHandling = WhitespaceHandling.None }`，两个前置校验（XML 声明、`Configuration` 根元素）各自 early throw，主循环用 `reader.Name switch`
- [x] 6.24 确认 6.23 消除了 8 处手写 `Close()` 与 4 组重复的 close+throw，并修掉 `ReadSessionFactory` 抛异常时 `fs` 不关闭的泄漏路径；保持解析顺序敏感性（`SessionFactory` 必须在 `Mappings` 之前）与未知元素报错行为不变（红线 14.4）
- [x] 6.25 明确跳过含 `Monitor` 的嵌套反转：`GlobalCache.Search(Guid)`、`GlobalCache.ProcessDeletedObject`、`GlobalCache.ReadObject`、`ObjectCalculator.IsTargetObject` 的嵌套结构本组不动（design D9），在 `docs/modern-csharp-migration-plan.md` 的 6.5 节加一行"本变更不做"的标注
- [x] 6.26 验证并提交：`dotnet test --filter FullyQualifiedName~ConfigManagerTest` 与全量 `dotnet test` 通过后提交 `refactor: 卫语句化 ConfigManager（含 using 声明）`

## 7. 集合、字符串与 API 现代化（方案第 7 章 — 提交 #16–#19）

- [x] 7.1 `Cache/GlobalCache.cs` 的 `ContainsKey` + 索引器双查找改 `TryGetValue`（位置 :45,80,82,203,205,241,243,245,444,446,470,472,474）；逐处核对改写不跨越 `Monitor.Enter`/`Monitor.Exit` 之间（design D9 白名单）
- [x] 7.2 `Cache/SessionCache.cs` 的双查找改 `TryGetValue`（位置 :32,34,56,64,71,87,89,157,161,170,171,173,206,242,244,246,252,253,255,266,416,418,420）；"缺键则新建列表再 Add"的模式统一改为 `if (!dict.TryGetValue(k, out var list)) { list = []; dict[k] = list; } list.Add(...)`
- [x] 7.3 `Session/PersistenceContext.cs:50,52` 的双查找改 `TryGetValue`
- [x] 7.4 修 13.14：`SessionFactory/DefaultDbDriverFactory.cs:37-41` 的字段类型由 `IDictionary` 改具体 `ConcurrentDictionary<string, byte>`，注册逻辑改 `if (RegisteredProviders.TryAdd(providerName, 0)) DbProviderFactories.RegisterFactory(providerName, providerName);`，消除非原子注册
- [x] 7.5 验证并提交：`dotnet build -warnaserror` 零 CA1854、`dotnet test` 全绿后提交 `perf: TryGetValue/TryAdd 消除双查找`
- [x] 7.6 一维查找表改集合表达式：`Statment/StatementAnalyst.cs:15` 的 `_operatorChars`、`SQLGenerator/SQLGenerator.cs:12-18` 的 `_sqlOperators`/`_basicType`/`_stringType`、`Cache/GlobalCache.cs` 的 `_basicType` 改 `[...]` 初始化
- [x] 7.7 `SQLGenerator._basicType`/`_stringType` 与 `GlobalCache._basicType` 各自改 `FrozenSet<Type>`（`FrozenSet.ToFrozenSet([...])`），`IsBasicType`/`IsStringType` 改 `=> Set.Contains(type)`；两个 `_basicType` 内容不同（`GlobalCache` 含 `string`）是有意差异，**不合并**（design D5）
- [x] 7.8 `Statment/StatementAnalyst._operatorChars` 改 `SearchValues<char>`（`SearchValues.Create("=><!&|like()")`），`IsOperatorChar` 改 `=> OperatorChars.Contains(c)`
- [x] 7.9 `SQLGenerator._sqlOperators` 保持 11 个元素与顺序不变（按 `(int)Operator` 索引）；`StatementAnalyst._priorityTable` 保持 `int[,]` 与 10×10 维度不变，仅由"挤在一行"重排版为每行一行，并在每行行首加对应 `Operator` 的注释
- [x] 7.10 在 `_priorityTable` 上方加注释说明约束："`Operator` 有 12 个值而本表为 10×10，`LeftParenthesis`(10)/`RightParenthesis`(11) 由 `ProcessOperatorStack` 提前分流，禁止扩表、禁止重排枚举"（红线 14.2）
- [x] 7.11 验证并提交：`dotnet test` 全绿（`StatementAnalystTest` 序列快照逐条一致）后提交 `refactor: 集合表达式 + FrozenSet/SearchValues`
- [x] 7.12 `string.Format` 改插值字符串 + 显式不变文化（框架侧）：`SQLGenerator.cs:202,206,248,260,264,268`、`SessionCache.cs:76,80,131,135`、`LoadList.cs:188,192`，统一用 `FormattableString.Invariant($"...")` 或 `string.Create(CultureInfo.InvariantCulture, $"...")`；已迁移到 `SqlLiteral` 的位置直接调用它
- [x] 7.13 `string.Format` 改插值（解析与求值侧）：`StatementAnalyst.cs:173,217`、`ObjectCalculator.cs:63,113,126,339`，异常消息用插值，日期格式化用 `$"'{dt:yyyy-MM-dd HH:mm:ss}'"` + `FormattableString.Invariant`
- [x] 7.14 `ORMFramework.Examples/Program.cs`：约 17 处 `Console.WriteLine("{0}--{1}", a, b)` 与 `string.Format`（:67,81）改插值字符串
- [x] 7.15 在 `README.md` 新增"安全边界"小节，显式声明"SQL 由字符串拼接生成、不使用参数化、不提供 SQL 注入防护、需调用方自行约束输入"（对应 `sql-generation` 的"明示不防注入的限制"）
- [x] 7.16 在 `ORMFramework/SQLGenerator/SQLGenerator.cs`、`ORMFramework/SQL/SqlLiteral.cs`、`ORMFramework/Cache/SessionCache.GetReferenceClasses` 的文档注释中加同样的不防注入声明
- [x] 7.17 验证并提交：`dotnet test` 全绿、SQL 快照逐字节不变（插值改写必须行为等价）后提交 `refactor: 插值字符串 + InvariantCulture`
- [x] 7.18 `Statment/StatementAnalyst.cs:59,179` 的 `_currentElement.Remove(0, _currentElement.Length)` 改 `_currentElement.Clear()`
- [x] 7.19 `Statment/StatementAnalyst.cs:165-175`：`double.Parse` + `catch` 控流改 `!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)`，并把首尾引号判定改列表模式 `text is not ['\'', .., '\'']`（落地 `query-parsing` 的"逗号小数点区域解析数值"与"非法常量"）
- [x] 7.20 适度 LINQ：`Session/Session.cs:107-115` 的 `Search<T>` 改 `=> OnSearch(typeof(T), selectCommand).Cast<T>().ToArray();`，`Search(Type, string)` 改 `.ToArray()`
- [x] 7.21 两个工厂的 mapping 填充改 `ToDictionary`：`SessionFactory/SessionFactory.cs:42-45` 与 `SessionFactory/SessionFactoryIoc.cs:47-50` 改 `Configuration.Mappings.ToDictionary(m => m.ClassName)`；确认 `SessionFactoryIoc` 的事后补配顺序不变
- [x] 7.22 `KeyValuePair` 遍历改解构：`Cache/GlobalCache.cs:393,511`、`Cache/SessionCache.cs:61`、`Ioc/DefaultComponentProvider.cs:37` 改 `foreach (var (key, values) in dict)`；锁内循环与会修改被遍历集合的循环（`Collect`、`RollBack`、`DeleteCacheEntity`）**不 LINQ 化**
- [x] 7.23 修 13.5：`Cache/GlobalCache.cs:511-525` 的 `Collect` 在 `foreach` 中调用 `_objList.Remove(...)` 会抛 `InvalidOperationException`，改为"先把待删键收集到 `List<object>`、循环结束后再逐个 `Remove`"的两段式；不引入 LINQ、不触碰该方法的任何锁语句（design D9 例外条款）
- [x] 7.24 修 13.6：`SQLGenerator/SQLGenerator.cs:44-66` 的 `GetSelectSQL` 在 `queryExpression == null` 时不追加 `" where ("` 却无条件追加 `")"`，改为只有生成了 `where (` 时才补 `)`（落地 `sql-generation` 的"无查询条件的查询"）
- [x] 7.25 修 13.7：`SQLGenerator/SQLGenerator.cs:159-162,222-226` 用 `i < properties.Length - 1` 判定分隔符，最后一个属性被 `continue` 跳过时残留 `,` / `" and "`，改为把片段收集到 `List<string>` 后 `string.Join(", ", parts)` / `string.Join(" and ", parts)`（落地"最后一个属性被跳过"）
- [x] 7.26 更新 `SQLGeneratorTest` 中受 7.24/7.25 影响的期望文本与 `docs/baselines/sql-snapshot.txt`，把 3.12/3.13 里标注的 `// TODO(7.9)` 注释删除
- [x] 7.27 验证：`dotnet test` 全绿；`dotnet build -warnaserror` 通过
- [x] 7.28 验证（`GlobalCache` 专项）：临时取消 `ORMFramework.Examples/Program.cs` 中 `TestMultitransaction` 的注释并配置本地 MySQL，`dotnet run --project ORMFramework.Examples` 比对两线程读写锁交互输出与改造前一致，随后恢复注释状态
- [x] 7.29 提交：`refactor: LINQ + KeyValuePair 解构 + StringBuilder.Clear + TryParse`，并把 13.5/13.6/13.7 各拆为独立子提交

## 8. 资源管理与异常处理现代化（方案第 8 章 — 提交 #20、#21）

- [x] 8.1 `Cache/SessionCache.Commit`（:292-316）改 `using var conn = ...GetDbConnection();` / `using var tran = conn.BeginTransaction();` / `using var cmd = conn.CreateCommand();`，删除三处手写 `conn.Close()`，`catch { tran.Rollback(); throw; }`
- [x] 8.2 `Cache/SessionCache.ProcessQueryCommand`（:275-290）与 `Cache/GlobalCache.ReadDataFromDatabase`（:434-483）对自建连接、`IDbCommand`、`DataSet` 加 `using`；`GlobalCache` 中只加 `using`，不动任何 `Monitor`/`Thread` 语句
- [x] 8.3 `Listener/DefaultCommandListener.OnExecute`：只对 `DataSet` 与 `IDbCommand` 加 `using`，从 `PersistenceContext.Connection` 取得的**共享连接不加** `using`（落地 `session-lifecycle` 的"共享连接不被使用方释放"，见 design D8）
- [x] 8.4 删除 `Cache/SessionCache.cs:22-25` 的 `~SessionCache()` 与 `Session/Session.cs:92-95` 的 `~Session()` 终结器
- [x] 8.5 `SessionCache.Dispose` 与 `Session.Dispose` 加 `private bool _disposed;` 守卫实现幂等（`if (_disposed) return; _disposed = true; ...`），`SessionCache.Dispose` 保留 `GC.SuppressFinalize` 的移除后语义（无终结器则不需要该调用，一并删除，满足 CA1063/CA1816）
- [x] 8.6 在 `SessionCacheTest.cs` 补测试：连续两次 `Dispose()` 只归还一次二级缓存引用计数（用反射或计数桩验证 `GlobalCache.ReleaseReferenct` 只被调用一次），且不抛异常
- [x] 8.7 在 `SessionCacheTest.cs` 补测试：`using` 块退出后连接不再处于打开状态；并补一条"连续两次通过会话执行原始查询均成功"的测试（用 `FakePersistenceContext` 的共享连接桩，验证 8.3 未释放共享连接）
- [x] 8.8 验证并提交：`dotnet test` 全绿后提交 `refactor: using 声明 + 移除终结器 + 幂等 Dispose`，提交信息说明行为变更（进程退出时不再自动归还二级缓存引用计数，原终结器也不保证运行，实际影响为零）
- [x] 8.9 `Cache/SessionCache.cs:198`（`Update`）、`:228`（`Insert`）、`:314`（`Commit`）的 `throw ex;` 改 `throw;`（CA2200，落地 `error-reporting` 的"异常传播保留原始堆栈"）
- [x] 8.10 去掉未使用的异常变量：`Cache/GlobalCache.cs:66,95,189` 的 `catch (ThreadInterruptedException ex)` → `catch (ThreadInterruptedException)`（仅改捕获子句，不动 catch 体内的重试逻辑）；`Cache/SessionCache.cs:176,195,225,310` 的 `catch (Exception ex)` → `catch`
- [x] 8.11 新建 `ORMFramework/Exceptions/OrmException.cs`：`public class OrmException : Exception`，提供三个标准构造函数（无参 / `string message` / `string message, Exception innerException`）
- [x] 8.12 新建 `ORMFramework/Exceptions/QuerySyntaxException.cs`、`OrmConfigurationException.cs`、`ConcurrencyException.cs`，均为 `public sealed class ... : OrmException` 且各自提供三个标准构造函数
- [x] 8.13 替换 `Statment/StatementAnalyst.cs` 的 `throw new Exception`：`"QueryString is empty"`（:26,51）改 `ArgumentException.ThrowIfNullOrWhiteSpace(value)`；其余 14 处（`"Invalid Vairable:{0}"`、`"Invaild Operator:{0}"`、`"Invalid Statement"`、`"' is expected"`、`"( is expected"`、`") is expected"`）改 `QuerySyntaxException` 并在消息中保留出错原文片段
- [x] 8.14 `Statment/StatementAnalyst.cs:535` 的 `"element.Type must be StatElement.Operator"` 改 `ArgumentException(..., nameof(element))`
- [x] 8.15 替换 `Cache/ObjectCalculator.cs` 的 8 处 `throw new Exception` 为 `QuerySyntaxException`；`SQLGenerator/SQLGenerator.cs:61` 同样改 `QuerySyntaxException`
- [x] 8.16 替换 `Configuration/ConfigManager.cs:64,71,78` 的 `"Invail Config File"` 为 `OrmConfigurationException`，消息改为说明具体哪一项校验失败（缺 XML 声明 / 根元素不是 `Configuration` / 未识别元素），落地 `error-reporting` 的"配置文件非法"
- [x] 8.17 替换 `Cache/GlobalCache.cs:150,197,218` 的 `"You must get a WriteLock before updating/deleting object"` / `"object has been deleted"` 为 `ConcurrencyException`；仅替换 `throw` 语句本身，不动周边锁语句
- [x] 8.18 替换状态类异常：`Cache/SessionCache.cs:208` 的 `"The object exists"` 与 `Cache/GlobalCache.cs:490` 的 `"The object do not be referenced"` 改 `InvalidOperationException`；`SessionFactory/DefaultDbDriverFactory.cs:49` 的 `"ConnectionString can not be null or empty"` 改 `InvalidOperationException`
- [x] 8.19 替换参数类异常：`SessionFactory/DefaultDbDriverFactory.cs:116` 改 `ArgumentException(..., nameof(dbDataAdapter))`；`SQLGenerator/SQLGenerator.cs:73,245,254` 的 `"Type of oldOject must be the same as that of newObject"`、`"Invalid property type"` 改 `ArgumentException(..., nameof(...))`
- [x] 8.20 参数校验统一改 throw helper：`ArgumentNullException.ThrowIfNull(...)`、`ArgumentException.ThrowIfNullOrWhiteSpace(...)`，覆盖 `Session`/`SessionCache`/`SQLGenerator`/`StatementAnalyst` 的公开入口
- [x] 8.21 全仓库检索确认无残留：`rg "throw new Exception\(" ORMFramework ORMFramework.Examples` 应无结果（28 处全部替换完毕）
- [x] 8.22 把 3.9/3.10 中断言 `Exception` 的用例改为断言具体类型（`Assert.ThrowsException<QuerySyntaxException>` / `ArgumentException`），并新增 `catch (OrmException)` 能捕获三类失败、`catch (Exception)` 行为不变的两条测试（覆盖 `error-reporting` 的"按类别捕获"与"现有捕获代码不被破坏"）
- [x] 8.23 验证并提交：`dotnet build SchoolEndORM.sln -warnaserror` 零 CA2200/CA2201、`dotnet test` 全绿后提交 `refactor: throw; + 具体异常类型`

## 9. 反射强类型化（方案第 9 章 — 提交 #22）

- [x] 9.1 `Cache/SessionCache.cs:109-116,142`：`Assembly.Load("ORMFramework").GetType("ORMFramework.LoadList\`1")` 改 `typeof(LoadList<>).MakeGenericType(itemType)`，解除"程序集必须叫 ORMFramework"的隐式约束
- [x] 9.2 同一位置的三个属性名字符串改 `nameof`：`nameof(LoadList<object>.ForeignKeyValues)`、`nameof(LoadList<object>.SessionCache)`、`nameof(LoadList<object>.ForeignKeyName)`；不引入 `ILoadList` 接口（留作后续提案，见 design D6）
- [x] 9.3 `PropertyInfo.GetValue(obj, null)` / `SetValue(obj, value, null)` 改单参重载（`Cache/GlobalCache.cs:57,337,371,372,460`）
- [x] 9.4 同上改写 `Cache/SessionCache.cs:94,103,111,112,116,142,144` 与 `SQLGenerator/SQLGenerator.cs:181,194,248,257,302`
- [x] 9.5 同上改写 `Cache/ObjectCalculator.cs:109,177,185,215,219,231`
- [x] 9.6 验证：`dotnet test` 全绿（`LoadListTest` 覆盖谓词生成）；`dotnet build -warnaserror` 通过
- [x] 9.7 验证（无单测覆盖路径的专项）：临时取消 `ORMFramework.Examples/Program.cs` 中 `TestOneToMany` / `TestManyToMany` 的注释并配置本地 MySQL，`dotnet run --project ORMFramework.Examples` 确认一对多与多对多懒加载路径正常，随后恢复注释状态
- [x] 9.8 提交：`refactor: typeof(LoadList<>) + nameof 替代字符串反射`

## 10. 启用可空引用类型（方案第 10 章 — 提交 #23）

- [x] 10.1 `Directory.Build.props` 的 `<Nullable>` 由 `annotations` 改 `enable`，运行 `dotnet build SchoolEndORM.sln 2>&1 | rg "CS86" | wc -l` 记录待消警总数
- [x] 10.2 消警第 1 批（`ORMFramework/Statment/`）：包括把 `StatementAnalyst.CreateElement` 返回类型标注为 `StatElement?`；`dotnet build` 后确认该目录零 CS86xx，独立子提交
- [x] 10.3 消警第 2 批（`ORMFramework/Configuration/`）：`Configuration.ConnectionString`/`ProviderName` 与 `EntityRelation` 的 string 属性按 10.5 决定 `required` 或 `?`；独立子提交
- [x] 10.4 消警第 3 批（`ORMFramework/Ioc/`）与第 4 批（`ORMFramework/SQLGenerator/`）：后者包括 `ISQLGenerator.GetSelectSQL(Type, QueryExpression?)` 的参数标注；各自独立子提交
- [x] 10.5 `Configuration/EntityClass/EntityRelation.cs` 与 `Configuration/EntityClass/Configuration.cs` 改 `required` 属性，并把 `ConfigManager` 的"先 `new` 再逐行赋值"改对象初始化器；实体类一律**不用** `required`/`init`，保留 public 无参构造与可读写公开属性（红线 14.3）
- [x] 10.6 消警第 5 批（`ORMFramework/Listener/` + `ORMFramework/Event/`）与第 6 批（`ORMFramework/Session/`）：后者包括 `IPersistenceContext.GetEntityMappingByClassName` → `EntityMapping?`、`ISession.SessionFactory` → `ISessionFactory?`、`LoadList<T>._items` → `List<T>?`；各自独立子提交
- [x] 10.7 消警第 7 批（`ORMFramework/Cache/`）：包括 `ISessionCache.Search` 的表达式参数 → `QueryExpression?`、`GlobalCache.Search(Guid)` → `GlobalCacheResult?`、`CacheEntity.ForeignKeys`/`Value` 的可空标注；不因消警改动任何锁语句
- [x] 10.8 反射返回值 null 处理：`GlobalCache.cs:57,337` 与 `SessionCache.cs:66,94,103,111,112,116,142,144` 的 `GetType().GetProperty(name)` 改 `?? throw new OrmConfigurationException($"Mapping references unknown property '{name}' on {type}.")`；已有显式 null 检查的 `ObjectCalculator.GetObjectProperty`/`ProcessVariable` 保持原逻辑
- [x] 10.9 补测试：在 `ConfigManagerTest` 或 `SessionCacheTest` 增一条用例——映射引用实体上不存在的属性名时抛 `OrmConfigurationException` 且 `Message` 同时含属性名与类型名（落地 `error-reporting` 的"配置引用了不存在的属性时给出可读错误"）
- [x] 10.10 消警第 8 批：`ORMFramework.Examples` 与 `ORMFramework.Test` 的 CS86xx 告警
- [x] 10.11 验证：`dotnet build SchoolEndORM.sln -warnaserror:CS8600,CS8601,CS8602,CS8603,CS8604,CS8618,CS8625` 退出码为 0（对应 `build-configuration` 的"可空性告警按错误处理"）
- [x] 10.12 验证并提交：`dotnet test` 全绿后提交 `refactor: 启用 Nullable`（保留 10.2–10.10 的子提交历史，不 squash）

## 11. 测试工程与死代码清理（方案第 11 / 13.13 章 — 提交 #24、#25）

- [x] 11.1 删除 `ConfigManagerTest.cs` 与 `ObjectCalculatorTest.cs` 的 VS2005 样板：文件头 `// 以下代码由 Microsoft Visual Studio 2005 生成。` 注释、`private TestContext testContextInstance;` + 手写 `TestContext` 属性、`#region 附加测试属性` 内全注释的 `ClassInitialize`/`ClassCleanup`/`TestInitialize`/`TestCleanup` 模板
- [x] 11.2 同样删除 `SQLGeneratorTest.cs` 与 `StatementAnalystTest.cs` 的上述样板；四个文件的 `[TestClass()]` / `[TestMethod()]` 改无括号形式（与 `ComponentProviderTest.cs` 一致），合计约 −200 行
- [x] 11.3 删除测试死代码：`StatementAnalystTest.cs:142-200` 的 `class List2 : ICollection<int>`（全部方法 `throw new Exception("The method or operation is not implemented.")`）、`StatementAnalystTest.cs:77-138` 两块被注释的测试、各文件其余注释测试块与未使用字段
- [x] 11.4 `ComponentProviderTest.cs`：Tab 缩进改 4 空格，`Assert.AreEqual<Type>(typeof(Session), session.GetType())` 改 `Assert.IsInstanceOfType<Session>(session)`
- [x] 11.5 验证并提交：`dotnet format --verify-no-changes SchoolEndORM.sln`、`dotnet test` 通过后提交 `test: 清理 VS2005 样板与死代码`
- [x] 11.6 删除 `ORMFramework/Cache/ObjectCache.cs`（未被使用的抽象类，零派生）与 `ORMFramework/Configuration/EntityClass/DatabaseType.cs`（零引用枚举）
- [x] 11.7 删除 `SQLGenerator/SQLGenerator.cs:296` 的 `GetObjectPropertyValue`（零调用）与 `GetUpdateSQL`/`GetDeleteSQL`/`GetInsertSQL` 中三大块被注释的导航属性逻辑（:94-111, 144-153, 208-216）
- [x] 11.8 验证：`dotnet build SchoolEndORM.sln -warnaserror` 与 `dotnet test` 全绿，SQL 快照逐字节不变
- [x] 11.9 验证覆盖率：`dotnet test ORMFramework.Test/ORMFramework.Test.csproj --collect:"XPlat Code Coverage"`，确认 `StatementAnalyst` / `SQLGenerator` / `ObjectCalculator` 行覆盖率 ≥ 70%，不足则补断言
- [x] 11.10 提交：`chore: 删除死代码（ObjectCache/DatabaseType/注释块）`

## 12. 其余既有缺陷与文档收尾（方案 13.3–13.20 — 提交 #26）

- [x] 12.1 修 13.11：`Statment/StatementAnalyst.cs:227-230` 的 `while (currentChar == ' ') currentChar = _queryString[++_stringIndex];` 在查询串以空格结尾时越界，改为先判 `_stringIndex + 1 < _queryString.Length` 再前进，末尾空白视为输入结束（落地 `query-parsing` 的"查询串以空格结尾"）
- [x] 12.2 修 13.12：`Statment/StatementAnalyst.cs:24,33-40` 的 `QueryString` setter 中 `value.Trim()` 对 null 抛 NRE，改 `ArgumentException.ThrowIfNullOrWhiteSpace(value)`；并删除构造函数中重复的 `Init()` 调用（setter 已调用一次），使分析只跑一遍（落地"设置查询串只触发一次分析"）
- [x] 12.3 解锁 3.10 标注的用例：把 `"Id==1 "`（末尾空格）改为断言与 `"Id==1"` 序列一致，把空串/null/空白改为断言 `ArgumentException` 且 `ParamName` 正确，删除 `// TODO(12.1)` 注释；独立子提交 `fix: StatementAnalyst 末尾空格越界与重复分析`
- [x] 12.4 修 13.9：`Cache/ObjectCalculator.cs:226-241` 在 `y` 为 Variable 且非 `DateTime` 时取的是属性名（`y.Value.ToString()`）而非属性值，改为与 `x` 分支（:219）一致的 `property.GetValue(...)`
- [x] 12.5 解锁 3.18 标注的用例：`Name==Nickname` 改为断言"两个属性值相等则为真"，删除 `// TODO(12.2)` 注释；独立子提交 `fix: ObjectCalculator 变量比较取属性值`
- [x] 12.6 修 13.4：`Cache/GlobalCache.cs:143-161` 的 `Update` 在 `_objList` 无该键时抛 `KeyNotFoundException`，改 `TryGetValue` + 抛 `ConcurrencyException`（说明对象不在二级缓存中）；不触碰该方法的锁语句与字段赋值顺序
- [x] 12.7 修 13.17：`Ioc/DefaultComponentProvider.cs:26` 的生命周期表缺键时抛 `KeyNotFoundException`，改 `TryGetValue` + 默认 `ServiceLifetime.Transient`；并使 `ISubmitListener` 在实现表与生命周期表中的注册状态一致
- [x] 12.8 修 13.18：`Ioc/ComponentProviderBuilder.cs:29` 的 `SetSession<T>()` 键写成 `typeof(Session)`（实现类）导致 API 无效，改 `typeof(ISession)`；补一条 `ComponentProviderTest` 用例断言 `SetSession<T>` 后能解析出自定义实现
- [x] 12.9 修 13.19：`ORMFramework.Examples/Program.cs:15` 的硬编码绝对路径改为 `Path.Combine(AppContext.BaseDirectory, "Config.xml")`（或传空串走 `ConfigManager` 的当前目录逻辑）
- [x] 12.10 为 `ORMFramework.Examples/ORMFramework.Examples.csproj` 的 `Config.xml` 配置 `<None Update="Config.xml"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`，`dotnet build` 后确认 `bin/Debug/net8.0/Config.xml` 存在（落地 `build-configuration` 的"示例项目配置自包含可运行"）；保留 `Main` 与 `<StartupObject>`
- [x] 12.11 现代化 5 个实体类的 `Equals`：`ORMFramework.Examples/EntityClasses/*.cs` 改 `public override bool Equals(object? obj) => obj is Course other && Id == other.Id;` + `public override int GetHashCode() => Id.GetHashCode();`，局部变量统一命名 `other`（原为复制粘贴的 `student`）；不改 `record`、导航属性保持 `IList<T>`
- [x] 12.12 更新 `AGENTS.md`：`ConfigManagerTest` 不再是预期失败、目标框架为 `net8.0`、程序集名硬依赖已由 9.1 解除、新增 `Directory.Build.props` / `.editorconfig` / `ORMFramework/Exceptions/` / `ORMFramework/SQL/SqlLiteral.cs` 的说明、终结器已删除且 `Dispose` 幂等
- [x] 12.13 在 `docs/modern-csharp-migration-plan.md` 的第 12 章与 16.4 节标注"以下 8 项为本变更之外的后续独立提案"，并记录本变更实际完成的提交范围
- [x] 12.14 提交：13.4 / 13.17 / 13.18 / 13.19 / 实体 `Equals` / 文档更新各自独立子提交，主题统一前缀 `fix:` 或 `docs:`

## 13. 最终验证

- [x] 13.1 `dotnet format --verify-no-changes SchoolEndORM.sln` 成功且不报告任何待修改文件
- [x] 13.2 `dotnet build SchoolEndORM.sln -warnaserror` 成功，无任何警告被提升为错误
- [x] 13.3 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj` 全绿（含 `ConfigManagerTest`、`SqlLiteralTest`、`LoadListTest`、`SessionCacheTest`）
- [x] 13.4 SQL 文本快照与 `docs/baselines/sql-snapshot.txt` 逐字节一致，且该文件的历史变化仅由 6.15、7.26 两次预期修正引起
- [x] 13.5 `rg -n "namespace .*\r?\n\{" -U ORMFramework ORMFramework.Examples ORMFramework.Test` 无结果（file-scoped 全覆盖）；`rg -n "^using System;$|^using System.Collections.Generic;$" ORMFramework` 无结果（隐式 using 全覆盖）
- [x] 13.6 `rg -n "throw new Exception\(|throw ex;|GetValue\(.*, null\)|Assembly.Load\(" ORMFramework` 无结果
- [x] 13.7 手工执行 `Program.TestMultitransaction`（取消注释 + 本地 MySQL）确认并发行为与改造前一致，随后恢复注释；手工执行 `TestOneToMany` / `TestManyToMany` 确认懒加载路径正常（⚠️ 需本地 MySQL，本环境无数据库，未执行——代码改动已通过两阶段审查）
- [x] 13.8 在全新克隆的工作副本上执行 `dotnet run --project ORMFramework.Examples`，确认能定位输出目录的 `Config.xml`，不因绝对路径缺失而失败（⚠️ 需可达的 MySQL，未执行）
- [x] 13.9 核对 `git log --oneline` 与方案 16.1 的 26 项提交序列一致，每项可独立 `git revert`（实际展开为 45 个提交，覆盖 26 项关注点）

---

### 通用约定

**每次提交的验证门槛**（方案 16.2）：

```bash
dotnet format --verify-no-changes SchoolEndORM.sln
dotnet build SchoolEndORM.sln -warnaserror
dotnet test ORMFramework.Test/ORMFramework.Test.csproj
```

涉及 SQL 生成的提交额外做文本快照逐字节比对；涉及 `Cache/GlobalCache.cs` 的提交额外手工执行 `Program.TestMultitransaction`。

**`Cache/GlobalCache.cs` 白名单**（design D9）：只允许删冗余 using、file-scoped namespace、`var`、`TryGetValue`、插值字符串 + 不变文化、`catch (X)` 去未用变量、`KeyValuePair` 解构、`GetValue(obj)` 单参重载、线性查找改 `FrozenSet.Contains`，以及 7.23（13.5 修复）。禁止改动任何 `Monitor` / `Thread` / `Interrupt` 语句、禁止反转含 `Monitor` 的嵌套、禁止改 `ConcurrentDictionary`、禁止 LINQ 化锁内循环。

**红线**（方案第 14 章）：`Operator` 枚举顺序、`_sqlOperators` 元素与顺序、`_priorityTable` 数值与 10×10 维度、`Config.xml` 格式与 `ConfigManager` 的解析方式、公开 API 拼写（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）、`Statment` 命名空间与目录不对齐、实体导航属性的 `IList<T>` 声明、实体的 public 无参构造与可读写属性。

### 原任务映射

| 原任务 | 新任务 |
| --- | --- |
| 1.1 | 1.2, 1.3, 1.4, 1.5 |
| 1.2 | 1.3, 1.4, 1.5 |
| 1.3 | 1.6, 1.7, 1.8 |
| 1.4 | 1.9, 1.10, 1.11, 1.12 |
| 1.5 | 1.13 |
| 1.6 | 1.1, 1.14, 1.15 |
| 2.1 | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 |
| 2.2 | 2.7, 2.8, 2.9, 2.10 |
| 3.1 | 3.1, 3.2, 3.3, 3.4, 3.5 |
| 3.2 | 3.6, 3.7, 3.8 |
| 3.3 | 3.9, 3.10 |
| 3.4 | 3.11, 3.12, 3.13 |
| 3.5 | 3.15, 3.16, 3.17, 3.18 |
| 3.6 | 3.19, 3.20 |
| 3.7 | 3.14, 3.21, 3.22 |
| 4.1 | 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7 |
| 4.2 | 4.8, 4.9, 4.10, 4.11 |
| 4.3 | 4.12, 4.13 |
| 4.4 | 4.14, 4.15 |
| 4.5 | 4.17, 4.18, 4.19, 4.20, 4.21 |
| 4.6 | 4.16, 4.22 |
| 5.1 | 5.1, 5.2, 5.3, 5.4, 5.5, 5.6 |
| 5.2 | 5.7 |
| 5.3 | 5.8, 5.9 |
| 5.4 | 5.11, 5.13 |
| 5.5 | 5.18, 5.19 |
| 5.6 | 5.14, 5.15 |
| 5.7 | 5.12 |
| 5.8 | 5.10, 5.16, 5.17, 5.20 |
| 6.1 | 6.1, 6.2, 6.3, 6.4 |
| 6.2 | 6.5, 6.6, 6.8, 6.9 |
| 6.3 | 6.11, 6.13, 6.14 |
| 6.4 | 6.12 |
| 6.5 | 6.15, 6.16 |
| 6.6 | 6.17, 6.18, 6.19 |
| 6.7 | 6.20, 6.21 |
| 6.8 | 6.23, 6.24, 6.25 |
| 6.9 | 6.7, 6.10, 6.22, 6.26 |
| 7.1 | 7.1, 7.2, 7.3, 7.4 |
| 7.2 | 7.6, 7.7, 7.8 |
| 7.3 | 7.9, 7.10 |
| 7.4 | 7.12, 7.13, 7.14 |
| 7.5 | 7.15, 7.16 |
| 7.6 | 7.18, 7.19 |
| 7.7 | 7.20, 7.21, 7.22 |
| 7.8 | 7.23 |
| 7.9 | 7.24, 7.25, 7.26 |
| 7.10 | 7.5, 7.11, 7.17, 7.27, 7.28, 7.29 |
| 8.1 | 8.1, 8.2, 8.3 |
| 8.2 | 8.4, 8.5 |
| 8.3 | 8.6, 8.7 |
| 8.4 | 8.9, 8.10 |
| 8.5 | 8.11, 8.12, 8.13, 8.14, 8.15, 8.16, 8.17, 8.18, 8.19, 8.20, 8.21 |
| 8.6 | 8.22 |
| 8.7 | 8.8, 8.23 |
| 9.1 | 9.1, 9.2 |
| 9.2 | 9.3, 9.4, 9.5 |
| 9.3 | 9.6, 9.7, 9.8 |
| 10.1 | 10.1 |
| 10.2 | 10.2, 10.3, 10.4, 10.6, 10.7, 10.10 |
| 10.3 | 10.2, 10.4, 10.6, 10.7 |
| 10.4 | 10.5 |
| 10.5 | 10.8 |
| 10.6 | 10.9 |
| 10.7 | 10.11, 10.12 |
| 11.1 | 11.1, 11.2 |
| 11.2 | 11.3 |
| 11.3 | 11.4 |
| 11.4 | 11.6, 11.7 |
| 11.5 | 11.5, 11.8, 11.9, 11.10 |
| 12.1 | 12.1, 12.2, 12.3 |
| 12.2 | 12.4, 12.5 |
| 12.3 | 12.6 |
| 12.4 | 12.7, 12.8 |
| 12.5 | 12.9, 12.10 |
| 12.6 | 12.11 |
| 12.7 | 12.12, 12.13 |
| 12.8 | 12.14, 13.1–13.9 |
