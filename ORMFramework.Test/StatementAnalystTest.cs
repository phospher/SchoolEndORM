using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Exceptions;
using ORMFramework.Statment;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class StatementAnalystTest
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
    public void GetQueryExpression_IdEquals1_ProducesExpectedInfixAndSuffix()
    {
        QueryExpression expression = Parse("Id==1");
        Assert.AreEqual("Variable:Id|Operator:Equals|Constant:1", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:Id|Constant:1|Operator:Equals", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void GetQueryExpression_AndLike_ProducesExpectedInfixAndSuffix()
    {
        QueryExpression expression = Parse("a==1 && b like 'x%'");
        Assert.AreEqual("Variable:a|Operator:Equals|Constant:1 |Operator:And|Variable:b|Operator:Like|Constant:'x%'", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:a|Constant:1 |Operator:Equals|Variable:b|Constant:'x%'|Operator:Like|Operator:And", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void GetQueryExpression_NotParenthesized_ProducesExpectedInfixAndSuffix()
    {
        QueryExpression expression = Parse("! ( b== 3 )");
        Assert.AreEqual("Operator:Not|Operator:LeftParenthesis|Variable:b|Operator:Equals|Constant:3 |Operator:RightParenthesis", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:b|Constant:3 |Operator:Equals|Operator:Not", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void GetQueryExpression_MixedExpression_ProducesExpectedInfixAndSuffix()
    {
        QueryExpression expression = Parse("Student like '%' && a=='2' || ! ( b== 3 )");
        Assert.AreEqual("Variable:Student|Operator:Like|Constant:'%'|Operator:And|Variable:a|Operator:Equals|Constant:'2'|Operator:Or|Operator:Not|Operator:LeftParenthesis|Variable:b|Operator:Equals|Constant:3 |Operator:RightParenthesis", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:Student|Constant:'%'|Operator:Like|Variable:a|Constant:'2'|Operator:Equals|Operator:And|Variable:b|Constant:3 |Operator:Equals|Operator:Not|Operator:Or", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void QueryString_Empty_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => Parse(""));
        Assert.AreEqual("value", ex.ParamName);
    }

    [TestMethod]
    public void QueryString_WhitespaceOnly_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => Parse("   "));
        Assert.AreEqual("value", ex.ParamName);
    }

    [TestMethod]
    public void QueryString_Null_ThrowsArgumentNullException()
    {
        // ThrowIfNullOrWhiteSpace(null) 抛 ArgumentNullException（ArgumentException 子类）。
        var ex = Assert.ThrowsException<ArgumentNullException>(() => Parse(null));
        Assert.AreEqual("value", ex.ParamName);
    }

    [TestMethod]
    public void QueryString_UnbalancedParenthesis_ThrowsQuerySyntaxException()
    {
        var ex = Assert.ThrowsException<QuerySyntaxException>(() => Parse("! ( b== 3"));
        Assert.AreEqual(") is expected", ex.Message);
    }

    [TestMethod]
    public void QueryString_UnclosedQuote_ThrowsQuerySyntaxException()
    {
        var ex = Assert.ThrowsException<QuerySyntaxException>(() => Parse("a=='x"));
        Assert.AreEqual("' is expected", ex.Message);
    }

    [TestMethod]
    public void QueryString_UnrecognizedOperator_ThrowsQuerySyntaxException()
    {
        var ex = Assert.ThrowsException<QuerySyntaxException>(() => Parse("a=<1"));
        Assert.AreEqual("Invaild Operator:=<", ex.Message);
    }

    [TestMethod]
    public void QueryString_InvalidNumericConstant_ThrowsQuerySyntaxException()
    {
        var ex = Assert.ThrowsException<QuerySyntaxException>(() => Parse("a==1x"));
        Assert.AreEqual("Invalid Vairable:1x", ex.Message);
    }

    [TestMethod]
    public void QueryString_TrailingSpaceAfterConstant_RecordsCurrentBehavior()
    {
        // TODO(12.1): 修复 13.11 后，"Id==1 " 应断言与 "Id==1" 相同的序列（末尾空白视为输入结束）。
        // 注意：任务书预期此处抛 IndexOutOfRangeException，但当前实现把末尾空格并入常量，
        // 实际得到常量 "1 "（带尾随空格）；真正触发 IndexOutOfRangeException 的是变量/带引号常量后接末尾空格（见下）。
        QueryExpression expression = Parse("Id==1 ");
        Assert.AreEqual("Variable:Id|Operator:Equals|Constant:1 ", Render(expression.NifixExpression));
        Assert.AreEqual("Variable:Id|Constant:1 |Operator:Equals", Render(expression.SuffixExpression));
    }

    [TestMethod]
    public void QueryString_TrailingSpaceAfterVariable_ThrowsIndexOutOfRange()
    {
        // TODO(12.1): 修复 13.11 后改为断言与 "Id" 相同的序列（末尾空白视为输入结束）。
        // 这是缺陷 13.11（while (currentChar == ' ') currentChar = _queryString[++_stringIndex] 越界）的真实触发形态。
        Assert.ThrowsException<IndexOutOfRangeException>(() => Parse("Id "));
    }
}
