using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Ioc;
using ORMFramework.Listener;

namespace ORMFramework.Test;

[TestClass]
public class ComponentProviderTest
{
    [TestMethod]
    public void TestGetSession()
    {
        IComponentProvider componentProvider = new ComponentProviderBuilder()
            .Build();
        ISession session = componentProvider.GetComponent<ISession>();
        Assert.IsInstanceOfType<Session>(session);
    }

    [TestMethod]
    public void SetSessionOverridesSessionResolution()
    {
        IComponentProvider componentProvider = new ComponentProviderBuilder()
            .SetSession<CustomSession>()
            .Build();
        ISession session = componentProvider.GetComponent<ISession>();
        Assert.IsInstanceOfType<CustomSession>(session);
    }

    private sealed class CustomSession : ISession
    {
        public IDbConnection Connection => throw new NotImplementedException();
        public IPersistenceContext PersistenceContext => throw new NotImplementedException();
        public ISelectListener SelectListener => throw new NotImplementedException();
        public ISessionFactory? SessionFactory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IUpdateListener> UpdateListeners => throw new NotImplementedException();
        public List<IInsertListener> InsertListeners => throw new NotImplementedException();
        public List<IDeleteListener> DeleteListeners => throw new NotImplementedException();
        public List<ISubmitListener> SubmitListeners => throw new NotImplementedException();
        public ICommandListener CommandListener => throw new NotImplementedException();
        public T[] Search<T>(string selectCommand) => throw new NotImplementedException();
        public object[] Search(Type objType, string selectCommand) => throw new NotImplementedException();
        public void Insert(object @object) => throw new NotImplementedException();
        public void Delete(object @object) => throw new NotImplementedException();
        public void Update(object @object) => throw new NotImplementedException();
        public int ExecuteNonQuery(string commandText) => throw new NotImplementedException();
        public DataSet ExecuteQuery(string commandText) => throw new NotImplementedException();
        public void Submit() => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}
