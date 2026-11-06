using System.Globalization;
using System.Text;

namespace ORMFramework.SQL;

/// <summary>
/// 将字面量拼接进 SQL 字符串；不使用参数化查询，不提供 SQL 注入防护，调用方需自行约束输入。
/// </summary>
internal static class SqlLiteral
{
    extension(object? value)
    {
        public string Format() => value switch
        {
            null => "NULL",
            string or char => $"'{value}'",
            DateTime dt => FormattableString.Invariant($"'{dt:yyyy-MM-dd HH:mm:ss}'"),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    extension(string column)
    {
        public string Comparison(object? value) =>
            value is null ? $"{column} is null" : $"{column}={value.Format()}";
    }

    public static void AppendComparison(this StringBuilder builder, string column, object? value)
    {
        builder.Append(column);
        builder.Append("==");
        builder.Append(value.Format());
    }
}
