## Purpose

定义 ORM 在各类失败场景下抛出的异常类型契约，使调用方能够按失败原因分类捕获，而不是只能捕获 `Exception` 基类。

## Requirements

### Requirement: 分类异常层次

框架抛出的所有自身定义的失败 MUST 使用一个共同的 ORM 异常基类的派生类型表达，且 MUST 至少区分三类原因：查询串语法/求值错误、配置文件错误、缓存并发冲突。框架 MUST NOT 直接抛出 `Exception` 基类实例。

#### Scenario: 查询串非法

- **WHEN** 调用方传入语法非法的查询谓词串（例如无法识别的运算符、括号不匹配、未闭合引号、非法常量）
- **THEN** 抛出查询语法异常类型，`Message` 包含出错的原始片段，且该类型派生自 ORM 异常基类

#### Scenario: 配置文件非法

- **WHEN** 解析的配置文件缺少 XML 声明、根元素不是 `Configuration`、或包含无法识别的元素
- **THEN** 抛出配置异常类型，`Message` 说明具体是哪一项校验失败

#### Scenario: 未持有写锁就更新

- **WHEN** 调用方在未取得对象写锁的情况下更新或删除二级缓存中的对象，或操作已被删除的对象
- **THEN** 抛出并发冲突异常类型

#### Scenario: 按类别捕获

- **WHEN** 调用方书写 `catch (OrmException)`
- **THEN** 上述三类失败均被捕获，而与 ORM 无关的异常不被吞掉

#### Scenario: 现有捕获代码不被破坏

- **WHEN** 既有调用方书写 `catch (Exception)`
- **THEN** 行为与改造前一致，所有 ORM 异常仍可被捕获

### Requirement: 参数与状态校验使用标准异常

对调用方传参错误 MUST 抛出标准的参数异常并携带参数名；对对象状态错误 MUST 抛出 `InvalidOperationException`。

#### Scenario: 空查询串

- **WHEN** 传入 null、空串或仅含空白的查询串
- **THEN** 抛出 `ArgumentException`（或其 `ArgumentNullException` 派生类型），`ParamName` 为该参数名

#### Scenario: 类型不匹配的参数

- **WHEN** 传入的对象类型不满足 API 约定（例如新旧对象类型不一致、数据适配器类型不符）
- **THEN** 抛出 `ArgumentException` 且 `ParamName` 为对应参数名

#### Scenario: 未配置连接串

- **WHEN** 在未设置连接字符串的情况下请求数据库连接
- **THEN** 抛出 `InvalidOperationException`

#### Scenario: 重复插入同一对象

- **WHEN** 同一对象在同一工作单元中被重复登记为新增
- **THEN** 抛出 `InvalidOperationException`

### Requirement: 异常传播保留原始堆栈

在捕获后需要向上传播的场合，框架 MUST 保留原始异常的堆栈信息。

#### Scenario: 提交失败后回滚并抛出

- **WHEN** 工作单元提交过程中数据库操作抛出异常
- **THEN** 事务被回滚，且传播出的异常的 `StackTrace` 仍指向最初的抛出点

#### Scenario: 更新/删除/插入失败后回滚并抛出

- **WHEN** 在取写锁或登记变更的过程中发生异常
- **THEN** 会话内已做的修改被还原，且传播出的异常保留原始抛出点

### Requirement: 配置引用了不存在的属性时给出可读错误

当映射配置引用了实体类型上不存在的属性名时，框架 MUST 抛出说明性的配置异常，MUST NOT 抛出 `NullReferenceException`。

#### Scenario: 映射键名拼写错误

- **WHEN** 配置中某个主键列或引用列的名字在对应实体类型上找不到同名公开属性
- **THEN** 抛出配置异常，`Message` 同时包含该属性名与所属类型名
