using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Cache;
using ORMFramework.Configuration;
using ORMFramework.SQL;
using ORMFramework.Test.Fakes;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class SessionTest
{
    [TestMethod]
    public void Commit_DisposesSelfCreatedConnection_AfterUsingBlockExits()
    {
        var driverFactory = new FakeDbDriverFactory();
        FakeDbConnection connection = driverFactory.Connection;
        var persistenceContext = new FakePersistenceContext { DbDriverFactory = driverFactory };
        var sessionCache = new SessionCache { PersistenceContext = persistenceContext };

        sessionCache.Commit();

        Assert.IsTrue(connection.IsDisposed, "self-created connection should be disposed by the using declaration");
    }

    [TestMethod]
    public void ExecuteNonQuery_TwoConsecutiveCalls_SharedConnectionIsNotDisposed()
    {
        var connection = new FakeDbConnection();
        var persistenceContext = new FakePersistenceContext { Connection = connection };
        var session = new Session(persistenceContext);

        session.ExecuteNonQuery("delete from Student");
        session.ExecuteNonQuery("update Student set Name='x'");

        Assert.IsFalse(connection.IsDisposed, "shared connection must not be disposed by OnExecute");
        Assert.AreEqual(2, connection.OpenCount, "the shared connection should be reopened for each execution");
    }

    [TestMethod]
    public void GlobalCacheSearch_ReadDataFromDatabase_DoesNotDisposeSharedConnection()
    {
        var connection = new FakeDbConnection();
        var driverFactory = new FakeDbDriverFactory { Connection = connection };
        var persistenceContext = new FakePersistenceContext { Connection = connection, DbDriverFactory = driverFactory };
        Type entityType = typeof(SharedConnectionProbe);
        persistenceContext.Mappings[entityType.FullName] = new EntityMapping { ClassName = entityType.FullName, TableName = "SharedConnectionProbe" };
        persistenceContext.SQLGenerator = new SQLGenerator { PersistenceContext = persistenceContext };

        GlobalCache.Search(entityType, null, persistenceContext, new List<Guid>());

        Assert.IsFalse(connection.IsDisposed, "shared connection must not be disposed by ReadDataFromDatabase");
    }

    private sealed class SharedConnectionProbe
    {
        public int Id { get; set; }
    }
}
