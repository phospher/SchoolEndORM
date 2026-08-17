using System.Globalization;

namespace ORMFramework.SQL;

/// <summary>
/// 将字面量拼接进 SQL 字符串；不使用参数化查询，不提供 SQL 注入防护，调用方需自行约束输入。
/// </summary>
internal static class SqlLiteral
{
    public static string Format(object? value) => value switch
    {
        null => "NULL",
        string or char => $"'{value}'",
        DateTime dt => FormattableString.Invariant($"'{dt:yyyy-MM-dd HH:mm:ss}'"),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };

    public static string Comparison(string column, object? value) =>
        value is null ? $"{column} is null" : $"{column}={Format(value)}";
}
