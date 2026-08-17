# SchoolEndORM 现代 C# 语法改造方案

> 文档性质：**改造方案（只读分析）**，本文不包含任何代码修改。
> 分析范围：`ORMFramework`（46 个 .cs）、`ORMFramework.Examples`（6 个 .cs）、`ORMFramework.Test`（5 个 .cs），以及 3 个 `.csproj`。
> 编写时间：2026-08。

---

## 目录

- [1. 现状评估](#1-现状评估)
- [2. 改造目标与原则](#2-改造目标与原则)
- [3. 第 0 阶段：工程基础设施](#3-第-0-阶段工程基础设施)
- [4. 第 1 阶段：机械化语法现代化（零语义变更）](#4-第-1-阶段机械化语法现代化零语义变更)
- [5. 第 2 阶段：类型与成员声明现代化](#5-第-2-阶段类型与成员声明现代化)
- [6. 第 3 阶段：控制流与模式匹配现代化](#6-第-3-阶段控制流与模式匹配现代化)
- [7. 第 4 阶段：集合、字符串与 API 现代化](#7-第-4-阶段集合字符串与-api-现代化)
- [8. 第 5 阶段：资源管理与异常处理现代化](#8-第-5-阶段资源管理与异常处理现代化)
- [9. 第 6 阶段：反射与 nameof 强类型化](#9-第-6-阶段反射与-nameof-强类型化)
- [10. 第 7 阶段：可空引用类型（NRT）启用](#10-第-7-阶段可空引用类型nrt启用)
- [11. 第 8 阶段：测试工程现代化](#11-第-8-阶段测试工程现代化)
- [12. 可选/高风险：并发模型与异步化](#12-可选高风险并发模型与异步化)
- [13. 改造中必然暴露的既有缺陷清单](#13-改造中必然暴露的既有缺陷清单)
- [14. 红线：禁止改动的部分](#14-红线禁止改动的部分)
- [15. 逐文件改造清单](#15-逐文件改造清单)
- [16. 执行顺序、验证与回滚](#16-执行顺序验证与回滚)

---

## 1. 现状评估

### 1.1 语法代际画像

代码目标框架已是 `net6.0`，但**语法风格整体停留在 C# 2.0 ~ 3.0**（2005–2007 年），只有零星使用了自动属性（C# 3.0）。典型特征统计：

| 过时特征 | 出现规模 | 首个可替代的 C# 版本 |
| --- | --- | --- |
| 块级 `namespace { }` 缩进 | 57/57 文件 | C# 10（file-scoped） |
| 冗余 `using System.Text;` 等样板 using | ~50 文件 | C# 10（ImplicitUsings） |
| 手写 `private T _x;` + `get{return _x;} set{_x=value;}` | 8 个类、约 30 个属性 | C# 3.0（自动属性） |
| `string.Format("{0}", x)` | 约 20 处 | C# 6（插值字符串） |
| `new Xxx(); x.A=..; x.B=..;` 逐行赋值 | 约 25 处 | C# 3.0（对象初始化器） |
| `switch` 语句用于纯映射 | 6 处 | C# 8（switch 表达式） |
| `if (...) return true; else return false;` | 8 处 | — （直接返回表达式） |
| `new char[]{...}` / `new Type[]{...}` | 5 处 | C# 12（集合表达式） |
| `ContainsKey` + 索引器双查找 | 约 30 处 | .NET 2.0（`TryGetValue`） |
| `throw new Exception("...")` | 28 处 | — （具体异常类型） |
| `throw ex;`（丢栈） | 3 处 | — （`throw;`） |
| 手写 `Close()` 而非 `using` | ConfigManager 8 处 | C# 8（using 声明） |
| 终结器 `~Class()` 里访问托管对象 | 2 处 | — （标准 Dispose 模式） |
| 字符串按名反射 | 6 处 | C# 6（`nameof`）/ `typeof(T<>)` |
| VS2005 生成的 `TestContext` 样板 | 4/5 测试文件 | — |

### 1.2 工程配置画像

- 三个 `.csproj` 均为 `net6.0`（**已于 2024-11 结束支持**），无 `LangVersion`、无 `Nullable`、无 `ImplicitUsings`、无分析器配置。
- 无 `Directory.Build.props`、无 `.editorconfig`、无 `Directory.Packages.props`。
- `ORMFramework.csproj` 残留 `<None Remove="Ioc\" />` + `<Folder Include="Ioc\" />` 这类 IDE 自动生成的无意义 item。
- `ORMFramework.Examples.csproj` 残留 `<WarningLevel>4</WarningLevel>`（.NET SDK 默认已是 4）。
- `obj/` 下同时存在 `net5.0` 与 `net6.0` 产物，说明历史升级未清理。
- `ORMFramework/ORMFramework.csproj` 显式引用 `Microsoft.NETCore.Platforms` 7.0.2 —— 该包在 net5+ 已无需显式引用。

### 1.3 结论

改造性质应界定为**"语法与工程规范现代化"**，而非重写。框架的架构（Session/Listener/Event/双层缓存/表达式解析三份实现）是本项目的核心价值，本方案**不触碰架构**，只把表达形式升级到 C# 12 / .NET 8。

---

## 2. 改造目标与原则

### 2.1 目标

1. 语言版本推进到 **C# 12**，目标框架推进到 **`net8.0`（LTS）**。
2. 全量启用 `ImplicitUsings`、`Nullable`、.NET 分析器；使 `dotnet build -warnaserror` 通过。
3. 消除全部"C# 2.0 时代"表达形式，代码可读性对齐现代 .NET 项目。
4. 保持**公开 API 二进制/源码兼容**（除第 14 节允许的例外），保持 `Config.xml` 格式 100% 兼容。
5. 每个阶段结束后 `dotnet build` + `dotnet test` 均通过，可独立提交、可独立回滚。

### 2.2 原则

- **语义等价优先**：语法改造不得顺手"修 bug"。发现的缺陷统一登记到第 13 节，作为独立提交、独立 PR 处理。
- **单一关注点提交**：一个提交只做一类语法变换（例如"全项目 file-scoped namespace"），便于 review 和 `git bisect`。
- **分析器驱动**：优先用 `dotnet format` + Roslyn IDExxxx 规则批量完成，人工只处理分析器无法自动修复的部分。
- **高危文件后置**：`Cache/GlobalCache.cs`（手写不平衡锁）与 `Statment/StatementAnalyst.cs`（优先级表索引强耦合）放到最后，且必须先补测试。

---

## 3. 第 0 阶段：工程基础设施

> 这是所有后续阶段的前置条件。没有分析器和统一配置，后面的改造无法自动化验证。

### 3.1 升级目标框架

`net6.0` → `net8.0`（三个项目一致）。

理由：`net6.0` 已 EOL；`net8.0` 是当前 LTS，且第 4/7 阶段依赖的 `char.IsAsciiLetter`（.NET 7）、`SearchValues<T>`、`FrozenSet<T>`、集合表达式（C# 12）都要求 net8。

配套动作：
- 删除 `ORMFramework.csproj` 中的 `Microsoft.NETCore.Platforms` 包引用（net8 下多余）。
- 删除 `<None Remove="Ioc\" />` / `<ItemGroup><Folder Include="Ioc\" /></ItemGroup>`。
- 删除 `ORMFramework.Examples.csproj` 中的 `<PropertyGroup Condition="...Debug|AnyCPU">` 段。
- 清理 `obj/Debug/net5.0`、`obj/Debug/net6.0` 残留（`git clean` 范围内，不入库）。
- `MySql.Data` 8.0.32.1 → 9.x（net8 兼容版本）；`MSTest` 3.0.2 → 3.6+；`log4net` 2.0.15 → 3.x（注意 log4net 3.x 有 API 变动，见 12.3）。

### 3.2 新增 `Directory.Build.props`（仓库根）

集中管理三个项目的公共属性，避免在每个 csproj 重复：

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>              <!-- 第 7 阶段才真正打开，之前先 annotations -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

> 注意：`Nullable` 一次性打开会产生数百个警告。建议第 0 阶段先设 `<Nullable>annotations</Nullable>`（只允许写 `?`，不产生警告），到第 7 阶段再切 `enable`。

### 3.3 新增 `.editorconfig`（仓库根）

用于把"风格约定"变成可机器执行的规则，`dotnet format` 才能批量改。关键项（与本项目现状匹配）：

```ini
[*.cs]
# 保留项目现有的 Allman 大括号风格
csharp_new_line_before_open_brace = all

# 现代语法偏好（对应第 1~6 阶段）
csharp_style_namespace_declarations = file_scoped:warning        # IDE0161
csharp_style_var_for_built_in_types = false:silent
csharp_style_var_when_type_is_apparent = true:suggestion         # IDE0007
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_implicit_object_creation_when_type_is_apparent = true:suggestion  # IDE0090
csharp_style_prefer_switch_expression = true:suggestion          # IDE0066
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_prefer_not_pattern = true:suggestion
csharp_style_prefer_index_operator = true:suggestion             # IDE0056
csharp_style_prefer_range_operator = true:suggestion             # IDE0057
csharp_prefer_simple_using_statement = true:suggestion           # IDE0063
csharp_style_throw_expression = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion      # out var
dotnet_style_object_initializer = true:suggestion                # IDE0017
dotnet_style_collection_initializer = true:suggestion            # IDE0028
dotnet_style_prefer_auto_properties = true:warning               # IDE0032
dotnet_style_readonly_field = true:suggestion                    # IDE0044
dotnet_diagnostic.IDE0005.severity = warning                     # 冗余 using

# 统一 this. 前缀（项目现状不一致：DefaultDbDriverFactory 全用，Session 全不用）
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning

# 命名规范：私有字段 _camelCase
dotnet_naming_rule.private_field_underscore.symbols = private_fields
dotnet_naming_rule.private_field_underscore.style = underscore_prefix
dotnet_naming_rule.private_field_underscore.severity = warning
```

> 决策点：`this.` 前缀选择"不加"（多数文件已如此），代价是 `DefaultDbDriverFactory.cs`、`SessionFactoryIoc.cs`、`PersistenceContext.cs`、`Ioc/*.cs` 会有较大 diff（纯格式）。这些 diff 独立成一个提交。

### 3.4 阶段验证

`dotnet build SchoolEndORM.sln` 通过（允许有警告），`dotnet test` 结果与改造前一致（`ConfigManagerTest` 的既有失败保持失败，见 13.16）。

---

## 4. 第 1 阶段：机械化语法现代化（零语义变更）

本阶段全部由 `dotnet format` / IDE 批量修复完成，人工只需 review diff。

### 4.1 文件范围命名空间（C# 10，IDE0161）

**涉及：全部 57 个 .cs 文件。**

```csharp
// 现状（ORMFramework/Statment/Operator.cs）
namespace ORMFramework.Statment
{
    public enum Operator { ... }
}

// 目标
namespace ORMFramework.Statment;

public enum Operator { ... }
```

收益：每个文件减少一层缩进，`GlobalCache.cs`（541 行）、`StatementAnalyst.cs`（605 行）可读性提升明显。

注意：`Statment/StatElement.cs` 一个文件里有 `class StatElement` + `enum StatElementType`，`Cache/GlobalCacheEntity.cs` 有 `class GlobalCacheEntity` + `enum Lock`，`Listener/IListeners.cs` 有 6 个接口 —— file-scoped 对多类型文件同样适用，无需拆分（拆分留作可选项，见 5.6）。

### 4.2 隐式 using + 清理冗余 using（IDE0005）

**涉及：约 50 个文件。**

几乎所有文件都带着 VS2005 模板留下的三件套：

```csharp
using System;
using System.Collections.Generic;
using System.Text;   // ← 绝大多数文件根本不用 StringBuilder
```

`ImplicitUsings` 会自动提供 `System`、`System.Collections.Generic`、`System.Linq`、`System.Threading`、`System.Threading.Tasks` 等，因此这些行整体删除。

需要**保留显式 using** 的（非隐式集合成员）：
- `System.Data`、`System.Data.Common`（`Session.cs`、`PersistenceContext.cs`、`IDbDriverFactory.cs`、`DefaultDbDriverFactory.cs`、`DefaultCommandListener.cs`、`GlobalCache.cs`、`SessionCache.cs`）
- `System.Reflection`、`System.Text.RegularExpressions`、`System.Xml`、`System.IO`
- 项目自身命名空间 `ORMFramework.*`
- `log4net.*`、`Microsoft.Extensions.DependencyInjection`、`Microsoft.VisualStudio.TestTools.UnitTesting`

**必须单独指出**：`SessionFactory/DefaultDbDriverFactory.cs:6` 的 `using System.Configuration.Provider;` 是 .NET Framework 遗留命名空间，当前文件完全没用到，**删除**。

`ORMFramework.Examples/EntityClasses/*.cs` 五个实体类的 `using System; using System.Collections.Generic; using System.Text;` 中，`Course.cs`/`Department.cs` 实际需要 `IList<T>`（由隐式 using 覆盖），因此全部三行可删。

### 4.3 目标类型 `new`（C# 9，IDE0090）

**涉及：约 60 处。**

```csharp
// 现状（Cache/SessionCache.cs:14-18）
private Dictionary<object, CacheEntity> _objList = new Dictionary<object, CacheEntity>();
private Dictionary<Type, List<CacheEntity>> _objTable = new Dictionary<Type, List<CacheEntity>>();

// 目标
private readonly Dictionary<object, CacheEntity> _objList = new();
private readonly Dictionary<Type, List<CacheEntity>> _objTable = new();
```

同时顺带把从未重新赋值的字段加上 `readonly`（IDE0044）：`SessionCache._objList/_objTable/_insertList/_updateList/_deleteList`、`Session._insertListeners/_deleteListeners/_updateListeners/_submitListeners`、`GlobalCache._objTable/_objList`、`Configuration._mappings`。

> 例外：`GlobalCache.CACHEMAXCOUNT` 在 `Collect()` 中会被修改，**不能** `readonly`（见 4.6）。

### 4.4 局部变量 `var`（IDE0007）

**涉及：约 200 处。** 仅在右侧类型显然时使用（`new`、强制转换、`typeof`）：

```csharp
// 现状（SQLGenerator/SQLGenerator.cs:76-80）
StringBuilder sql = new StringBuilder("update ");
StringBuilder where = new StringBuilder(" where (");
EntityMapping mapping = this.PersistenceContext.GetEntityMappingByClassName(...);
PropertyInfo[] properties = oldObject.GetType().GetProperties();

// 目标
var sql = new StringBuilder("update ");
var where = new StringBuilder(" where (");
var mapping = PersistenceContext.GetEntityMappingByClassName(...);   // 方法返回值：保守起见可保留显式类型
var properties = oldObject.GetType().GetProperties();
```

策略：`new` 表达式与强制转换 → `var`；方法返回值 → 保留显式类型（`csharp_style_var_when_type_is_apparent = true` + `var_elsewhere = false:silent`），避免可读性下降。

**⚠️ 陷阱**：`Cache/SessionCache.cs:428` 的 `foreach (Guid objectId in _objList.Keys)`（`_objList` 是 `Dictionary<object, ...>`）一旦改成 `var` 就会**编译期类型不匹配**并暴露一个运行时 bug。见 13.1，必须先修 bug 再改语法。

### 4.5 变量声明前移消除（C# 7 `out var` / 就近声明）

VS2005 风格把所有局部变量声明在方法开头，现代风格就近声明：

```csharp
// 现状（Cache/SessionCache.cs:292-301）
IDbDriverFactory driverFactory = this.PersistenceContext.DbDriverFactory;
IDbConnection conn = driverFactory.GetDbConnection();
IDbCommand cmd;
IDbTransaction tran;
conn.Open();
tran = conn.BeginTransaction();
cmd = conn.CreateCommand();

// 目标
var driverFactory = PersistenceContext.DbDriverFactory;
var conn = driverFactory.GetDbConnection();
conn.Open();
var tran = conn.BeginTransaction();
var cmd = conn.CreateCommand();
```

同类位置：`SessionCache.ProcessQueryCommand`（`DataTable dt;` 前置）、`GlobalCache.ReadDataFromDatabase`（`DataTable dt;`、`cacheEntity`、`cacheResult` 前置）、`Session.Search<T>`、`LoadList.GetItems`、`ObjectCalculator.ProcessNotBooleanOperator`（`object xValue; object yValue;`）、`SQLGenerator.GetSelectSQL`（`List<StatElement> expression;`）、`StatementAnalyst.ReadElement`（`char currentChar;`）。

### 4.6 命名规范修正（私有字段/常量）

```csharp
// 现状（Cache/GlobalCache.cs:15-16）—— 全大写但是可变 static 字段
private static int CACHEMAXCOUNT = 1000;
private static int INCREASE = 100;

// 目标
private const int CacheIncrease = 100;      // 从不变化 → const
private static int _cacheMaxCount = 1000;   // Collect() 会递增 → 可变字段命名
```

```csharp
// 现状（Ioc/ComponentProviderBuilder.cs:11、Ioc/DefaultComponentProvider.cs:12,14,29）
private readonly IDictionary<Type, Type> componentImplTypes = ...;
private readonly IDictionary<Type, ServiceLifetime> componentLiftimeDict = ...;  // 拼写错误
private readonly ServiceProvider serviceProvider;

// 目标
private readonly Dictionary<Type, Type> _componentImplTypes = ...;
private readonly Dictionary<Type, ServiceLifetime> _componentLifetimes = ...;
private readonly ServiceProvider _serviceProvider;
```

同类：`ORMFramework.Test/*.cs` 的 `testContextInstance`（第 8 阶段整体删除）、`SQLGeneratorTest.pesistenceContext`（拼写错误）。

> 私有标识符改名是安全的（编译期可验证），公开 API 拼写错误见第 14 节。

### 4.7 阶段验证

`dotnet format --verify-no-changes` 通过；`git diff --stat` 应显示"删除行数 ≫ 新增行数"（预计净减 400+ 行）。

---

## 5. 第 2 阶段：类型与成员声明现代化

### 5.1 手写属性 → 自动属性（IDE0032）

**涉及 8 个类、约 30 个属性。** 这是本次改造中删除代码量最大的一项。

```csharp
// 现状（Statment/StatElement.cs:7-23）—— 23 行
public class StatElement
{
    private object _value;
    private StatElementType _type;

    public object Value
    {
        get { return _value; }
        set { _value = value; }
    }

    public StatElementType Type
    {
        get { return _type; }
        set { _type = value; }
    }
}

// 目标 —— 5 行
public class StatElement
{
    public object Value { get; set; }
    public StatElementType Type { get; set; }
}
```

清单：

| 文件 | 属性 | 目标形式 |
| --- | --- | --- |
| `Statment/StatElement.cs` | `Value`、`Type` | `{ get; set; }` |
| `Statment/QueryExpression.cs` | `NifixExpression`、`SuffixExpression`、`Expression` | `{ get; }`（构造函数赋值） |
| `Configuration/ConfigManager.cs` | `ConfigFilePath` | `{ get; set; }` |
| `Configuration/EntityClass/Configuration.cs` | `Mappings` | `{ get; } = [];` |
| `Session/LoadList.cs` | `SessionCache`、`ForeignKeyName`、`ForeignKeyValues`、`SelectListener` | `{ get; set; }` / `{ get; }` |
| `Session/PersistenceContext.cs` | `DbDriverFactory` | `{ get; }` |
| `Session/Session.cs` | `SelectListener`、`InsertListeners`、`DeleteListeners`、`UpdateListeners`、`SubmitListeners`、`CommandListener`、`PersistenceContext` | `{ get; }` + 构造/初始化器赋值 |
| `Cache/GlobalCacheEntity.cs` | `BlockThreads` | `{ get; } = new();` |
| `Cache/SessionCache.cs` | —（已是自动属性） | — |
| `Examples/EntityClasses/Course.cs` | `Id`、`Name`、`Classroom`、`Students` | `{ get; set; }` |
| `Examples/EntityClasses/Teacher.cs` | `Id`、`Name`、`Gender`、`Department` | `{ get; set; }` |
| `Examples/EntityClasses/Department.cs` | `Id`、`Name`、`Teachers` | `{ get; set; }` |
| `Examples/EntityClasses/ContartMenu.cs` | `Id`、`Email`、`Phone`、`Address`、`Student` | `{ get; set; }` |

**⚠️ 实体类特别注意**：框架通过反射读写 `public` 属性（`GlobalCache.CloneObject`、`SQLGenerator.GetInsertSQL` 等）。自动属性对反射完全等价（`PropertyInfo.GetValue/SetValue` 行为不变），**安全**。但 `Examples/EntityClasses/Student.cs` 已经是自动属性，可作为改造后的参照样板。

### 5.2 延迟初始化属性 → 字段初始化器

```csharp
// 现状（Configuration/EntityClass/EntityMapping.cs:16-40）
public IEnumerable<string> Keys
{
    get { if (_keys == null) _keys = new List<string>(); return _keys; }
    set { _keys = value; }
}
public IDictionary<string, EntityRelation> Relations
{
    get { if (_relations == null) _relations = new Dictionary<string, EntityRelation>(); return _relations; }
    set { _relations = value; }
}

// 目标
public IEnumerable<string> Keys { get; set; } = [];
public IDictionary<string, EntityRelation> Relations { get; set; } = new Dictionary<string, EntityRelation>();
```

语义差异需确认：原写法在 `set(null)` 之后 `get` 会重新给出空集合；新写法会返回 `null`。检查调用方 —— `ConfigManager.ReadMap` 只做 `map.Relations.Add(...)`、`map.Keys = ...Split(...)`，`GlobalCache`/`SessionCache` 只读，**没有任何地方赋 null**，因此等价。为保险可保留 `set` 的空值合并：`set => _relations = value ?? new();`。

### 5.3 只读成员 → 表达式体（C# 6/7）

```csharp
// 现状（Session/Session.cs:25-63）
public ISelectListener SelectListener { get { return _selectListener; } }
public IDbConnection Connection { get { return _persistenceContext.Connection; } }

// 目标
public ISelectListener SelectListener { get; }
public IDbConnection Connection => PersistenceContext.Connection;
```

```csharp
// 现状（SQLGenerator/SQLGenerator.cs:24-27, 236-239）
public string GetSelectSQL(Type type) { return GetSelectSQL(type, null); }
private string GetSQLOperator(Operator @operator) { return _sqlOperators[(int)@operator]; }

// 目标
public string GetSelectSQL(Type type) => GetSelectSQL(type, null);
private static string GetSQLOperator(Operator @operator) => _sqlOperators[(int)@operator];
```

同类：`Ioc/DefaultComponentProvider.GetComponent<T>`、`Ioc/ComponentProviderBuilder.Build`、`Configuration/ConfigManager.ConfigFilePath`、`LoadList` 中 12 个成员（`GetItems()` 守卫可用局部方法收敛，见 5.5）、`SessionFactory.GetEntityMappingByClassName`、`SessionFactoryIoc.Initialize()`、全部 5 个 Listener 的 `OnXxx`、`ObjectCalculator.IsBooleanOperator`。

### 5.4 主构造函数（C# 12）

适用于"构造函数只做字段赋值"的类：

```csharp
// 现状（Configuration/ConfigManager.cs:19-27）
public ConfigManager()
{
    _configFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Config.xml";
}
public ConfigManager(string configFilePath) { _configFilePath = configFilePath; }

// 目标（C# 12 主构造函数 + 默认参数）
public class ConfigManager(string? configFilePath = null)
{
    public string ConfigFilePath { get; set; } =
        string.IsNullOrEmpty(configFilePath)
            ? Path.Combine(Environment.CurrentDirectory, "Config.xml")
            : configFilePath;
}
```

顺带修掉 `Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Config.xml"` 的手工拼接 → `Path.Combine`（`LogHelper.InitLog:19` 同样问题）。

候选清单：`ConfigManager`、`PersistenceContext`（两个构造函数重载，只有部分可用主构造）、`GlobalCacheResult`、`QueryExpression`、`DefaultComponentProvider`。

> 保守建议：`Session`（3 个链式构造函数）**不要**改主构造函数，链式 `: this(...)` 与主构造函数混用可读性反而下降。

### 5.5 `LoadList<T>` 的重复守卫收敛

现状 12 个成员每个都写：

```csharp
if (_items == null) { GetItems(); }
```

现代化目标（保持懒加载语义不变）：

```csharp
private List<T> Items => _items ??= LoadItems();   // C# 8 空合并赋值

public int Count => Items.Count;
public bool Contains(T item) => Items.Contains(item);
public void Add(T item) => Items.Add(item);
// ...
public T this[int index]
{
    get => Items[index];
    set => Items[index] = value;
}
IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();
```

`GetItems()` 由"填充字段的 void 方法"改为"返回 `List<T>` 的 `LoadItems()`"。**注意**：这是本方案中唯一涉及 `LoadList<T>` 的改动，`ForeignKeyName` / `ForeignKeyValues` / `SessionCache` 三个属性名**必须保持不变**（`SessionCache.GetReferenceClasses` 用字符串反射访问，见第 9 阶段）。

### 5.6 `record` 化（谨慎、可选）

只对**真正不可变的纯数据载体**使用：

| 类型 | 建议 | 理由 |
| --- | --- | --- |
| `Statment/QueryExpression` | ✅ `internal sealed record QueryExpression(List<StatElement> NifixExpression, List<StatElement> SuffixExpression, string Expression)` | 三个属性全只读，构造函数 internal |
| `Cache/GlobalCacheResult` | ⚠️ 可改，但 `ForeignKeys` 在外部被赋值（`GlobalCache.cs:105,115,329,476`）→ 需保留 `{ get; set; }` |
| `Statment/StatElement` | ❌ **不要** | `ObjectCalculator.ProcessNotOperator:100` 直接修改 `element.Value`，是可变对象 |
| `Cache/CacheEntity` / `GlobalCacheEntity` | ❌ **不要** | 全程可变，且 `GlobalCacheEntity` 作为 `Monitor` 锁对象 —— record 的值相等语义与"引用即锁"冲突，极危险 |
| `Event/ActionEvent` 系列 | ❌ **不要** | Listener 通过 `@event.Result = ...` 回写，是可变输出参数 |

**⚠️ 关键风险**：`record` 会重写 `Equals`/`GetHashCode` 为值相等。`SessionCache._objList` 是 `Dictionary<object, CacheEntity>`，键是**实体对象**；`GlobalCache` 用 `lock(cacheEntity)`。任何把这些类型改成 `record` 的动作都会改变字典行为和锁行为。**除 `QueryExpression` 外一律不改。**

同理，`sealed` 关键字可安全加到：所有 Listener 实现、`SessionCache`、`Session`、`PersistenceContext`、`DefaultDbDriverFactory`、`LoadList<T>`、`SQLGenerator`、`ConfigManager`、`DefaultComponentProvider`、`ComponentProviderBuilder`、`SessionFactory`、`SessionFactoryIoc`。**不要** `sealed` 的：`EntityRelation`（被 `ManyToManyRelation` 继承）、`CacheEntity`（被 `GlobalCacheEntity` 继承）、`ActionEvent`（被 `SelectEvent`/`UpdateEvent` 继承）。

### 5.7 静态类修正

`LogHelper/LogHelper.cs` 是 `public class LogHelper`，只有静态成员 → `public static class LogHelper`（CA1052）。

`Cache/ObjectCache.cs` 是**完全未被使用的抽象类**（无任何派生类，`GlobalCache` 自己重复定义了 `_objTable`/`_objList`）→ 列入死代码清单（13.13），建议独立提交删除。

### 5.8 阶段验证

`dotnet build` 无新增警告；`dotnet test` 通过。这一阶段预计再净减 300+ 行。

---

## 6. 第 3 阶段：控制流与模式匹配现代化

### 6.1 `switch` 语句 → `switch` 表达式（C# 8，IDE0066）

**6 处纯映射型 switch。** 最典型的是 `StatementAnalyst.GetOperatorByString`：

```csharp
// 现状（Statment/StatementAnalyst.cs:188-219）—— 32 行
private Operator GetOperatorByString(string operatorStr)
{
    switch (operatorStr.ToLower())
    {
        case "==": return Operator.Equals;
        case "!=": return Operator.UnEquals;
        // ... 10 more
        default: throw new Exception(string.Format("Invaild Operator:{0}", operatorStr));
    }
}

// 目标 —— 16 行
private static Operator GetOperatorByString(string operatorStr) => operatorStr.ToLowerInvariant() switch
{
    "==" => Operator.Equals,
    "!=" => Operator.UnEquals,
    ">"  => Operator.Greater,
    ">=" => Operator.GreaterEquals,
    "<"  => Operator.Less,
    "<=" => Operator.LessEquals,
    "&&" => Operator.And,
    "||" => Operator.Or,
    "!"  => Operator.Not,
    "like" => Operator.Like,
    "("  => Operator.LeftParenthesis,
    ")"  => Operator.RightParenthesis,
    _ => throw new QuerySyntaxException($"Invalid operator: {operatorStr}"),
};
```

> 顺带把 `ToLower()` → `ToLowerInvariant()`（CA1311：`ToLower()` 依赖当前 culture，土耳其语环境下 `"LIKE".ToLower()` 会产生 `"lıke"`，导致解析失败）。同一问题也在 `StatementAnalyst.CreateElement:134`。

其余 5 处：
- `ObjectCalculator.CalculateBooleanOperator`（:296-307）→ switch 表达式
- `ObjectCalculator.CalculateNotBooleanOperator`（:309-328）→ switch 表达式
- `ObjectCalculator.CalculateStringOperator` 尾部 switch（:250-260）→ switch 表达式
- `Ioc/DefaultComponentProvider` 生命周期 switch（:39-50）→ switch 表达式返回注册委托，或保留语句形式（有副作用，保留亦可）
- `ConfigManager.ReadMap` 的 `node.Name` switch（:187-200）→ switch 表达式赋值 `relation.Type`

`ConfigManager.GetSystemConfiguration` / `ReadSessionFactory` 的 switch（有副作用 + 赋值 `result`）→ 保留语句形式，但用**属性模式**简化：`case "SessionFactory": ...`。

### 6.2 `if/else return true/false` → 直接返回布尔表达式

**8 处。** 最夸张的是 `StatementAnalyst.IsVariableChar`：

```csharp
// 现状（Statment/StatementAnalyst.cs:98-120）—— 23 行
private bool IsVariableChar(char c)
{
    if (c >= 'a' && c <= 'z') return true;
    else if (c >= 'A' && c <= 'Z') return true;
    else if (c >= '0' && c <= '9') return true;
    else if (c == '_') return true;
    else return false;
}

// 目标（C# 9 关系模式 + or 模式）—— 2 行
private static bool IsVariableChar(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';

// 或使用 .NET 7 API（语义完全等价，且意图更清晰）
private static bool IsVariableChar(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';
private static bool IsVariableFirstChar(char c) => char.IsAsciiLetter(c) || c == '_';
```

> ⚠️ 不要误用 `char.IsLetterOrDigit(c)` —— 它对 Unicode 字母（如中文）返回 true，会**改变词法分析器的行为**。必须用 `IsAsciiLetterOrDigit`。

其余位置：
- `StatementAnalyst.ReadNotOpeartion`（:428-435）、`ReadLeftParenthesis`（:447-458）、`ReadRightParenthesis`（:470-477）、`ReadVariableOrConstant`（:490-501）、`ReadOperator`（:513-528）—— 这 5 个方法的 `if(...) return true; else if(...) return true; else return false;` 全部可折叠为单个布尔表达式，但**这些是语法分析器的状态转移判定，改造前必须有覆盖测试**（见 11.4）。
- `ObjectCalculator.IsString`（:343-373）
- `Configuration` / `ConfigManager` 中的 `if (result && ...) return true; else return false;`（`ReadSessionFactory`、`ReadConnectionString`、`ReadProviderName`，共 3 处）
- `SQLGenerator.IsBasicType` / `IsStringType`（:272-294）→ 见 7.2
- `GlobalCache.IsBasicType`（:378-388）→ 见 7.2
- `GlobalCache.GetObjectVersion`（:201-211）→ `_objList.TryGetValue(objectId, out var e) ? e.Version : -1`

### 6.3 类型检查模式匹配（C# 7/9）

```csharp
// 现状（Cache/GlobalCache.cs:408-423，另有 4 处几乎完全相同的副本）
if (o is string || o is char)
{
    sql.Append("'"); sql.Append(o); sql.Append("'");
}
else if (o is DateTime)
{
    sql.Append("'"); sql.Append(((DateTime)o).ToString("yyyy-MM-dd HH:mm:ss")); sql.Append("'");
}
else { sql.Append(o); }

// 目标（switch 表达式 + 声明模式 + or 模式）
sql.Append(o switch
{
    string or char => FormattableString.Invariant($"'{o}'"),
    DateTime dt    => FormattableString.Invariant($"'{dt:yyyy-MM-dd HH:mm:ss}'"),
    _              => Convert.ToString(o, CultureInfo.InvariantCulture),
});
```

这段"字面量格式化"逻辑在项目中被**复制了 5 遍**：
1. `Cache/GlobalCache.cs:408-423`（`ReadIgnoreKeys`）
2. `Cache/SessionCache.cs:74-85`（`GetReferenceClasses` 的 `*-to-One` 分支）
3. `Cache/SessionCache.cs:129-140`（`GetReferenceClasses` 的 many-to-many 分支）
4. `Session/LoadList.cs:186-197`（`GetItems`）
5. `SQLGenerator/SQLGenerator.cs:196-207 + 258-269`（`GetInsertSQL` / `GenerateStatementByStringProperty`）

**建议**：抽取为一个 `internal static class SqlLiteral { public static string Format(object? value) => ...; }`。这是语法改造中唯一建议做的"结构性抽取"，因为它同时消除 5 份重复和 5 处 culture 缺陷。抽取本身是纯重构（行为等价），风险低、收益高。

### 6.4 `not` 模式与 `is` 声明模式

```csharp
// 现状（SessionFactory/DefaultDbDriverFactory.cs:106-118）
if (dbDataAdapter is DbDataAdapter)
{
    DbCommandBuilder b = this.GetDBCommandBuilder();
    b.DataAdapter = (DbDataAdapter)dbDataAdapter;   // 二次转换
    return b;
}
else { throw new Exception("dbDataAdapter must be DbDataAdapter"); }

// 目标
if (dbDataAdapter is not DbDataAdapter adapter)
{
    throw new ArgumentException("dbDataAdapter must be a DbDataAdapter.", nameof(dbDataAdapter));
}
var builder = GetDBCommandBuilder();
builder.DataAdapter = adapter;
return builder;
```

同类：`ObjectCalculator` 中大量 `(bool)x.Value`、`(string)element.Value`、`(DateTime)property.GetValue(...)` 强制转换 → 声明模式；`Statment/StatementAnalyst` 中约 20 处 `(Operator)element.Value == Operator.Xxx` → 可引入 `StatElement` 的辅助方法 `bool Is(Operator op)` 或用属性模式 `element is { Type: StatElementType.Operator, Value: Operator.Not }`（C# 8 属性模式，可读性显著提升）。

```csharp
// 现状（Statment/StatementAnalyst.cs:428）
if (element.Type == StatElementType.Operator && (Operator)element.Value == Operator.LeftParenthesis)

// 目标（C# 8 属性模式）
if (element is { Type: StatElementType.Operator, Value: Operator.LeftParenthesis })
```

### 6.5 反转嵌套 → 卫语句（early return）

`Configuration/ConfigManager.GetSystemConfiguration`（:29-80）是三层 `if/else`，每个 else 都重复 `reader.Close(); fs.Close(); throw`（共 4 组重复）。配合第 8 阶段的 `using` 声明，重写为：

```csharp
public Configuration GetSystemConfiguration()
{
    using var fs = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read);
    using var reader = new XmlTextReader(fs) { WhitespaceHandling = WhitespaceHandling.None };

    if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
        throw new OrmConfigurationException("Invalid config file: XML declaration expected.");
    if (!reader.Read() || reader.NodeType != XmlNodeType.Element || reader.Name != "Configuration")
        throw new OrmConfigurationException("Invalid config file: <Configuration> root expected.");

    var configuration = new Configuration();
    var result = true;
    while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
    {
        result = reader.Name switch
        {
            "SessionFactory" => ReadSessionFactory(reader, configuration),
            "Mappings"       => ReadMappings(reader, configuration),
            _                => result,
        };
    }
    if (!result) throw new OrmConfigurationException("Invalid config file.");
    return configuration;
}
```

行数从 52 降到 22，且不再有 `Close()` 泄漏路径（原代码在 `ReadSessionFactory` 抛异常时 `fs` 不会关闭）。

同类嵌套反转：`GlobalCache.Search(Guid)`（:78-141，四层嵌套，可反转为 3 个卫语句）、`GlobalCache.ProcessDeletedObject`、`ObjectCalculator.IsTargetObject`（:37-52）、`GlobalCache.ReadObject`。

> **本变更不做**：`GlobalCache.Search(Guid)` / `GlobalCache.ProcessDeletedObject` / `GlobalCache.ReadObject` 的嵌套结构（含 `Monitor` 的不平衡出入口）在提交 #12–#15 中不反转，仅 `ConfigManager.GetSystemConfiguration` 卫语句化。

> ⚠️ `GlobalCache.Search(Guid)` 的嵌套里混着 `Monitor.Enter/Exit` 不平衡调用，**反转嵌套必须逐分支核对 Monitor 出入口**。列入高风险，放到最后做，且必须先补测试（见第 12 节）。

### 6.6 阶段验证

`dotnet test`；对 `StatementAnalyst` / `ObjectCalculator` / `SQLGenerator` 三者必须有第 11 阶段补充的测试作为回归网。

---

## 7. 第 4 阶段：集合、字符串与 API 现代化

### 7.1 集合表达式（C# 12）

```csharp
// 现状（Statment/StatementAnalyst.cs:15）
private readonly char[] _operatorChars = new char[] { '=', '>', '<', '!', '&', '|', 'l', 'i', 'k', 'e', '(', ')' };

// 目标
private static readonly char[] OperatorChars = ['=', '>', '<', '!', '&', '|', 'l', 'i', 'k', 'e', '(', ')'];
```

```csharp
// 现状（SQLGenerator/SQLGenerator.cs:12-18）
private static readonly string[] _sqlOperators = new string[] { "=", "!=", ">", ">=", "<", "<=", " and ", " or ", " like ", "(", ")" };
private static readonly Type[] _basicType = new Type[] { typeof(bool), ... };
private static readonly Type[] _stringType = new Type[] { typeof(char), typeof(string), typeof(DateTime) };

// 目标
private static readonly string[] SqlOperators = ["=", "!=", ">", ">=", "<", "<=", " and ", " or ", " like ", "(", ")"];
private static readonly FrozenSet<Type> BasicTypes = FrozenSet.ToFrozenSet([typeof(bool), ...]);
private static readonly FrozenSet<Type> StringTypes = FrozenSet.ToFrozenSet([typeof(char), typeof(string), typeof(DateTime)]);
```

> **⚠️ `_sqlOperators` 严禁改变元素顺序与个数** —— 它按 `Operator` 枚举值下标索引（`GetSQLOperator` 用 `_sqlOperators[(int)@operator]`）。同理 `StatementAnalyst._priorityTable` 是 10×10 二维表，按枚举下标寻址。二维数组无集合表达式语法，**保持 `new int[,] { ... }` 不变**，仅重新排版为每行一行 + 加注释标明行列对应的 `Operator`（现状是全部挤在一行，605 行文件里最难读的一行）。
>
> 另注：`_priorityTable` 是 10×10，但 `Operator` 枚举有 12 个值（0..11）。`LeftParenthesis`(10) / `RightParenthesis`(11) 会越界 —— 现有代码靠 `ProcessOperatorStack` 提前分流括号规避。改造时**不要**"顺手扩成 12×12"。

### 7.2 线性查找 → 集合查找

```csharp
// 现状（SQLGenerator/SQLGenerator.cs:272-282；GlobalCache.cs:378-388 有一份重复）
private bool IsBasicType(Type type)
{
    for (int i = 0; i < _basicType.Length; i++)
        if (type == _basicType[i]) return true;
    return false;
}

// 目标
private static bool IsBasicType(Type type) => BasicTypes.Contains(type);
```

```csharp
// 现状（Statment/StatementAnalyst.cs:122-125）
private bool IsOperatorChar(char c) => Array.IndexOf<char>(_operatorChars, c) != -1;

// 目标（.NET 8 SearchValues，或最简版）
private static readonly SearchValues<char> OperatorChars = SearchValues.Create("=><!&|like()");
private static bool IsOperatorChar(char c) => OperatorChars.Contains(c);
```

> **注意**：`GlobalCache._basicType` 与 `SQLGenerator._basicType` 内容**不同**（前者含 `string`，后者不含，`string` 被归入 `_stringType`）。这是有意的行为差异，**不要合并这两个集合**，只把各自的线性查找改为 `FrozenSet.Contains`。

### 7.3 `ContainsKey` + 索引器 → `TryGetValue` / `TryAdd`（CA1854）

**约 30 处双查找。** 这是本项目最普遍的性能反模式：

```csharp
// 现状（Cache/GlobalCache.cs:45-47）
if (_objTable.ContainsKey(objType))
{
    foreach (GlobalCacheEntity entity in _objTable[objType]) { ... }
}

// 目标
if (_objTable.TryGetValue(objType, out var entities))
{
    foreach (var entity in entities) { ... }
}
```

```csharp
// 现状（Cache/GlobalCache.cs:241-245，另有 4 处相同模式）
if (!_objTable.ContainsKey(cacheEntity.Value.GetType()))
{
    _objTable.Add(cacheEntity.Value.GetType(), new List<GlobalCacheEntity>());
}
_objTable[cacheEntity.Value.GetType()].Add(cacheEntity);   // 第 3 次哈希查找！

// 目标
var type = cacheEntity.Value.GetType();
if (!_objTable.TryGetValue(type, out var list))
{
    list = [];
    _objTable[type] = list;
}
list.Add(cacheEntity);

// 或 .NET 6+ 的 CollectionsMarshal（单次查找，适合热路径）
ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_objTable, type, out _);
slot ??= [];
slot.Add(cacheEntity);
```

```csharp
// 现状（GlobalCache.cs:51-57 / ReadObject:331-338，两处相同）
foreach (string key in mapping.Keys)
{
    if (!ignorePrimaryKey.ContainsKey(key)) ignorePrimaryKey.Add(key, new List<object>());
    ignorePrimaryKey[key].Add(objType.GetProperty(key).GetValue(entity.Value, null));
}

// 目标
foreach (var key in mapping.Keys)
{
    if (!ignorePrimaryKey.TryGetValue(key, out var values))
    {
        values = [];
        ignorePrimaryKey[key] = values;
    }
    values.Add(objType.GetProperty(key)!.GetValue(entity.Value));
}
```

完整位置清单：`GlobalCache.cs:45,80,82,203,205,241,243,245,444,446,470,472,474` / `SessionCache.cs:32,34,56,64,71,87,89,157,161,170,171,173,206,242,244,246,252,253,255,266,416,418,420` / `PersistenceContext.cs:50,52`。

特例 —— `DefaultDbDriverFactory.SetProviderName`（:37-41）：

```csharp
// 现状：ConcurrentDictionary 却用 ContainsKey + Add（非原子，并发下 Add 抛 ArgumentException）
if (!_registedProviderName.ContainsKey(providerName))
{
    DbProviderFactories.RegisterFactory(providerName, providerName);
    _registedProviderName.Add(providerName, new object());
}

// 目标：字段类型改为 ConcurrentDictionary<string, object> 并用 TryAdd（或直接 HashSet + lock）
private static readonly ConcurrentDictionary<string, byte> RegisteredProviders = new();
...
if (RegisteredProviders.TryAdd(providerName, 0))
{
    DbProviderFactories.RegisterFactory(providerName, providerName);
}
```

> 现状把 `ConcurrentDictionary` 声明为 `IDictionary`，导致拿不到 `TryAdd`，是典型的"接口选择过宽"。改字段类型为具体类型即可。

### 7.4 `string.Format` → 插值字符串（IDE0071）+ culture 显式化

**约 20 处。** 但**这一项不是纯语法替换**，必须同时处理 culture：

```csharp
// 现状（SQLGenerator/SQLGenerator.cs:248）
return string.Format("{0}={1}", property.Name, property.GetValue(@object, null));
```

`string.Format(string, object[])` 使用 **CurrentCulture**。对于 `double`/`decimal` 属性，在德语/法语环境下会生成 `Price=1,5`，产出**语法错误的 SQL**。这是一个既有缺陷（见 13.10），插值字符串默认也是 CurrentCulture，因此直接改插值**不会修好也不会变坏**，但既然在改，就应一次性用不变文化：

```csharp
// 目标
return string.Create(CultureInfo.InvariantCulture, $"{property.Name}={property.GetValue(@object)}");
// 或
return FormattableString.Invariant($"{property.Name}={property.GetValue(@object)}");
```

日期格式化可以更简洁：

```csharp
// 现状（多处）
string.Format("'{0}'", ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"))

// 目标
FormattableString.Invariant($"'{(DateTime)value:yyyy-MM-dd HH:mm:ss}'")
```

位置清单：`SQLGenerator.cs:202,206,248,260,264,268` / `SessionCache.cs:76,80,131,135` / `LoadList.cs:188,192` / `StatementAnalyst.cs:173,217` / `ObjectCalculator.cs:63,113,126,339` / `Program.cs:67,81`（示例中的 `string.Format` 可直接插值，无 culture 顾虑，因为只用于查询条件的 `Guid`/字符串）。

`Program.cs` 中大量 `Console.WriteLine("{0}--{1}--{2}", a, b, c)` → `Console.WriteLine($"{a}--{b}--{c}")`（约 15 处）。

**⚠️ 明确声明**：把 `string.Format` 换成插值字符串**不解决 SQL 注入**。本框架全程字符串拼接 SQL（`SQLGenerator` 全部方法 + `SessionCache.GetReferenceClasses` 的 many-to-many 查询），注入面依旧存在。若要真正解决必须改用 `DbParameter`，这属于架构改动，不在本方案范围内 —— 但**必须在代码注释和 README 中显式标注该限制**，避免"看起来现代化了所以安全了"的误判。

### 7.5 索引/范围运算符（C# 8，IDE0056/IDE0057）

```csharp
// 现状（Cache/ObjectCalculator.cs:242-249）
if (xStr[0] == '\'' && xStr[xStr.Length - 1] == '\'') xStr = xStr.Substring(1, xStr.Length - 2);
if (yStr[0] == '\'' && yStr[yStr.Length - 1] == '\'') yStr = yStr.Substring(1, yStr.Length - 2);

// 目标
if (xStr is ['\'', .., '\'']) xStr = xStr[1..^1];   // C# 11 列表模式，一行表达"首尾都是单引号且长度≥2"
if (yStr is ['\'', .., '\'']) yStr = yStr[1..^1];
```

> 列表模式还顺带修掉一个边界缺陷：现状 `xStr[0]` 对空字符串会抛 `IndexOutOfRangeException`；`xStr is ['\'', .., '\'']` 对空串直接返回 false。这是"语法现代化附带修正"的少数可接受案例，但仍应在提交信息中注明。

同类：`ObjectCalculator.IsString:360`（`value[value.Length - 1]`）、`StatementAnalyst.CreateElement:165`（`_currentElement[_currentElement.Length - 1]`）。

### 7.6 `StringBuilder` 清空

```csharp
// 现状（Statment/StatementAnalyst.cs:59, 179）
_currentElement.Remove(0, _currentElement.Length);

// 目标
_currentElement.Clear();
```

### 7.7 LINQ 替代手写循环（适度）

```csharp
// 现状（Session/Session.cs:107-115）
public T[] Search<T>(string selectCommand)
{
    List<T> result = new List<T>();
    foreach (object item in OnSearch(typeof(T), selectCommand)) result.Add((T)item);
    return result.ToArray();
}

// 目标
public T[] Search<T>(string selectCommand) => OnSearch(typeof(T), selectCommand).Cast<T>().ToArray();
public object[] Search(Type objType, string selectCommand) => OnSearch(objType, selectCommand).ToArray();
```

```csharp
// 现状（SessionFactory/SessionFactory.cs:42-45，SessionFactoryIoc.cs:47-50 有一份重复）
foreach (EntityMapping map in this.Configuration.Mappings) _mappings.Add(map.ClassName, map);

// 目标
_mappings = Configuration.Mappings.ToDictionary(m => m.ClassName);
```

```csharp
// 现状（Cache/SessionCache.cs:403-406）
foreach (CacheEntity cacheEntity in _deleteList) ignoreIds.Add(cacheEntity.ObjectId);

// 目标（同时修 13.2 的 null 缺陷）
ignoreIds ??= [];
foreach (var entity in _deleteList) ignoreIds.Add(entity.ObjectId);
```

**⚠️ 不要 LINQ 化的位置**：`GlobalCache` / `SessionCache` 中所有在锁内、或会修改被遍历集合的循环（`Collect()`、`RollBack()`、`DeleteCacheEntity`）。`GlobalCache.Collect()`（:511-525）在 `foreach (KeyValuePair<...> item in _objList)` 中调用 `_objList.Remove(objectId)` —— 这本身就是**运行时 `InvalidOperationException`**（见 13.5），LINQ 化会掩盖问题。此处应改为"先收集待删 key，再删除"的两段式，而不是 LINQ 一行流。

### 7.8 KeyValuePair 解构

```csharp
// 现状（Cache/GlobalCache.cs:393, 511；SessionCache.cs:61；Ioc/DefaultComponentProvider.cs:37）
foreach (KeyValuePair<string, List<object>> key in ignoreKeys) { ... key.Key ... key.Value ... }

// 目标
foreach (var (columnName, values) in ignoreKeys) { ... }
```

### 7.9 `double.Parse` 异常控制流 → `TryParse`

```csharp
// 现状（Statment/StatementAnalyst.cs:165-175）—— 用异常做流程控制 + 无 culture
if (_currentElement[0] != '\'' || _currentElement[_currentElement.Length - 1] != '\'')
{
    try { double.Parse(_currentElement.ToString()); }
    catch { throw new Exception(string.Format("Invalid Vairable:{0}", _currentElement.ToString())); }
}

// 目标
var text = _currentElement.ToString();
if (text is not ['\'', .., '\''] &&
    !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
{
    throw new QuerySyntaxException($"Invalid constant: {text}");
}
```

### 7.10 阶段验证

`dotnet test`；用 `dotnet run --project ORMFramework.Examples` 对照改造前后生成的 SQL 文本（可临时在 `SQLGenerator` 加日志比对，或用第 11 阶段的 SQL 快照测试）。

---

## 8. 第 5 阶段：资源管理与异常处理现代化

### 8.1 `using` 声明（C# 8，IDE0063）

`Configuration/ConfigManager.GetSystemConfiguration`：8 处手写 `Close()` → 2 个 `using var`（详见 6.5 的完整重写示例）。

`Cache/SessionCache.Commit`（:292-316）：手写 `conn.Close()` 三处 + `tran.Rollback()`：

```csharp
// 目标
public void Commit()
{
    using var conn = PersistenceContext.DbDriverFactory.GetDbConnection();
    conn.Open();
    using var tran = conn.BeginTransaction();
    using var cmd = conn.CreateCommand();
    cmd.Transaction = tran;
    try
    {
        CommitToDB(cmd);
        CommitToGlobalCache();
        ClearAllChangeList();
        tran.Commit();
    }
    catch
    {
        tran.Rollback();
        throw;                      // ← 不再 throw ex（见 8.3）
    }
}
```

`Cache/SessionCache.ProcessQueryCommand`（:275-290）与 `GlobalCache.ReadDataFromDatabase`（:434-483）、`Listener/DefaultCommandListener.OnExecute`：`IDbConnection` / `DataSet` 都实现 `IDisposable` 但从未释放 → 加 `using`。

> ⚠️ `DefaultCommandListener.OnExecute` 与 `PersistenceContext.Connection` 的连接是**共享**的（`PersistenceContext` 缓存 `_connection` 并在 Closed 时重建）。这里加 `using` 会**改变语义**（把共享连接释放掉）。所以：`ProcessQueryCommand` 里自己 `GetDbConnection()` 创建的连接 → 加 `using`；从 `PersistenceContext.Connection` 拿到的 → **不加** `using`，仅对 `DataSet` / `IDbCommand` 加。这一区分必须逐处核对。

### 8.2 终结器 → 标准 Dispose 模式

```csharp
// 现状（Cache/SessionCache.cs:22-25, 434-438）
~SessionCache() { Collect(); }               // ← 终结器里调用 GlobalCache 静态方法（访问托管对象）
public void Dispose() { Collect(); GC.SuppressFinalize(this); }

// 现状（Session/Session.cs:92-95, 191-194）
~Session() { _sessionCache.Dispose(); }      // ← 终结器里 Dispose 另一个托管对象
public void Dispose() { _sessionCache.Dispose(); }   // ← 无 SuppressFinalize，且可重复执行
```

问题：终结器运行在 GC 线程、顺序不确定，此时 `GlobalCache` 的字典可能已被其他终结器动过；`GlobalCache` 内部还有 `Monitor` 操作 —— 在终结器里取锁是明确的死锁风险。

目标：**删除两个终结器**，实现幂等 Dispose（`CA1816`/`CA1063`）：

```csharp
public sealed class SessionCache : ISessionCache
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseGlobalCacheReferences();
    }
}
```

`Session.Dispose` 同样加 `_disposed` 守卫。这属于"语法/模式现代化"与"缺陷修复"的交叉项，建议**独立提交**并在提交信息说明"删除终结器"是行为变更（进程退出时不再自动释放 GlobalCache 引用计数 —— 但原本终结器也不保证运行，因此实际影响为零）。

可选：`ISession` / `ISessionCache` 增加 `IAsyncDisposable`（为第 12 节异步化预留）。**本阶段不做。**

### 8.3 `throw ex;` → `throw;`（CA2200）

**3 处，全部丢失原始堆栈：**

- `Cache/SessionCache.cs:198`（`Update` 的 catch）
- `Cache/SessionCache.cs:228`（`Insert` 的 catch）
- `Cache/SessionCache.cs:314`（`Commit` 的 catch）

另有 `Cache/SessionCache.cs:176-180`（`Delete`）已正确使用 `throw;`，但捕获的 `ex` 变量未使用 → `catch (Exception) { RollBack(); throw; }` 或直接 `catch { RollBack(); throw; }`。

### 8.4 未使用的异常变量（CS0168 / IDE0059）

- `Cache/GlobalCache.cs:66`：`catch (ThreadInterruptedException ex)` → `catch (ThreadInterruptedException)`
- `Cache/GlobalCache.cs:95`：同上
- `Cache/GlobalCache.cs:189`：同上
- `Cache/SessionCache.cs:176,195,225,310`：`catch (Exception ex)` → `catch`

### 8.5 `new Exception(...)` → 具体异常类型（CA2201）

**28 处 `throw new Exception("...")`。** `Exception` 基类无法被调用方有针对性地捕获。建议引入 3 个自定义异常（放在新文件 `ORMFramework/Exceptions/`）：

```csharp
public class OrmException : Exception { ... }                       // 基类
public sealed class QuerySyntaxException : OrmException { ... }     // 查询串解析/求值错误
public sealed class OrmConfigurationException : OrmException { ... } // Config.xml 错误
public sealed class ConcurrencyException : OrmException { ... }     // 锁/版本冲突
```

映射规则：

| 现状 | 位置 | 目标 |
| --- | --- | --- |
| `"QueryString is empty"` | `StatementAnalyst.cs:26,51` | `ArgumentException.ThrowIfNullOrWhiteSpace(value)` (.NET 8) |
| `"Invalid Vairable:{0}"`、`"Invaild Operator:{0}"`、`"Invalid Statement"`、`"' is expected"`、`"( is expected"`、`") is expected"` | `StatementAnalyst.cs`（14 处）、`ObjectCalculator.cs`（8 处）、`SQLGenerator.cs:61` | `QuerySyntaxException` |
| `"Invail Config File"`（注意原文拼写错误） | `ConfigManager.cs:64,71,78` | `OrmConfigurationException` |
| `"You must get a WriteLock before updating/deleting object"`、`"object has been deleted"` | `GlobalCache.cs:150,197,218` | `ConcurrencyException`（或 `InvalidOperationException`） |
| `"The object exists"`、`"The object do not be referenced"` | `SessionCache.cs:208`、`GlobalCache.cs:490` | `InvalidOperationException` |
| `"ConnectionString can not be null or empty"` | `DefaultDbDriverFactory.cs:49` | `InvalidOperationException` |
| `"dbDataAdapter must be DbDataAdapter"` | `DefaultDbDriverFactory.cs:116` | `ArgumentException(..., nameof(dbDataAdapter))` |
| `"Type of oldOject must be the same as that of newObject"`、`"Invalid property type"` | `SQLGenerator.cs:73,245,254` | `ArgumentException(..., nameof(...))` |
| `"element.Type must be StatElement.Operator"` | `StatementAnalyst.cs:535` | `ArgumentException(..., nameof(element))` |
| `"The method or operation is not implemented."` ×9 | `Test/StatementAnalystTest.cs`（死类 `List2`） | 整类删除（见 11.3） |

同时用 .NET 6+ 的 throw helper 简化参数校验：

```csharp
ArgumentNullException.ThrowIfNull(persistenceContext);              // .NET 6
ArgumentException.ThrowIfNullOrWhiteSpace(queryString);             // .NET 8
ObjectDisposedException.ThrowIf(_disposed, this);                   // .NET 7
```

### 8.6 阶段验证

`dotnet test`；新增针对异常类型的断言（`Assert.ThrowsException<QuerySyntaxException>`），替换掉原来只能断言 `Exception` 的写法。

---

## 9. 第 6 阶段：反射与 `nameof` 强类型化

这一阶段收益/风险比最高：把 6 处**字符串硬编码反射**改成编译期可验证的形式。

### 9.1 `Assembly.Load` → `typeof(LoadList<>)`

```csharp
// 现状（Cache/SessionCache.cs:109-116, 142）
referenceType = (referenceType.GetGenericArguments())[0];
Type loadListType = Assembly.Load("ORMFramework").GetType("ORMFramework.LoadList`1").MakeGenericType(referenceType);
object loadList = Activator.CreateInstance(loadListType);
List<object> foreignKeyValues = (List<object>)loadListType.GetProperty("ForeignKeyValues").GetValue(loadList, null);
loadListType.GetProperty("SessionCache").SetValue(loadList, this, null);
loadListType.GetProperty("ForeignKeyName").SetValue(loadList, relation.ReferenceColum, null);

// 目标
var itemType = referenceType.GetGenericArguments()[0];
var loadListType = typeof(LoadList<>).MakeGenericType(itemType);   // ← 同程序集，无需 Assembly.Load
var loadList = Activator.CreateInstance(loadListType)!;
var foreignKeyValues = (List<object>)loadListType
    .GetProperty(nameof(LoadList<object>.ForeignKeyValues))!.GetValue(loadList)!;
loadListType.GetProperty(nameof(LoadList<object>.SessionCache))!.SetValue(loadList, this);
loadListType.GetProperty(nameof(LoadList<object>.ForeignKeyName))!.SetValue(loadList, relation.ReferenceColum);
```

收益：
1. 移除对**程序集名 `"ORMFramework"` 的硬依赖**（`AGENTS.md` 明确记录"assembly 必须保持叫 ORMFramework"这一隐式约束 —— 改造后该约束消失）。
2. 移除对**类型全名字符串 `"ORMFramework.LoadList\`1"`** 的依赖（含反引号，极易写错）。
3. 三个属性名改用 `nameof`，改名时编译器报错而不是运行时 `NullReferenceException`。

> `nameof(LoadList<object>.ForeignKeyValues)` 中的 `object` 只是占位（`nameof` 不求值），任何类型参数都行。

**更彻底的可选做法**（去掉反射设值）：给 `LoadList<T>` 加一个 `internal` 接口 `ILoadList { string ForeignKeyName { set; } List<object> ForeignKeyValues { get; } ISessionCache SessionCache { set; } }`，然后 `((ILoadList)loadList).ForeignKeyName = ...`。只剩 `Activator.CreateInstance` 一处反射。这属于小幅结构改动，收益明显，建议纳入但标为"可选加分项"。

### 9.2 `PropertyInfo` 调用简化

`GetValue(obj, null)` / `SetValue(obj, value, null)` 的 `null` 索引参数在 .NET 4.5 起有单参重载（`GetValue(obj)` / `SetValue(obj, value)`）。**全项目约 25 处**可简化：`GlobalCache.cs:57,337,371,372,460` / `SessionCache.cs:94,103,111,112,116,142,144` / `SQLGenerator.cs:181,194,248,257,302` / `ObjectCalculator.cs:109,177,185,215,219,231`。

### 9.3 反射结果缓存（可选，性能）

`SQLGenerator.GetInsertSQL/GetUpdateSQL/GetDeleteSQL` 与 `GlobalCache.CloneObject/ReadDataFromDatabase` 每次调用都 `GetType().GetProperties()`。可用 `ConcurrentDictionary<Type, PropertyInfo[]>` 缓存。**这是性能优化而非语法现代化**，列为可选，不在核心范围。

### 9.4 阶段验证

必须运行 `dotnet run --project ORMFramework.Examples` 的 `TestOneToMany` / `TestManyToMany`（当前被注释掉），因为 9.1 的代码路径**没有任何单元测试覆盖**。这也是第 11 阶段要补测试的重点。

---

## 10. 第 7 阶段：可空引用类型（NRT）启用

### 10.1 策略

把 `Directory.Build.props` 的 `<Nullable>annotations</Nullable>` 切为 `enable`，逐文件消警。文件数多但每处改动微小，建议**按目录分批提交**（Statment → Configuration → Ioc → SQLGenerator → Listener/Event → Session → Cache）。

### 10.2 必须显式标注可空的公开签名

| 位置 | 现状 | 目标 |
| --- | --- | --- |
| `IPersistenceContext.GetEntityMappingByClassName` | 返回 `EntityMapping`，实现中会 `return null`（`PersistenceContext.cs:56`） | `EntityMapping?` |
| `ISQLGenerator.GetSelectSQL(Type, QueryExpression)` | `queryExpression` 实际允许 null（`SQLGenerator.cs:26` 传 null） | `QueryExpression?` |
| `ISessionCache.Search(Type, QueryExpression)` | `expression` 可为 null（`GlobalCache.Search` 有 null 分支） | `QueryExpression?` |
| `StatementAnalyst.CreateElement` | 会 `return null`（:184） | `StatElement?` |
| `SQLGenerator.GetObjectPropertyValue` | 会 `return null`（:300） | `object?`（且该方法未被使用，见 13.13） |
| `ISession.SessionFactory` | `SessionFactoryIoc`/`Session(persistenceContext)` 路径下可为 null | `ISessionFactory?` |
| `LoadList<T>._items` | 懒加载，初值 null | `List<T>?` + `Items` 属性（见 5.5） |
| `CacheEntity.ForeignKeys` / `Value` | 可能未赋值（`SessionCache.Insert` 不设 `ForeignKeys`） | `Dictionary<string, object>?` 或初始化为 `[]` |
| `EntityRelation` 全部 string 属性 | 由 XML 填充，缺失属性会 NRE | `required string`（C# 11）或保留可空 |
| `Configuration.ConnectionString` / `ProviderName` | 同上 | `required` 或 `string?` |
| `GlobalCache.Search(Guid)` | 多个 `return null` 分支 | `GlobalCacheResult?` |

`EntityRelation` / `Configuration` 用 `required` 属性（C# 11）是最优雅的表达，但 `ConfigManager` 用的是"先 `new`，再逐个赋值"的写法，`required` 会要求改成对象初始化器。这与 6.1 的 switch 表达式改造正好可以合并做。

### 10.3 反射返回值的 null 处理

`GetType().GetProperty(name)` 返回 `PropertyInfo?`。全项目约 20 处直接 `.GetValue(...)`，NRT 打开后全部报警。处理原则：

- 已有显式 null 检查的（`ObjectCalculator.GetObjectProperty`、`ProcessVariable`）→ 保持，加 `!` 或改用模式匹配。
- 无检查的（`GlobalCache.cs:57,337`、`SessionCache.cs:66,94,103,111,112,116,142,144`）→ **不要**简单加 `!` 消警，应改为 `?? throw new OrmConfigurationException($"Mapping references unknown property '{name}' on {type}.")`。这会把"配置写错时抛 NRE"改善为"抛可读异常"，属于合理的附带改进。

### 10.4 阶段验证

`dotnet build -warnaserror:CS8600,CS8601,CS8602,CS8603,CS8604,CS8618,CS8625` 通过。

---

## 11. 第 8 阶段：测试工程现代化

### 11.1 删除 VS2005 样板

`ConfigManagerTest.cs`、`ObjectCalculatorTest.cs`、`SQLGeneratorTest.cs`、`StatementAnalystTest.cs` 四个文件各有：

- 文件头注释 `// 以下代码由 Microsoft Visual Studio 2005 生成。`
- `private TestContext testContextInstance;` + 15 行手写 `TestContext` 属性 —— MSTest 3.x 只需 `public TestContext TestContext { get; set; }`，而且这四个类**根本没用到** `TestContext` → 整体删除。
- `#region 附加测试属性` 内 33 行全注释的 `ClassInitialize`/`ClassCleanup`/`TestInitialize`/`TestCleanup` 模板 → 删除。
- `[TestClass()]` / `[TestMethod()]` 的空括号 → `[TestClass]` / `[TestMethod]`（与 `ComponentProviderTest.cs` 保持一致）。

仅这一项就删除约 200 行噪声（测试项目现有 5 个文件共 ~19KB，其中大半是样板与注释）。

### 11.2 缩进风格统一

`ComponentProviderTest.cs` 用 **Tab** 缩进，其余文件用 4 空格。`.editorconfig` 统一为空格后由 `dotnet format` 修正。

### 11.3 删除死代码

- `StatementAnalystTest.cs:142-200`：`public class List2 : ICollection<int>` —— 一个全部方法 `throw new Exception("The method or operation is not implemented.")` 的类，与任何测试无关，纯实验残留 → **删除整个类**。
- `StatementAnalystTest.cs:77-138`：两块被注释的测试（`AnalysisTest`、`test`）→ 见 11.4，要么恢复要么删除，不要留注释。
- `ObjectCalculatorTest.cs:74-86`、`SQLGeneratorTest.cs:76-…`：被注释的测试引用了**已不存在的 API**（`new Student(1)`、`Student.Class`、`SQLGenerator(pesistenceContext)` 构造函数、`IsTargetObject(object, List<StatElement>)` 旧签名）→ 按新 API 重写（见 11.4），不要保留注释。
- `SQLGeneratorTest.cs:73-74`：`private IPersistenceContext pesistenceContext; object @object;` 未使用字段。

### 11.4 补充回归测试（改造安全网，**必须先于第 3/4/6 阶段完成**）

改造涉及大量语义敏感的表达式改写。以下测试必须先建立：

| 测试目标 | 覆盖内容 | 保护的改造项 |
| --- | --- | --- |
| `StatementAnalystTest` | 中缀/后缀序列快照：`"Id==1"`、`"a==1 && b like 'x%'"`、`"! ( b== 3 )"`、`"Student like '%' && a=='2' || ! ( b== 3 )"`；异常用例：空串、括号不匹配、非法运算符、未闭合引号 | 6.1 switch 表达式、6.2 布尔折叠、6.4 属性模式、7.1 集合表达式、7.9 TryParse |
| `SQLGeneratorTest` | `GetSelectSQL` / `GetInsertSQL` / `GetUpdateSQL` / `GetDeleteSQL` 的**字符串快照**（用假 `IPersistenceContext` + 内存 mapping，无需数据库） | 7.4 插值字符串、6.3 字面量抽取、7.2 类型集合 |
| `ObjectCalculatorTest` | `IsTargetObject` 对各运算符 × 各类型（int/string/DateTime/bool/char）的真值表；`like` 通配符 `%`/`_`/`[...]` | 6.1、6.2、7.5 列表模式 |
| `ConfigManagerTest` | 用**嵌入资源**里的 Config.xml（替换硬编码 Windows 路径），断言 mapping 数量、Keys、四种 Relation 类型与全部列名 | 6.5 using+卫语句重写、10.2 required 属性 |
| `SqlLiteralTest`（新增） | 6.3 抽取的字面量格式化：null / string / char / DateTime / int / double / bool，并在 `de-DE` culture 下断言不变文化输出 | 6.3、7.4 |
| `LoadListTest`（新增） | 假 `ISessionCache`，断言懒加载只触发一次、生成的谓词串为 `fk==v1 || fk==v2` | 5.5、9.1 |
| `SessionCacheTest`（新增，可选） | 假 `IPersistenceContext`，覆盖 Insert/Update/Delete 的 change list 状态迁移 | 4.4、7.3、13.1 |

`ConfigManagerTest` 的硬编码路径修正（现状 `"E:\\毕业设计\\ORMFramework\\..."`，在任何非该机器环境必然失败）：

```csharp
// 目标：Config.xml 作为 EmbeddedResource 或 CopyToOutputDirectory
var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Config.xml");
var target = new ConfigManager(path);
var actual = target.GetSystemConfiguration();
Assert.AreEqual(5, actual.Mappings.Count);
```

### 11.5 断言现代化

`ComponentProviderTest.cs:16`：`Assert.AreEqual<Type>(typeof(Session), session.GetType())` → `Assert.IsInstanceOfType<Session>(session)`（MSTest 3.x 泛型重载）。

### 11.6 阶段验证

`dotnet test` 全绿（包括修好的 `ConfigManagerTest`）；`dotnet test --collect:"XPlat Code Coverage"` 中 `StatementAnalyst` / `SQLGenerator` / `ObjectCalculator` 三者行覆盖率 ≥ 70%。

---

## 12. 可选/高风险：并发模型与异步化

> **本节列为"方案外的后续提案"，不建议与语法改造同批进行。**

> **变更记录（提交 #26「其余既有缺陷与文档收尾」）**：本节 12.1–12.3 的三项均为**本变更之外的后续独立提案**，未在本变更实施。

### 12.1 `GlobalCache` 手写锁

`Cache/GlobalCache.cs` 实现了一套自制读写锁，其特征：

- `Monitor.Enter` 与 `Monitor.Exit` **跨方法不平衡**：`ProcessDeletedObject`（:281）进入 monitor，由调用方（`Search`、`GetWriteLockForObject`、`ReadObject`）退出。
- 用 `Thread.Sleep(Timeout.Infinite)` 阻塞 + `Thread.Interrupt()` 唤醒，`ThreadInterruptedException` 被当作正常控制流（:66-69, 95-98, 189-192 三处 catch 后递归重试）。
- `Monitor.Pulse` / `PulseAll` 与上述 Sleep/Interrupt 机制**混用**，实际上 `Pulse` 在这里不起唤醒作用（等待方不是 `Monitor.Wait`）。
- 递归重试（`return Search(objectId)`、`GetWriteLockForObject(objectId)`）在高竞争下有栈溢出风险。

**现代化方向**（需独立设计 + 压力测试）：
- 用 `ReaderWriterLockSlim`（每实体一个，或分段）替换 `Lock` 枚举 + `ReadCount` + `WriteLockThreadId` + `Queue<Thread>`。
- 用 `SemaphoreSlim` + `CancellationToken` 替换 `Thread.Sleep(Infinite)` + `Interrupt`。
- `_objTable` / `_objList` 改为 `ConcurrentDictionary`（目前是普通 `Dictionary` 且**大部分访问路径没有任何锁保护** —— 这是真实的数据竞争）。

**前置条件**：必须先有多线程回归测试。当前唯一的并发验证是 `Program.TestMultitransaction`（且已被注释掉），完全没有自动化覆盖。**在补齐并发测试之前，禁止改动此文件的锁逻辑**；本方案第 1~7 阶段对该文件只做"绝对安全"的变换（using 清理、file-scoped namespace、`var`、`TryGetValue`、插值字符串、`catch (X)` 去变量），**不动任何 `Monitor` / `Thread` 语句、不反转含 Monitor 的嵌套**。

### 12.2 ADO.NET 现代化 / 异步化

- `IDbConnection` / `IDbCommand` / `IDbDataAdapter`（.NET 1.1 非泛型接口）→ `DbConnection` / `DbCommand` / `DbDataReader`。
- `DataSet` / `DataTable` / `Fill()`（`GlobalCache.ReadDataFromDatabase`、`SessionCache.ProcessQueryCommand`、`DefaultCommandListener`、`ISession.ExecuteQuery`）→ `DbDataReader` 流式读取。
- 全链路 `async`：`ISession.SearchAsync` / `SubmitAsync`，`ISessionCache.CommitAsync`，Listener 接口返回 `ValueTask`。

这会**改变公开 API**（`ISession.ExecuteQuery` 返回 `DataSet`），是 v2 级别的改动。不在本方案范围。

### 12.3 日志

`LogHelper` 是静态类 + 静态 `ILog _log`，且：
- `_log` 在 `InitLog` 前为 null → 任何 `LogHelper.Debug(...)` 都会 NRE（**目前全项目没有一处调用 LogHelper 的日志方法**，所以没暴露）。
- `LogManager.CreateRepository("NETCoreRepository")` 用固定名字，重复调用抛异常（`SessionFactory.Initialize` 与 `SessionFactoryIoc.Initialize` 都会调）。

现代化方向：改用 `Microsoft.Extensions.Logging` + `ILogger<T>` 注入（项目已引用 `Microsoft.Extensions.DependencyInjection`，天然契合）。同时 `Config.xml` 里的 `log4net` 段可保留（向后兼容）或迁移到 `appsettings.json`。**可选，独立提案。**

---

## 13. 改造中必然暴露的既有缺陷清单

> 这些缺陷**不属于语法改造**，但语法改造过程中会撞上它们（编译失败或分析器告警）。每一项应作为**独立提交/独立 PR**，与语法提交分离。标 🔴 的会阻塞语法改造，必须先修。

| # | 位置 | 问题 | 阻塞项 |
| --- | --- | --- | --- |
| 13.1 🔴 | `Cache/SessionCache.cs:428` | `foreach (Guid objectId in _objList.Keys)` 但 `_objList` 是 `Dictionary<object, CacheEntity>` → 运行时 `InvalidCastException`。应遍历 `_objList.Values.Select(e => e.ObjectId)` | 4.4 `var` 化会编译失败 |
| 13.2 🔴 | `Cache/SessionCache.cs:399-406` | `GetObjectsFromGlobalCache(type, expr, null)`（:102 调用）后立即 `ignoreIds.Add(...)` → `_deleteList` 非空时 NRE | 10.x NRT 会报警 |
| 13.3 | `Cache/GlobalCache.cs:62-69` | `catch (ThreadInterruptedException)` 后重试 `ReadObject`，但首次调用已 `Monitor.Exit` → 重试时 `Enter` 状态不一致 | 12.1 |
| 13.4 | `Cache/GlobalCache.cs:143-161` `Update` | 修改 `cacheEntity` 全部字段但**未取 monitor**（只检查 `WriteLockThreadId`）；`_objList[objectId]` 键不存在时抛 `KeyNotFoundException` | 7.3 |
| 13.5 | `Cache/GlobalCache.cs:511-525` `Collect` | `foreach (... in _objList)` 内部 `_objList.Remove(objectId)` → `InvalidOperationException: Collection was modified` | 7.7 |
| 13.6 | `SQLGenerator/SQLGenerator.cs:44-66` | `queryExpression == null` 时不会 `Append(" where (")`，但 :65 无条件 `Append(")")` → 生成 `select * from t)`（非法 SQL）。当前靠 `GlobalCache.ReadIgnoreKeys` 在其后追加 `where` 掩盖，实际仍产出 `select * from t) where ...` | 7.x SQL 快照测试会立刻暴露 |
| 13.7 | `SQLGenerator/SQLGenerator.cs:159-162, 222-226` | 用 `i < properties.Length - 1` 决定是否加分隔符。若**最后一个属性被 `continue` 跳过**（导航属性），会留下尾随 `" and "` / `","` → 非法 SQL。应改用"先收集片段再 `string.Join`" | 7.7 |
| 13.8 | `SQLGenerator/SQLGenerator.cs:258-261` | 值为 null 时生成 `Name='NULL'`（字符串 "NULL"）而非 `Name is NULL` | 6.3 |
| 13.9 | `Cache/ObjectCalculator.cs:226-241` | `y` 是 Variable 且非 DateTime 时，`yStr = y.Value.ToString()`（取的是**属性名**）而非 `property.GetValue(...)`。对照 `x` 分支（:219 用 `property.GetValue`）可确认是复制粘贴缺陷 → 变量之间的字符串比较永远错 | 6.1 真值表测试会暴露 |
| 13.10 | 全项目 SQL 生成 | `string.Format` / `double.Parse` 均无 culture → 非英语区生成 `Price=1,5` | 7.4 / 7.9 |
| 13.11 | `Statment/StatementAnalyst.cs:227-230` | `while (currentChar == ' ') currentChar = _queryString[++_stringIndex];` 查询串以空格结尾时 `IndexOutOfRangeException` | 6.x 测试会暴露 |
| 13.12 | `Statment/StatementAnalyst.cs:24, 33-40` | ① `value.Trim()` 对 null NRE，应 `IsNullOrWhiteSpace(value)`；② 构造函数里 `QueryString = queryString`（setter 已调 `Init()`）后又调一次 `Init()` → **词法/语法分析跑两遍** | 4.x / 8.5 |
| 13.13 | 死代码 | `Cache/ObjectCache.cs`（抽象类，零派生）；`Configuration/EntityClass/DatabaseType.cs`（枚举，零引用）；`SQLGenerator.GetObjectPropertyValue`（:296，零调用）；`Test/StatementAnalystTest.List2`；`SQLGenerator`/`GetUpdateSQL`/`GetDeleteSQL`/`GetInsertSQL` 中三大块被注释的导航属性逻辑（:94-111, 144-153, 208-216） | 5.7 / 11.3 |
| 13.14 | `SessionFactory/DefaultDbDriverFactory.cs:37-41` | `ConcurrentDictionary` 用 `ContainsKey`+`Add` 非原子 → 并发下 `Add` 抛 | 7.3 |
| 13.15 | `LogHelper/LogHelper.cs:21` | `CreateRepository("NETCoreRepository")` 固定名，`SessionFactory` 与 `SessionFactoryIoc` 都调 `InitLog` → 第二次抛 `LogException` | 12.3 |
| 13.16 | `Test/ConfigManagerTest.cs:77` | 硬编码 `E:\毕业设计\...` Windows 路径 → 非该机器必然失败（`AGENTS.md` 已记录为"预期失败"） | 11.4 |
| 13.17 | `Ioc/DefaultComponentProvider.cs:26` vs `ComponentProviderBuilder.cs` | `ISubmitListener` 在生命周期表中注册，但**不在实现表中** → 从未被 DI 注册。反之，实现表中任何未登记生命周期的键会在构造时抛 `KeyNotFoundException`。应改为 `TryGetValue` + 默认 Transient，或合并成单一表 | 7.3 |
| 13.18 | `Ioc/ComponentProviderBuilder.cs:29` | `SetSession<T>()` 写的是 `componentImplTypes[typeof(Session)] = typeof(T)`（键为**实现类** `Session`），应为 `typeof(ISession)` → 该 API 完全无效 | — |
| 13.19 | `Examples/Program.cs:15` | 硬编码绝对路径 `/Users/liuqiming/...`；且 `Config.xml` 未配置 `CopyToOutputDirectory` | 3.1 |
| 13.20 | `Session/Session.cs:92-95` / `Cache/SessionCache.cs:22-25` | 终结器中访问托管对象 + 取锁 | 8.2 |

---

## 14. 红线：禁止改动的部分

以下项目**看起来像应该被"现代化"，但实际是承载语义的**，改动会破坏功能：

### 14.1 拼写错误（load-bearing）

| 标识符 | 位置 | 为何不能改 |
| --- | --- | --- |
| `KeyColum`、`ReferenceColum`、`ReferenceClassColum`、`ReferenceClassKeyColum` | `EntityRelation`、`ManyToManyRelation` | `ConfigManager` 用 `node.Attributes["KeyColum"]` **字面匹配 XML 属性名**。改 C# 属性名的同时必须改 `Config.xml` 和 `ConfigManager`，会破坏所有既有配置文件 |
| `Statment`（命名空间与目录名） | `ORMFramework/Statment/` | 改名需同步 6 个文件的 using + 测试项目 3 处。可做，但收益低、diff 大 → **建议不改** |
| `NifixExpression`（应为 Infix） | `QueryExpression` | 公开 API，`SQLGenerator.cs:38` 使用。若要改，用 `[Obsolete]` 保留旧名转发 |
| `RelationType.MantToMany` | `RelationType` 枚举 | 公开枚举成员。`ConfigManager.cs:220`、`SessionCache` 隐式依赖。若要改，加 `MantToMany = ManyToMany` 别名 |
| `ISessionCache.GetReferenceClasses(object soruceObject)` | 参数名 | 参数名是公开 API 的一部分（命名参数调用）。可安全改（项目内无命名参数调用），但需一并改实现 |
| `GlobalCache.ReleaseReferenct` | 方法名 | 公开静态方法，`SessionCache.Collect:430` 调用。可改，属于破坏性变更 → 建议加 `[Obsolete]` 转发 |

**策略**：私有/internal 拼写错误（`componentLiftimeDict`、`pesistenceContext`、`foreingKeyValue`、`pattenRegex`、`ReadNotOpeartion`、`_epxression`、`ContartMenu.cs` 文件名 vs `ContactMenu` 类名）→ **可改，编译期安全**。公开 API 拼写错误 → **本次不改**，另开 breaking-change 提案。

### 14.2 索引对齐的常量表

- `Statment/Operator.cs` 枚举值 **0..11 显式赋值** —— `SQLGenerator._sqlOperators`（11 元素，按 `(int)Operator` 索引）和 `StatementAnalyst._priorityTable`（10×10，按 `(int)Operator` 双索引）都靠这个顺序。**禁止重排、禁止插入新成员**。
- `SQLGenerator._sqlOperators` 与 `GlobalCache._basicType` / `SQLGenerator._basicType` / `_stringType` 的**元素内容和顺序**。
- `_priorityTable` 保持 `int[,]`，只重排版不改数值/维度。

### 14.3 反射依赖的契约

- 实体类的导航集合属性必须声明为 `IList<T>`（`SessionCache.cs:108` 取 `GetGenericArguments()[0]`）→ **不要**"现代化"成 `List<T>`、`IReadOnlyList<T>` 或 `ICollection<T>`。
- 实体类必须有 **public 无参构造函数**（`Activator.CreateInstance`）→ 加主构造函数或 `required` 属性时要小心不要消除无参构造。
- 实体属性必须是 **public 可读写**（`CloneObject`、SQL 生成、`ReadDataFromDatabase` 反射读写）→ **不要**改成 `{ get; init; }` 或 `readonly record struct`。
- `LoadList<T>` 的命名空间 + 类名 + 三个属性名（`ForeignKeyName`、`ForeignKeyValues`、`SessionCache`）在完成 9.1 改造**之前**不能改；完成后可以改（因为已用 `nameof`）。

### 14.4 `Config.xml` 格式

`ConfigManager` 是手写 `XmlTextReader` 状态机，对**元素顺序敏感**（`SessionFactory` 必须在 `Mappings` 之前）、对**未知元素直接报错**。禁止在语法改造中"顺手"改成 `XDocument` / `XmlSerializer` / `System.Text.Json`（那是独立的架构提案，且必须保证既有 XML 100% 兼容）。

### 14.5 命名空间与目录不一致

`SQLGenerator/` 目录下是 `namespace ORMFramework.SQL`；`Session/`、`SessionFactory/`、`Cache/` 部分类型在 `namespace ORMFramework`。改成"目录=命名空间"是破坏性变更（影响所有 using）。**本次不改**，只在文件内做 file-scoped 转换。

---

## 15. 逐文件改造清单

图例：**M** 机械（4 章，dotnet format 可自动）｜**D** 声明（5 章）｜**C** 控制流（6 章）｜**A** API/集合/字符串（7 章）｜**R** 资源/异常（8 章）｜**F** 反射（9 章）｜**N** NRT（10 章）｜**T** 测试（11 章）｜⚠ 高风险

### ORMFramework（46 文件）

| 文件 | 行数 | 改造项 | 风险 |
| --- | --- | --- | --- |
| `Cache/CacheEntity.cs` | 17 | M | 低 |
| `Cache/GlobalCache.cs` | 541 | M, A(7.1/7.2/7.3/7.4), R(8.4), F(9.2), N | ⚠ **不动 Monitor/Thread 语句、不反转含 Monitor 的嵌套** |
| `Cache/GlobalCacheEntity.cs` | 56 | M, D(5.1 `BlockThreads`) | 低（`Lock` 枚举名与 `lock` 关键字冲突已用 `Lock.` 限定，保持） |
| `Cache/GlobalCacheResult.cs` | 25 | M, D(5.4 主构造) | 低 |
| `Cache/ISessionCache.cs` | 17 | M, N(10.2) | 低 |
| `Cache/ObjectCache.cs` | 22 | **删除（死代码 13.13）** | 低 |
| `Cache/ObjectCalculator.cs` | 377 | M, C(6.1/6.2/6.3/6.4), A(7.5), R(8.5), F(9.2), N | 中（需 6.x 真值表测试；含缺陷 13.9） |
| `Cache/SessionCache.cs` | 440 | M, C(6.3), A(7.3/7.4/7.7), R(8.1/8.2/8.3/8.4), F(9.1/9.2), N | ⚠ 含 🔴13.1、🔴13.2 |
| `Configuration/ConfigManager.cs` | 244 | M, D(5.4), C(6.1/6.5), R(8.1/8.5), N | 中（配置解析，需 11.4 测试） |
| `Configuration/EntityClass/Configuration.cs` | 23 | M, D(5.1), N(10.2 required) | 低 |
| `Configuration/EntityClass/DatabaseType.cs` | 14 | **删除（死代码 13.13）** | 低 |
| `Configuration/EntityClass/EntityMapping.cs` | 42 | M, D(5.1/5.2) | 低 |
| `Configuration/EntityClass/EntityRelation.cs` | 19 | M, N(10.2 required) | 低（属性名是红线 14.1） |
| `Configuration/EntityClass/ManyToManyRelation.cs` | 15 | M, N | 低（同上） |
| `Configuration/EntityClass/RelationType.cs` | 14 | M | 低（`MantToMany` 是红线） |
| `Event/ActionEvent.cs` | 14 | M | 低（不 record） |
| `Event/CommandEvent.cs` | 19 | M | 低 |
| `Event/SelectEvent.cs` | 15 | M | 低 |
| `Event/UpdateEvent.cs` | 11 | M | 低 |
| `Ioc/ComponentProviderBuilder.cs` | 45 | M(4.6), D(5.3), A(7.1) | 低（含缺陷 13.18） |
| `Ioc/DefaultComponentProvider.cs` | 63 | M(4.6), D(5.3/5.4), C(6.1), A(7.3/7.8) | 低（含缺陷 13.17） |
| `Ioc/IComponentProvider.cs` | 11 | M | 低 |
| `Listener/DefaultCommandListener.cs` | 40 | M, R(8.1 谨慎), N | 中（连接所有权，见 8.1 注） |
| `Listener/DefaultDeleteListener.cs` | 16 | M, D(5.3) | 低 |
| `Listener/DefaultInsertListener.cs` | 16 | M, D(5.3) | 低 |
| `Listener/DefaultSelectListener.cs` | 18 | M, D(5.3) | 低 |
| `Listener/DefaultSubmitListener.cs` | 16 | M, D(5.3) | 低 |
| `Listener/DefaultUpdateListener.cs` | 17 | M, D(5.3) | 低 |
| `Listener/IListeners.cs` | 37 | M | 低（6 接口同文件，保持） |
| `LogHelper/LogHelper.cs` | 51 | M, D(5.7 static class), A(Path.Combine), N | 低（缺陷 13.15 另议） |
| `SQLGenerator/ISQLGenerator.cs` | 17 | M, N(10.2) | 低 |
| `SQLGenerator/SQLGenerator.cs` | 305 | M, D(5.3), C(6.3), A(7.1/7.2/7.4), R(8.5), F(9.2/9.3), N | 中（需 SQL 快照测试；含 13.6/13.7/13.8/13.13） |
| `Session/IPersistenceContext.cs` | 18 | M, N(10.2) | 低 |
| `Session/ISession.cs` | 28 | M, N(10.2) | 低 |
| `Session/LoadList.cs` | 215 | M, D(5.1/5.3/5.5), C(6.3), A(7.4), N | 中（属性名在 9.1 前是红线） |
| `Session/PersistenceContext.cs` | 60 | M(this. 统一), D(5.1/5.3), A(7.3), N(10.2) | 低（两个构造函数与 DI 解析强相关，见 `AGENTS.md`，**不要删任一重载**） |
| `Session/Session.cs` | 196 | M, D(5.1/5.3/5.6 sealed), A(7.7), R(8.2), N | 中（终结器删除是行为变更） |
| `SessionFactory/DefaultDbDriverFactory.cs` | 121 | M(4.2 删 `System.Configuration.Provider`/this. 统一), D(5.3), C(6.4), A(7.3), R(8.5), N | 低（含缺陷 13.14） |
| `SessionFactory/IDbDriverFactory.cs` | 25 | M | 低 |
| `SessionFactory/ISessionFactory.cs` | 12 | M(Tab→空格) | 低 |
| `SessionFactory/SessionFactory.cs` | 62 | M, A(7.7), N | 低（与 `SessionFactoryIoc` 重复逻辑可抽基类 —— 可选） |
| `SessionFactory/SessionFactoryIoc.cs` | 73 | M(this. 统一), D(5.3), A(7.7), N | 低 |
| `Statment/Operator.cs` | 22 | M **仅此** | 低（显式枚举值是红线 14.2） |
| `Statment/QueryExpression.cs` | 35 | M, D(5.1/5.6 record) | 低 |
| `Statment/StatElement.cs` | 32 | M, D(5.1) | 低（**不可** record，见 5.6） |
| `Statment/StatementAnalyst.cs` | 605 | M, D(5.3), C(6.1/6.2/6.4), A(7.1/7.5/7.6/7.9), R(8.5), N | ⚠ 词法/语法核心，必须先有 11.4 测试；`_priorityTable` 是红线 |

### ORMFramework.Examples（6 文件）

| 文件 | 改造项 | 风险 |
| --- | --- | --- |
| `Program.cs` | M, A(7.4 插值 ×17), 13.19 路径修正；可选 top-level statements（**但 csproj 有 `<StartupObject>`，改 top-level 需同步删除该属性 → 建议保留 `Main`**） | 低 |
| `EntityClasses/Student.cs` | M；`Equals`/`GetHashCode` 可用 `obj is Student s && ...` 模式简化 | 低 |
| `EntityClasses/Course.cs` | M, D(5.1 全部 4 属性), C(6.4 Equals) | 低（`IList<Student>` 是红线 14.3） |
| `EntityClasses/Teacher.cs` | M, D(5.1 全部 4 属性), C(6.4) | 低 |
| `EntityClasses/Department.cs` | M, D(5.1 全部 3 属性), C(6.4) | 低（`IList<Teacher>` 红线） |
| `EntityClasses/ContartMenu.cs` | M, D(5.1 全部 5 属性), C(6.4)；**文件名 `ContartMenu.cs` vs 类名 `ContactMenu` 不一致 → 重命名文件（安全）** | 低 |

> 实体类的 `Equals` 现代化示例：
> ```csharp
> public override bool Equals(object? obj) => obj is Course other && Id == other.Id;
> public override int GetHashCode() => Id.GetHashCode();
> ```
> 注意 5 个实体类的 `Equals` 里局部变量都叫 `student`（复制粘贴），改造时统一为 `other`。
> **⚠️ 不要**把实体改成 `record` —— `SessionCache._objList` 以实体对象为字典键，值相等语义会导致"两个 Id 相同的不同实例被视为同一缓存项"。

### ORMFramework.Test（5 文件）

| 文件 | 改造项 | 风险 |
| --- | --- | --- |
| `ComponentProviderTest.cs` | M(Tab→空格), T(11.5) | 低 |
| `ConfigManagerTest.cs` | T(11.1 删样板 ~50 行), T(11.4 路径+断言) | 低 |
| `ObjectCalculatorTest.cs` | T(11.1 删样板), T(11.3 删注释), T(11.4 重写真值表) | 低 |
| `SQLGeneratorTest.cs` | T(11.1), T(11.3 删未用字段+注释), T(11.4 SQL 快照) | 低 |
| `StatementAnalystTest.cs` | T(11.1), T(11.3 **删 `List2` 死类**), T(11.4 中缀/后缀快照) | 低 |

### 新增文件

| 文件 | 用途 | 阶段 |
| --- | --- | --- |
| `Directory.Build.props` | 集中 TFM / LangVersion / Nullable / 分析器 | 0 |
| `.editorconfig` | 风格规则（驱动 dotnet format） | 0 |
| `ORMFramework/Exceptions/OrmException.cs` 等 4 个 | 替换 28 处 `new Exception` | 5 |
| `ORMFramework/SQL/SqlLiteral.cs` | 消除 5 份重复的字面量格式化（6.3） | 3 |
| `ORMFramework.Test/TestData/Config.xml` | `ConfigManagerTest` 用测试夹具 | 8 |
| `ORMFramework.Test/SqlLiteralTest.cs`、`LoadListTest.cs` | 新增覆盖 | 8 |

---

## 16. 执行顺序、验证与回滚

### 16.1 推荐提交序列

| # | 提交主题 | 对应章节 | 预计 diff | 前置 |
| --- | --- | --- | --- | --- |
| 1 | `build: 升级到 net8.0 + Directory.Build.props + .editorconfig` | 3 | +80 / −20 | — |
| 2 | `fix: 修正 SessionCache.Collect 的 Guid 遍历类型错误` | 13.1 🔴 | ~10 行 | 1 |
| 3 | `fix: GetObjectsFromGlobalCache 的 ignoreIds 空引用` | 13.2 🔴 | ~5 行 | 1 |
| 4 | `test: 补齐 StatementAnalyst / SQLGenerator / ObjectCalculator 回归测试` | 11.4 | +400 | 1 |
| 5 | `style: file-scoped namespace（全项目）` | 4.1 | 全文件缩进 | 1 |
| 6 | `style: ImplicitUsings + 清理冗余 using` | 4.2 | −150 | 5 |
| 7 | `style: target-typed new + readonly 字段 + var` | 4.3–4.5 | −80 | 6 |
| 8 | `style: 统一 this. 前缀与私有字段命名` | 4.6, 3.3 | 局部 | 6 |
| 9 | `refactor: 手写属性 → 自动属性/表达式体成员` | 5.1–5.3 | **−300** | 7 |
| 10 | `refactor: 主构造函数 + sealed + static class` | 5.4, 5.6–5.7 | −40 | 9 |
| 11 | `refactor: LoadList 懒加载守卫收敛` | 5.5 | −60 | 9 |
| 12 | `refactor: switch 表达式 + 布尔表达式折叠` | 6.1–6.2 | −180 | 4, 9 |
| 13 | `refactor: 抽取 SqlLiteral 消除 5 份重复格式化` | 6.3 | −60 / +40 | 4, 12 |
| 14 | `refactor: 模式匹配（is not / 属性模式 / 列表模式）` | 6.4, 7.5 | −100 | 12 |
| 15 | `refactor: 卫语句化 ConfigManager（含 using 声明）` | 6.5, 8.1 | −40 | 4, 12 |
| 16 | `perf: TryGetValue/TryAdd 消除双查找` | 7.3 | ~60 处 | 7 |
| 17 | `refactor: 集合表达式 + FrozenSet/SearchValues` | 7.1–7.2 | −40 | 16 |
| 18 | `refactor: 插值字符串 + InvariantCulture` | 7.4 | ~35 处 | 13 |
| 19 | `refactor: LINQ + KeyValuePair 解构 + StringBuilder.Clear` | 7.6–7.9 | −60 | 16 |
| 20 | `refactor: using 声明 + 移除终结器 + 幂等 Dispose` | 8.1–8.2 | −30 | 15 |
| 21 | `refactor: throw; + 具体异常类型` | 8.3–8.5 | +60 | 20 |
| 22 | `refactor: typeof(LoadList<>) + nameof 替代字符串反射` | 9.1–9.2 | −20 | 11 |
| 23 | `refactor: 启用 Nullable（按目录分 7 个子提交）` | 10 | ~200 处 | 22 |
| 24 | `test: 清理 VS2005 样板与死代码` | 11.1–11.3, 11.5 | **−250** | 4 |
| 25 | `chore: 删除死代码（ObjectCache/DatabaseType/注释块）` | 13.13 | −120 | 24 |
| 26 | `fix: 其余既有缺陷（逐项独立子提交）` | 13.3–13.20 | 视项 | 23 |

预计净效果：**框架代码行数减少约 25%~30%**（约 1200 行），无功能变更。

### 16.2 每次提交的验证门槛

```bash
dotnet format --verify-no-changes SchoolEndORM.sln     # 风格一致
dotnet build SchoolEndORM.sln -warnaserror             # 零警告
dotnet test ORMFramework.Test/ORMFramework.Test.csproj # 全绿
```

对涉及 SQL 生成的提交（#12–#19），额外做**SQL 文本快照对比**：改造前先跑一次 `SQLGeneratorTest` 把生成的 SQL 落盘为基线，改造后逐字节比对。

对涉及 `GlobalCache` 的提交（#16、#19），额外手工执行 `Program.TestMultitransaction`（需先取消注释 + 准备本地 MySQL），观察两个线程的读写锁交互输出是否与改造前一致。

### 16.3 回滚策略

每个提交独立可 revert。若 #12–#14（表达式改写）出现回归，用 `git bisect run dotnet test` 定位到具体提交。这是"一个提交只做一类变换"这条原则的直接收益。

### 16.4 不纳入本次改造的后续提案

> **变更记录（提交 #26「其余既有缺陷与文档收尾」）**：以下 8 项均属于**本变更之外的后续独立提案**，未在本变更实施。本变更实际完成的提交范围：`dfd339e..9842586`（StatementAnalyst 末尾空格/重复分析、ObjectCalculator 变量比较、GlobalCache.Update 缺失键、IoC 生命周期表与 SetSession、Examples 配置自包含、实体 Equals 现代化、AGENTS.md 更新）。

1. `GlobalCache` 锁模型重写（`ReaderWriterLockSlim` / `SemaphoreSlim`）—— 需先建并发测试，见 12.1。
2. 全链路异步 + `DbDataReader` 替代 `DataSet` —— 破坏公开 API，见 12.2。
3. 参数化 SQL（`DbParameter`）—— 安全议题，架构级，见 7.4 结尾。
4. `log4net` → `Microsoft.Extensions.Logging` —— 见 12.3。
5. 公开 API 拼写修正（`NifixExpression`、`MantToMany`、`ReleaseReferenct`、`KeyColum` 系列）—— breaking change，见 14.1。
6. `Statment` 命名空间改名、命名空间与目录对齐 —— 见 14.5。
7. `ConfigManager` 改用 `XDocument`/`XmlSerializer` —— 见 14.4。
8. 反射结果缓存（`ConcurrentDictionary<Type, PropertyInfo[]>`）—— 见 9.3。
