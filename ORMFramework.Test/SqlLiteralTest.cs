using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.SQL;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public class SqlLiteralTest
{
    [TestMethod]
    public void Format_Null_ReturnsNullKeyword()
    {
        Assert.AreEqual("NULL", SqlLiteral.Format(null));
    }

    [TestMethod]
    public void Format_String_ReturnsSingleQuoted()
    {
        Assert.AreEqual("'abc'", SqlLiteral.Format("abc"));
    }

    [TestMethod]
    public void Format_Char_ReturnsSingleQuoted()
    {
        Assert.AreEqual("'A'", SqlLiteral.Format('A'));
    }

    [TestMethod]
    public void Format_DateTime_ReturnsQuotedInvariantFormat()
    {
        Assert.AreEqual("'2024-01-02 03:04:05'", SqlLiteral.Format(new DateTime(2024, 1, 2, 3, 4, 5)));
    }

    [TestMethod]
    public void Format_Int_ReturnsInvariantNumber()
    {
        Assert.AreEqual("1", SqlLiteral.Format(1));
    }

    [TestMethod]
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
    public void Format_Bool_ReturnsKeyword()
    {
        Assert.AreEqual("True", SqlLiteral.Format(true));
    }

    [TestMethod]
    public void Comparison_Null_ReturnsIsNull()
    {
        Assert.AreEqual("Name is null", SqlLiteral.Comparison("Name", null));
    }

    [TestMethod]
    public void Comparison_Value_ReturnsEquality()
    {
        Assert.AreEqual("Name='abc'", SqlLiteral.Comparison("Name", "abc"));
    }
}
