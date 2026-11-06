using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Cache;
using ORMFramework.Statment;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public sealed class ObjectCalculatorTest
{
    private sealed class CalcEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime Created { get; set; }
        public bool Active { get; set; }
        public char Grade { get; set; }
        public bool Other { get; set; }
        public string? Nickname { get; set; }
    }

    private static bool IsTarget(CalcEntity entity, string query)
    {
        QueryExpression expression = new StatementAnalyst(query).GetQueryExpression();
        return ObjectCalculator.IsTargetObject(entity, expression);
    }

    [TestMethod]
    [DataRow("Id==1", true)]
    [DataRow("Id==2", false)]
    [DataRow("Id!=1", false)]
    [DataRow("Id!=2", true)]
    [DataRow("Id>0", true)]
    [DataRow("Id>1", false)]
    [DataRow("Id>=1", true)]
    [DataRow("Id>=2", false)]
    [DataRow("Id<2", true)]
    [DataRow("Id<1", false)]
    [DataRow("Id<=1", true)]
    [DataRow("Id<=0", false)]
    [DataRow("Name=='abc'", true)]
    [DataRow("Name=='xyz'", false)]
    [DataRow("Name!='abc'", false)]
    [DataRow("Created=='2024-01-02 03:04:05'", true)]
    [DataRow("Created=='2024-01-02 03:04:06'", false)]
    // 两个 bool 属性比较时走数值路径（Convert.ToDouble），当前返回 true。
    [DataRow("Active==Other", true)]
    public void IsTargetObject_Comparisons_ReturnExpectedTruthTable(string query, bool expected)
    {
        var entity = new CalcEntity
        {
            Id = 1,
            Name = "abc",
            Created = new DateTime(2024, 1, 2, 3, 4, 5),
            Active = true,
            Other = true,
            Grade = 'A',
        };

        Assert.AreEqual(expected, IsTarget(entity, query));
    }

    [TestMethod]
    [DataRow("Active==true")]
    [DataRow("Active==false")]
    [DataRow("Grade=='A'")]
    [DataRow("Grade=='B'")]
    public void IsTargetObject_BoolAndCharDefects_ThrowInvalidCastException(string query)
    {
        // TODO(12.2): bool 常量参与 == 当前抛 InvalidCastException（IsString 对 bool 常量做强转），
        // 后续阶段修复后应断言 true/false 而非异常。
        // TODO(12.2): char 属性参与 == 当前抛 InvalidCastException（CalculateStringOperator 对 char 值强转为 string），
        // 后续阶段修复后应断言 true/false 而非异常。
        var entity = new CalcEntity { Active = true, Other = true, Grade = 'A' };

        Assert.ThrowsExactly<InvalidCastException>(() => IsTarget(entity, query));
    }

    [TestMethod]
    public void IsTargetObject_LogicalCombinations_ReturnExpectedResults()
    {
        var entity = new CalcEntity { Id = 1, Name = "abc", Active = true };

        Assert.IsTrue(IsTarget(entity, "Name=='abc' && Id==1"));
        Assert.IsTrue(IsTarget(entity, "Name=='abc' || Id==2"));
        Assert.IsTrue(IsTarget(entity, "!(Name=='xyz')"));
        Assert.IsFalse(IsTarget(entity, "!(Name=='abc')"));
        Assert.IsTrue(IsTarget(entity, "Id==1 && Id!=2"));
        Assert.IsTrue(IsTarget(entity, "Id==1 || Id==2"));
        Assert.IsFalse(IsTarget(entity, "Id==2 || Id==2"));
        Assert.IsTrue(IsTarget(entity, "!(Id==2)"));
        Assert.IsFalse(IsTarget(entity, "!(Id==1)"));

        // bool 常量参与 == 抛 InvalidCastException，导致整个 && 表达式抛异常。
        Assert.ThrowsExactly<InvalidCastException>(() => IsTarget(entity, "Name=='abc' && Active==true"));
    }

    [TestMethod]
    public void IsTargetObject_LikePatterns_ReturnExpectedResults()
    {
        var entity = new CalcEntity { Id = 1, Name = "abc" };

        Assert.IsTrue(IsTarget(entity, "Name like 'a%'"));      // % 前缀
        Assert.IsTrue(IsTarget(entity, "Name like '%c'"));      // % 后缀
        Assert.IsTrue(IsTarget(entity, "Name like '%b%'"));     // % 双侧
        Assert.IsFalse(IsTarget(entity, "Name like 'a_'"));     // _ 单字符：'a_' 共 2 字符
        Assert.IsTrue(IsTarget(entity, "Name like 'ab_'"));     // 'ab' + 单字符
        Assert.IsTrue(IsTarget(entity, "Name like '[a-c]bc'")); // 字符组命中
        Assert.IsFalse(IsTarget(entity, "Name like '[x-z]bc'"));// 字符组未命中
        Assert.IsTrue(IsTarget(entity, "Name=='abc'"));         // 单引号剥离
        Assert.IsTrue(IsTarget(entity, "Name like 'a%' && Id==1"));
        Assert.IsTrue(IsTarget(entity, "Name like 'x%' || Id==1"));
    }

    [TestMethod]
    public void IsTargetObject_TwoPropertyComparison_ComparesValues()
    {
        var entity = new CalcEntity { Name = "abc", Nickname = "abc" };
        Assert.IsTrue(IsTarget(entity, "Name==Nickname"));
    }

    [TestMethod]
    public void IsTargetObject_EmptyStringProperty_ComparesNormally()
    {
        // 6.20 的列表模式剥离修正了空串越界：空串属性现在正常参与比较，不再抛 IndexOutOfRangeException。
        var entity = new CalcEntity { Name = "" };
        Assert.IsFalse(IsTarget(entity, "Name=='abc'"));
        Assert.IsTrue(IsTarget(entity, "Name==''"));
    }
}
