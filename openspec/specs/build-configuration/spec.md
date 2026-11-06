## Purpose

约定本仓库全部项目在编译期必须满足的目标框架、语言版本、可空上下文与代码风格门槛，使"现代 C#"从口头约定变成可由构建命令验证的事实。

## Requirements

### Requirement: 统一目标框架与语言版本

仓库内全部项目 MUST 以 `net10.0` 为目标框架、以 C# 14 为语言版本，且这些属性 MUST 在仓库根的统一位置声明一次，而非在各项目文件中重复。场景名称沿用基线规格中的稳定标识，以各场景的 WHEN / THEN 正文为当前要求。

#### Scenario: 构建产物落在 net8.0

- **WHEN** 执行 `dotnet build SchoolEndORM.sln`
- **THEN** 三个项目均输出到 `net10.0` 目标目录，且没有任何项目仍声明 `net8.0` 或更早的目标框架

#### Scenario: 使用 C# 12 语法不需要额外配置

- **WHEN** 任意项目中的源文件使用 `field` 关键字、`nameof` 非绑定泛型或集合表达式
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

### Requirement: 固定 .NET 10 SDK 版本

仓库 MUST 在根目录提供 `global.json` 声明 .NET 10 SDK 版本与 `rollForward` 策略，使三个项目在不同机器上都使用同一主版本的 SDK 构建。

#### Scenario: SDK 版本由 global.json 选定

- **WHEN** 在仓库根目录执行 `dotnet --version`
- **THEN** 输出为 `10.x`，且该版本由 `global.json` 解析得到，不依赖机器上恰好安装的最新 SDK

#### Scenario: SDK 与目标框架匹配

- **WHEN** 检查 `global.json` 与 `Directory.Build.props`
- **THEN** 声明的 SDK 主版本与 `TargetFramework=net10.0` 匹配，且 `LangVersion` 未被写成低于 14 的值

### Requirement: 依赖版本与目标框架对齐

全部 `PackageReference` MUST 使用声明支持 `net10.0` 的稳定版本；`dotnet restore` MUST 不产生 NU1701（框架资产回退）或 NU190x（已知漏洞）警告。

#### Scenario: 还原无兼容性警告

- **WHEN** 执行 `dotnet restore SchoolEndORM.sln`
- **THEN** 还原成功，且输出中不含 NU1701 / NU190x 警告

#### Scenario: 包资产覆盖 net10.0

- **WHEN** 检查 `obj/project.assets.json` 中各包提供的 target 资产
- **THEN** 每个包都提供 `net10.0` 兼容资产，而不是仅通过 `netstandard` 回退解析

### Requirement: 最低运行时要求为 .NET 10

库、示例程序与测试程序 MUST 仅在 .NET 10 或更高运行时上执行；仅安装 .NET 8 运行时的下游消费者 MUST 升级后才能引用本库。

#### Scenario: 运行配置声明 net10.0

- **WHEN** 检查三个项目编译产物中的 `*.runtimeconfig.json`
- **THEN** 其 `tfm` 为 `net10.0`，framework 版本为 `10.0.x`

#### Scenario: 旧运行时不被静默兼容

- **WHEN** 在仅安装 .NET 8 运行时的机器上尝试运行示例程序
- **THEN** 运行时明确报出缺少 .NET 10，而不是以兼容模式启动或降级到 .NET 8 行为

### Requirement: 新分析器告警的处置

由 .NET 10 SDK 引入的新分析器告警 MUST 被修复，或在 `.editorconfig` / `Directory.Build.props` 中被有理由地抑制并附注释；构建 MUST 保持零警告。

#### Scenario: 零警告构建

- **WHEN** 执行 `dotnet build SchoolEndORM.sln -warnaserror`
- **THEN** 构建成功，没有任何警告被提升为错误

#### Scenario: 抑制均有理由

- **WHEN** 检查 `.editorconfig` 中严重度设为 `none` 的规则
- **THEN** 每条规则都带有说明抑制理由的注释，且理由与项目既有红线一致
