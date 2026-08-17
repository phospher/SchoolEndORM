## Purpose

定义框架从实体对象与查询表达式生成 SQL 文本时必须满足的可移植性与正确性契约，包括字面量格式化、空值表达、子句拼接与文本可回归性。

## Requirements

### Requirement: 字面量格式化与运行环境无关

生成 SQL 中的所有值字面量 MUST 使用不变文化（invariant culture）格式化，MUST NOT 受线程当前区域设置影响。

#### Scenario: 小数在逗号小数点区域仍生成点号

- **WHEN** 当前线程区域设置为 `de-DE`，为某个 `double` 或 `decimal` 属性生成 SQL
- **THEN** 生成的字面量使用点号作小数点（如 `1.5`），与 `en-US` 环境下逐字节一致

#### Scenario: 日期时间使用固定格式

- **WHEN** 为 `DateTime` 属性生成字面量
- **THEN** 输出为 `'yyyy-MM-dd HH:mm:ss'` 形式的带引号文本，且与区域设置无关

#### Scenario: 字符与字符串加引号

- **WHEN** 为 `char` 或 `string` 属性生成字面量
- **THEN** 输出被单引号包裹

#### Scenario: 各生成入口输出一致

- **WHEN** 同一个值分别经由 select 的 where 子句、insert 的值列表、update 的赋值与 where 子句、二级缓存排除键子句生成
- **THEN** 该值的字面量文本在所有入口完全一致

### Requirement: 空值使用 SQL 空值语义

值为 null 时，框架 MUST 生成 SQL 空值语义，MUST NOT 生成字符串 `'NULL'`。

#### Scenario: where 子句中的空值

- **WHEN** 用于比较的属性值为 null
- **THEN** 生成 `<列名> is null` 而非 `<列名>='NULL'`

#### Scenario: 插入值列表中的空值

- **WHEN** 待插入的属性值为 null
- **THEN** 生成不带引号的 `NULL` 关键字

### Requirement: 子句拼接语法完整

生成的 SQL MUST 括号配对、MUST NOT 出现尾随分隔符。

#### Scenario: 无查询条件的查询

- **WHEN** 请求某类型的查询 SQL 且未提供查询表达式
- **THEN** 生成 `select ... from <表>`，不含 `where`，也不含任何多余的右括号

#### Scenario: 有查询条件的查询

- **WHEN** 提供了查询表达式
- **THEN** 生成 `select ... from <表> where (...)`，括号左右配对

#### Scenario: 最后一个属性被跳过

- **WHEN** 实体的最后一个公开属性是导航属性因而被排除在列清单之外
- **THEN** 生成的语句末尾不残留 `,` 或 ` and `

### Requirement: 生成文本可回归比对

框架 MUST 能在不连接数据库的前提下，为给定的映射与实体生成完整 SQL 文本，以便逐字节回归比对。

#### Scenario: 用内存映射生成四类语句

- **WHEN** 以内存中构造的持久化上下文与映射调用 select / insert / update / delete 的 SQL 生成
- **THEN** 全部返回完整 SQL 字符串且不发生任何数据库连接

#### Scenario: 改造前后文本一致

- **WHEN** 对同一组输入在改造前后分别生成 SQL
- **THEN** 除本规格明确要求变更的空值表达与区域无关格式化之外，输出逐字节一致

### Requirement: 明示不防注入的限制

框架 MUST 在公开文档与相关代码注释中显式声明：SQL 由字符串拼接生成，不使用参数化，因此不提供 SQL 注入防护。

#### Scenario: 阅读文档了解安全边界

- **WHEN** 使用者查阅 README 或 SQL 生成相关类型的文档注释
- **THEN** 能看到"拼接生成、不防注入、需调用方自行约束输入"的明确说明
