using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.SQL;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public sealed class SqlLiteralTest
{
    [TestMethod]
    [DataRow(null, "NULL")]
    [DataRow("abc", "'abc'")]
    [DataRow('A', "'A'")]
    [DataRow(1, "1")]
    [DataRow(true, "True")]
    public void Format_ReturnsExpected(object? input, string expected)
    {
        Assert.AreEqual(expected, SqlLiteral.Format(input));
    }

    [TestMethod]
    public void Format_DateTime_ReturnsQuotedInvariantFormat()
    {
        Assert.AreEqual("'2024-01-02 03:04:05'", SqlLiteral.Format(new DateTime(2024, 1, 2, 3, 4, 5)));
    }

    [TestMethod]
    [DoNotParallelize]
    public void Format_Double_ReturnsInvariantNumber_EvenUnderDeDeCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.AreEqual("1.5", SqlLiteral.Format(1.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    [DataRow(null, "Name is null")]
    [DataRow("abc", "Name='abc'")]
    public void Comparison_ReturnsExpected(object? value, string expected)
    {
        Assert.AreEqual(expected, SqlLiteral.Comparison("Name", value));
    }
}
