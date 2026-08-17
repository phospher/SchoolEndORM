using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Configuration;
using ORMFramework.SQL;
using ORMFramework.Statment;
using ORMFramework.Test.Fakes;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class SQLGeneratorTest
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public double Score { get; set; }
        public IList<TestEntity> Children { get; set; }
    }

    private static SQLGenerator CreateGenerator()
    {
        var persistenceContext = new FakePersistenceContext();
        var mapping = new EntityMapping
        {
            ClassName = typeof(TestEntity).FullName,
            TableName = "TestEntity",
            Keys = new[] { "Id" }
        };
        persistenceContext.Mappings[typeof(TestEntity).FullName] = mapping;
        return new SQLGenerator { PersistenceContext = persistenceContext };
    }

    [TestMethod]
    public void GetSelectSQL_WithoutQueryExpression_MatchesCurrentSnapshot()
    {
        // TODO(7.9): 修复 13.6 后（无查询条件时不追加多余 ")"）更新此快照。
        string actual = CreateGenerator().GetSelectSQL(typeof(TestEntity));
        Assert.AreEqual("select * from TestEntity)", actual);
    }

    [TestMethod]
    public void GetSelectSQL_WithQueryExpression_MatchesCurrentSnapshot()
    {
        QueryExpression expression = new StatementAnalyst("Id==1").GetQueryExpression();
        string actual = CreateGenerator().GetSelectSQL(typeof(TestEntity), expression);
        Assert.AreEqual("select * from TestEntity where (Id=1)", actual);
    }

    [TestMethod]
    public void GetInsertSQL_MatchesCurrentSnapshot()
    {
        // TODO(6.3/7.9): 修复 13.7（最后一个属性被跳过时残留尾随分隔符）后更新此快照。
        var entity = new TestEntity { Id = 1, Name = null, Created = new DateTime(2024, 1, 2, 3, 4, 5), Score = 1.5, Children = null };
        string actual = CreateGenerator().GetInsertSQL(entity);
        Assert.AreEqual("insert into TestEntity(Id,Name,Created,Score,) values(1,NULL,'2024-01-02 03:04:05',1.5,)", actual);
    }

    [TestMethod]
    public void GetUpdateSQL_MatchesCurrentSnapshot()
    {
        var oldObject = new TestEntity { Id = 1, Name = null, Created = new DateTime(2024, 1, 2, 3, 4, 5), Score = 1.5, Children = null };
        var newObject = new TestEntity { Id = 2, Name = "abc", Created = new DateTime(2025, 6, 7, 8, 9, 10), Score = 2.5, Children = null };
        string actual = CreateGenerator().GetUpdateSQL(oldObject, newObject);
        Assert.AreEqual("update TestEntity set Id=2,Name='abc',Created='2025-06-07 08:09:10',Score=2.5 where (Id=1 and Name is null and Created='2024-01-02 03:04:05' and Score=1.5)", actual);
    }

    [TestMethod]
    public void GetDeleteSQL_MatchesCurrentSnapshot()
    {
        // TODO(7.9): 修复 13.7（尾随分隔符）后更新此快照。
        var entity = new TestEntity { Id = 1, Name = null, Created = new DateTime(2024, 1, 2, 3, 4, 5), Score = 1.5, Children = null };
        string actual = CreateGenerator().GetDeleteSQL(entity);
        Assert.AreEqual("delete from TestEntity where (Id=1 and Name is null and Created='2024-01-02 03:04:05' and Score=1.5 and )", actual);
    }
}
