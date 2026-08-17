using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Cache;
using ORMFramework.Configuration;
using ORMFramework.Exceptions;
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
        FieldInfo objListField = typeof(SessionCache).GetField("_objList", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var objList = (Dictionary<object, CacheEntity>)objListField.GetValue(sessionCache)!;
        objList.Add(entity1.Value, entity1);
        objList.Add(entity2.Value, entity2);

        sessionCache.Dispose();
    }

    [TestMethod]
    public void Dispose_CalledTwice_ReleasesGlobalCacheReferenceExactlyOnce_AndDoesNotThrow()
    {
        var objectId = Guid.NewGuid();
        var entity = new CacheEntity { ObjectId = objectId, Value = new TestEntity() };
        GlobalCache.Insert(entity);

        var sessionCache = new SessionCache();
        FieldInfo objListField = typeof(SessionCache).GetField("_objList", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var objList = (Dictionary<object, CacheEntity>)objListField.GetValue(sessionCache)!;
        objList.Add(entity.Value, entity);

        sessionCache.Dispose();
        sessionCache.Dispose();

        FieldInfo globalObjListField = typeof(GlobalCache).GetField("_objList", BindingFlags.NonPublic | BindingFlags.Static)!;
        var globalObjList = (Dictionary<Guid, GlobalCacheEntity>)globalObjListField.GetValue(null)!;
        Assert.IsTrue(globalObjList.TryGetValue(objectId, out var globalEntity), "entity should still be present (reference count > 0 prevents collection)");
        Assert.AreEqual(0, globalEntity!.ReferenceCount, "disposing twice must release the global cache reference exactly once");
    }

    [TestMethod]
    public void GetObjectsFromGlobalCache_WithNullIgnoreIds_DoesNotThrowNullReference()
    {
        var persistenceContext = new FakePersistenceContext();
        Type entityType = typeof(TestEntity);
        var mapping = new EntityMapping { ClassName = entityType.FullName!, TableName = "TestEntity" };
        persistenceContext.Mappings[entityType.FullName!] = mapping;
        persistenceContext.SQLGenerator = new SQLGenerator { PersistenceContext = persistenceContext };

        var sessionCache = new SessionCache { PersistenceContext = persistenceContext };

        var deleteEntity = new CacheEntity { ObjectId = Guid.NewGuid(), Value = new TestEntity() };
        FieldInfo deleteListField = typeof(SessionCache).GetField("_deleteList", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var deleteList = (List<CacheEntity>)deleteListField.GetValue(sessionCache)!;
        deleteList.Add(deleteEntity);

        MethodInfo method = typeof(SessionCache).GetMethod("GetObjectsFromGlobalCache", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Exception? inner = null;
        try
        {
            method.Invoke(sessionCache, new object?[] { entityType, null, null });
        }
        catch (TargetInvocationException ex)
        {
            inner = ex.InnerException;
        }

        Assert.IsNotNull(inner, "expected GetObjectsFromGlobalCache to throw (no database in unit test)");
        Assert.IsFalse(inner is NullReferenceException, "NullReferenceException should not be thrown, got: " + inner);
    }

    [TestMethod]
    public void GetReferenceClasses_MappingReferencesUnknownProperty_ThrowsOrmConfigurationException()
    {
        Type entityType = typeof(TestEntity);
        var persistenceContext = new FakePersistenceContext();
        var mapping = new EntityMapping { ClassName = entityType.FullName!, TableName = "TestEntity" };
        mapping.Relations["MissingProp"] = new EntityRelation
        {
            KeyColum = "Id",
            Property = "MissingProp",
            ReferenceClassName = "SomeClass",
            ReferenceColum = "Id",
            Type = RelationType.OneToOne,
        };
        persistenceContext.Mappings[entityType.FullName!] = mapping;

        var sourceEntity = new TestEntity { Id = 1 };
        var cacheEntity = new CacheEntity
        {
            ObjectId = Guid.NewGuid(),
            Value = sourceEntity,
            ForeignKeys = new Dictionary<string, object> { ["Id"] = 1 },
        };

        var sessionCache = new SessionCache { PersistenceContext = persistenceContext };
        FieldInfo objListField = typeof(SessionCache).GetField("_objList", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var objList = (Dictionary<object, CacheEntity>)objListField.GetValue(sessionCache)!;
        objList.Add(sourceEntity, cacheEntity);

        var ex = Assert.ThrowsException<OrmConfigurationException>(() => sessionCache.GetReferenceClasses(sourceEntity));
        StringAssert.Contains(ex.Message, "MissingProp");
        StringAssert.Contains(ex.Message, entityType.FullName!);
    }
}
