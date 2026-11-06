# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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
