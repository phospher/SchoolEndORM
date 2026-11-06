SchoolEndORM
============

The last project of my school life

## 安全边界

SQL 由字符串拼接生成、不使用参数化、不提供 SQL 注入防护、需调用方自行约束输入。

## 运行时要求（BREAKING）

最低运行时为 **.NET 10**；目标框架 `net10.0`、语言版本 **C# 14**（由仓库根 `Directory.Build.props` 统一声明，`global.json` 固定 10.0.1xx Feature Band SDK）。

本次升级不保留 `net8.0` 兼容目标：仅安装 .NET 8 运行时的下游消费者无法运行本框架，必须同步升级到 .NET 10 运行时。

