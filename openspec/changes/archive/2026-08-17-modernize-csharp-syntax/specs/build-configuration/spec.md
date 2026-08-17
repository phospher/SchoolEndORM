## Purpose

约定本仓库全部项目在编译期必须满足的目标框架、语言版本、可空上下文与代码风格门槛，使"现代 C#"从口头约定变成可由构建命令验证的事实。

## ADDED Requirements

### Requirement: 统一目标框架与语言版本

仓库内全部项目 MUST 以 `net8.0` 为目标框架、以 C# 12 为语言版本，且这些属性 MUST 在仓库根的统一位置声明一次，而非在各项目文件中重复。

#### Scenario: 构建产物落在 net8.0

- **WHEN** 执行 `dotnet build SchoolEndORM.sln`
- **THEN** 三个项目均输出到 `net8.0` 目标目录，且没有任何项目仍声明 `net6.0`

#### Scenario: 使用 C# 12 语法不需要额外配置

- **WHEN** 任意项目中的源文件使用集合表达式或主构造函数
- **THEN** 编译成功，且该项目文件本身未声明 `LangVersion`

#### Scenario: 单个项目不重复声明公共属性

- **WHEN** 检查任一 `.csproj`
- **THEN** 其中不包含 `TargetFramework`、`LangVersion`、`ImplicitUsings`、`Nullable`、分析器开关等公共属性的重复声明

### Requirement: 隐式 using 与可空上下文全量启用

全部项目 MUST 启用隐式 using 与可空引用类型（`Nullable=enable`）。

#### Scenario: 源文件不再包含隐式 using 提供的命名空间

- **WHEN** 检查任意源文件的 using 列表
- **THEN** 其中不出现 `System`、`System.Collections.Generic`、`System.Linq`、`System.Text`（当该文件确实使用 `StringBuilder` 时可显式保留）

#### Scenario: 可空性告警按错误处理

- **WHEN** 执行 `dotnet build SchoolEndORM.sln -warnaserror:CS8600,CS8601,CS8602,CS8603,CS8604,CS8618,CS8625`
- **THEN** 构建成功且退出码为 0

### Requirement: 代码风格与分析器可机器验证

仓库 MUST 提供一份可执行的风格规则，且构建 MUST 启用 .NET 分析器与构建期风格检查。

#### Scenario: 格式检查通过

- **WHEN** 执行 `dotnet format --verify-no-changes SchoolEndORM.sln`
- **THEN** 命令成功退出，且不报告任何待修改文件

#### Scenario: 零警告构建

- **WHEN** 执行 `dotnet build SchoolEndORM.sln -warnaserror`
- **THEN** 构建成功，无任何警告被提升为错误

#### Scenario: 缩进与命名一致

- **WHEN** 检查任意源文件
- **THEN** 缩进为 4 个空格（无制表符），私有实例字段命名为 `_camelCase`，且成员访问不使用 `this.` 前缀

### Requirement: 示例项目配置自包含可运行

示例项目 MUST 不依赖任何开发者机器上的绝对路径，其配置文件 MUST 随构建复制到输出目录。

#### Scenario: 从任意机器运行示例

- **WHEN** 在一台全新克隆仓库的机器上执行 `dotnet run --project ORMFramework.Examples`
- **THEN** 程序能定位到输出目录中的 `Config.xml` 并开始按其中的连接串工作，不会因路径不存在而失败

### Requirement: 测试项目脱离机器环境

测试 MUST 不依赖任何机器专属的绝对路径；需要配置文件的测试 MUST 使用随测试分发的夹具文件。

#### Scenario: 全量测试在任意机器通过

- **WHEN** 在一台全新克隆仓库的机器上执行 `dotnet test ORMFramework.Test/ORMFramework.Test.csproj`
- **THEN** 全部测试通过，包括读取配置文件的测试
