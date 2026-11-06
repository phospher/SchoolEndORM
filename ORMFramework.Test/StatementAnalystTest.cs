using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Exceptions;
using ORMFramework.Statment;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public sealed class StatementAnalystTest
{
    private static string Render(List<StatElement> expression)
    {
        var parts = new List<string>();
        foreach (StatElement element in expression)
        {
            if (element.Type == StatElementType.Operator)
            {
                parts.Add("Operator:" + (Operator)element.Value);
            }
            else
            {
                parts.Add(element.Type + ":" + element.Value);
            }
        }
        return string.Join("|", parts);
    }

    private static QueryExpression Parse(string? query)
    {
        return new StatementAnalyst(query!).GetQueryExpression();
    }

    [TestMethod]
    [DataRow("Id==1", "Variable:Id|Operator:Equals|Constant:1", "Variable:Id|Constant:1|Operator:Equals")]
    [DataRow("a==1 && b like 'x%'", "Variable:a|Operator:Equals|Constant:1 |Operator:And|Variable:b|Operator:Like|Constant:'x%'", "Variable:a|Constant:1 |Operator:Equals|Variable:b|Constant:'x%'|Operator:Like|Operator:And")]
    [DataRow("! ( b== 3 )", "Operator:Not|Operator:LeftParenthesis|Variable:b|Operator:Equals|Constant:3 |Operator:RightParenthesis", "Variable:b|Constant:3 |Operator:Equals|Operator:Not")]
    [DataRow("Student like '%' && a=='2' || ! ( b== 3 )", "Variable:Student|Operator:Like|Constant:'%'|Operator:And|Variable:a|Operator:Equals|Constant:'2'|Operator:Or|Operator:Not|Operator:LeftParenthesis|Variable:b|Operator:Equals|Constant:3 |Operator:RightParenthesis", "Variable:Student|Constant:'%'|Operator:Like|Variable:a|Constant:'2'|Operator:Equals|Operator:And|Variable:b|Constant:3 |Operator:Equals|Operator:Not|Operator:Or")]
    public void GetQueryExpression_ProducesExpectedInfixAndSuffix(string query, string expectedInfix, string expectedSuffix)
    {
        QueryExpression expression = Parse(query);
        Assert.AreEqual(expectedInfix, Render(expression.NifixExpression));
        Assert.AreEqual(expectedSuffix, Render(expression.SuffixExpression));
    }

    [TestMethod]
    [DataRow("", typeof(ArgumentException), "value")]
    [DataRow("   ", typeof(ArgumentException), "value")]
    [DataRow(null, typeof(ArgumentNullException), "value")]
    [DataRow("! ( b== 3", typeof(QuerySyntaxException), ") is expected")]
    [DataRow("a=='x", typeof(QuerySyntaxException), "' is expected")]
    [DataRow("a=<1", typeof(QuerySyntaxException), "Invaild Operator:=<")]
    [DataRow("a==1x", typeof(QuerySyntaxException), "Invalid Vairable:1x")]
    public void QueryString_Invalid_ThrowsExpectedException(string? query, Type exceptionType, string message)
    {
        Exception? thrown = null;
        try
        {
            Parse(query);
        }
        catch (Exception caught)
        {
            thrown = caught;
        }

        Assert.IsNotNull(thrown);
        Exception ex = thrown!;
        Assert.AreEqual(exceptionType, ex.GetType());
        string actual = ex is ArgumentException argumentException
            ? argumentException.ParamName!
            : ex.Message;
        Assert.AreEqual(message, actual);
    }

    [TestMethod]
    public void QueryString_TrailingSpaceAfterConstant_TreatedAsEndOfInput()
    {
        QueryExpression expression = Parse("Id==1 ");
        Assert.AreEqual("Variable:Id|Operator:Equals|Constant:1", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:Id|Constant:1|Operator:Equals", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void QueryString_TrailingSpaceAfterVariable_TreatedAsEndOfInput()
    {
        QueryExpression expression = Parse("Id ");
        Assert.AreEqual("Variable:Id", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:Id", Render(expression.SuffixExpression));
    }
}
