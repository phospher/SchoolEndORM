using System.Globalization;

namespace ORMFramework.SQL;

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
