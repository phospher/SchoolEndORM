using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Cache;
using ORMFramework.Configuration;
using ORMFramework.SQL;
using ORMFramework.Test.Fakes;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class SessionCacheTest
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
    }

    [TestMethod]
    public void Dispose_WithMultipleCachedObjects_DoesNotThrow()
    {
        var entity1 = new CacheEntity { ObjectId = Guid.NewGuid(), Value = new TestEntity() };
        var entity2 = new CacheEntity { ObjectId = Guid.NewGuid(), Value = new TestEntity() };

        GlobalCache.Insert(entity1);
        GlobalCache.Insert(entity2);

        var sessionCache = new SessionCache();
        FieldInfo objListField = typeof(SessionCache).GetField("_objList", BindingFlags.NonPublic | BindingFlags.Instance);
        var objList = (Dictionary<object, CacheEntity>)objListField.GetValue(sessionCache);
        objList.Add(entity1.Value, entity1);
        objList.Add(entity2.Value, entity2);

        sessionCache.Dispose();
    }

    [TestMethod]
    public void GetObjectsFromGlobalCache_WithNullIgnoreIds_DoesNotThrowNullReference()
    {
        var persistenceContext = new FakePersistenceContext();
        Type entityType = typeof(TestEntity);
        var mapping = new EntityMapping { ClassName = entityType.FullName, TableName = "TestEntity" };
        persistenceContext.Mappings[entityType.FullName] = mapping;
        persistenceContext.SQLGenerator = new SQLGenerator { PersistenceContext = persistenceContext };

        var sessionCache = new SessionCache { PersistenceContext = persistenceContext };

        var deleteEntity = new CacheEntity { ObjectId = Guid.NewGuid(), Value = new TestEntity() };
        FieldInfo deleteListField = typeof(SessionCache).GetField("_deleteList", BindingFlags.NonPublic | BindingFlags.Instance);
        var deleteList = (List<CacheEntity>)deleteListField.GetValue(sessionCache);
        deleteList.Add(deleteEntity);

        MethodInfo method = typeof(SessionCache).GetMethod("GetObjectsFromGlobalCache", BindingFlags.NonPublic | BindingFlags.Instance);

        Exception inner = null;
        try
        {
            method.Invoke(sessionCache, new object[] { entityType, null, null });
        }
        catch (TargetInvocationException ex)
        {
            inner = ex.InnerException;
        }

        Assert.IsNotNull(inner, "expected GetObjectsFromGlobalCache to throw (no database in unit test)");
        Assert.IsFalse(inner is NullReferenceException, "NullReferenceException should not be thrown, got: " + inner);
    }
}
