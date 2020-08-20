using System;
using System.Collections.Generic;
using System.Data;
using ORMFramework.Listener;

namespace ORMFramework
{
    public interface ISession : IDisposable
    {
        IDbConnection Connection { get; }
        SessionFactory SessionFactory { get; }
        IPersistenceContext PersistenceContext { get; }
        ISelectListener SelectListener { get; }
        List<IUpdateListener> UpdateListeners { get; }
        List<IInsertListener> InsertListeners { get; }
        List<IDeleteListener> DeleteListeners { get; }
        List<ISubmitListener> SubmitListeners { get; }
        ICommandListener CommandListener { get; }
        T[] Search<T>(string selectCommand);
        object[] Search(Type objType, string selectCommand);
        void Insert(object @object);
        void Delete(object @object);
        void Update(object @object);
        int ExecuteNonQuery(string commandText);
        DataSet ExecuteQuery(string commandText);
        void Submit();
    }
}