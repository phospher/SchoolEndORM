using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Cache;
using ORMFramework.Statment;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class ObjectCalculatorTest
{
    private sealed class CalcEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public bool Active { get; set; }
        public char Grade { get; set; }
        public bool Other { get; set; }
        public string Nickname { get; set; }
    }

    private static bool IsTarget(CalcEntity entity, string query)
    {
        QueryExpression expression = new StatementAnalyst(query).GetQueryExpression();
        return ObjectCalculator.IsTargetObject(entity, expression);
    }

    [TestMethod]
    public void IsTargetObject_IntComparisons_ReturnExpectedTruthTable()
    {
        var entity = new CalcEntity { Id = 1 };

        Assert.IsTrue(IsTarget(entity, "Id==1"));
        Assert.IsFalse(IsTarget(entity, "Id==2"));
        Assert.IsFalse(IsTarget(entity, "Id!=1"));
        Assert.IsTrue(IsTarget(entity, "Id!=2"));
        Assert.IsTrue(IsTarget(entity, "Id>0"));
        Assert.IsFalse(IsTarget(entity, "Id>1"));
        Assert.IsTrue(IsTarget(entity, "Id>=1"));
        Assert.IsFalse(IsTarget(entity, "Id>=2"));
        Assert.IsTrue(IsTarget(entity, "Id<2"));
        Assert.IsFalse(IsTarget(entity, "Id<1"));
        Assert.IsTrue(IsTarget(entity, "Id<=1"));
        Assert.IsFalse(IsTarget(entity, "Id<=0"));
    }

    [TestMethod]
    public void IsTargetObject_StringComparisons_ReturnExpectedTruthTable()
    {
        var entity = new CalcEntity { Name = "abc" };

        Assert.IsTrue(IsTarget(entity, "Name=='abc'"));
        Assert.IsFalse(IsTarget(entity, "Name=='xyz'"));
        Assert.IsFalse(IsTarget(entity, "Name!='abc'"));
    }

    [TestMethod]
    public void IsTargetObject_DateTimeComparisons_ReturnExpectedTruthTable()
    {
        var entity = new CalcEntity { Created = new DateTime(2024, 1, 2, 3, 4, 5) };

        Assert.IsTrue(IsTarget(entity, "Created=='2024-01-02 03:04:05'"));
        Assert.IsFalse(IsTarget(entity, "Created=='2024-01-02 03:04:06'"));
    }

    [TestMethod]
    public void IsTargetObject_BoolComparisons_RecordCurrentBehavior()
    {
        var entity = new CalcEntity { Active = true, Other = true };

        // TODO(12.2): bool 常量参与 == 当前抛 InvalidCastException（IsString 对 bool 常量做强转），
        // 后续阶段修复后应断言 true/false 而非异常。
        Assert.ThrowsException<InvalidCastException>(() => IsTarget(entity, "Active==true"));
        Assert.ThrowsException<InvalidCastException>(() => IsTarget(entity, "Active==false"));

        // 两个 bool 属性比较时走数值路径（Convert.ToDouble），当前返回 true。
        Assert.IsTrue(IsTarget(entity, "Active==Other"));
    }

    [TestMethod]
    public void IsTargetObject_CharComparisons_RecordCurrentBehavior()
    {
        var entity = new CalcEntity { Grade = 'A' };

        // TODO(12.2): char 属性参与 == 当前抛 InvalidCastException（CalculateStringOperator 对 char 值强转为 string），
        // 后续阶段修复后应断言 true/false 而非异常。
        Assert.ThrowsException<InvalidCastException>(() => IsTarget(entity, "Grade=='A'"));
        Assert.ThrowsException<InvalidCastException>(() => IsTarget(entity, "Grade=='B'"));
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
        Assert.ThrowsException<InvalidCastException>(() => IsTarget(entity, "Name=='abc' && Active==true"));
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
    public void IsTargetObject_TwoPropertyComparison_RecordsCurrentBug()
    {
        // TODO(12.2): 修复 13.9 后，Name==Nickname 应比较两个属性值（此处 Name="abc"、Nickname="abc" 应相等）。
        // 当前实现把 y 为 Variable 时的属性名（"Nickname"）当作值参与比较，故返回 false。
        var entity = new CalcEntity { Name = "abc", Nickname = "abc" };
        Assert.IsFalse(IsTarget(entity, "Name==Nickname"));
    }

    [TestMethod]
    public void IsTargetObject_EmptyStringProperty_RecordsCurrentBug()
    {
        // TODO(12.2): 修复后空串属性参与比较不应越界（当前 CalculateStringOperator 的 xStr[0] 空串越界抛 IndexOutOfRangeException）。
        var entity = new CalcEntity { Name = "" };
        Assert.ThrowsException<IndexOutOfRangeException>(() => IsTarget(entity, "Name=='abc'"));
        Assert.ThrowsException<IndexOutOfRangeException>(() => IsTarget(entity, "Name==''"));
    }
}
