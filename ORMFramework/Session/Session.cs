using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Listener;
using ORMFramework.Event;
using System.Data;
using ORMFramework.Statment;
using ORMFramework.Cache;

namespace ORMFramework
{
    public class Session : ISession, IDisposable
    {
        private SessionFactory _sessionFactory;
        private ISelectListener _selectListener;
        private List<IInsertListener> _insertListeners = new List<IInsertListener>();
        private List<IDeleteListener> _deleteListeners = new List<IDeleteListener>();
        private List<IUpdateListener> _updateListeners = new List<IUpdateListener>();
        private List<ISubmitListener> _submitListeners = new List<ISubmitListener>();
        private ICommandListener _commandListener;
        private IPersistenceContext _persistenceContext;
        private ISessionCache _sessionCache;

        public SessionFactory SessionFactory
        {
            get { return _sessionFactory; }
        }

        public ISelectListener SelectListener
        {
            get { return _selectListener; }
        }

        public List<IInsertListener> InsertListeners
        {
            get { return _insertListeners; }
        }

        public List<IDeleteListener> DeleteListeners
        {
            get { return _deleteListeners; }
        }

        public List<IUpdateListener> UpdateListeners
        {
            get { return _updateListeners; }
        }

        public List<ISubmitListener> SubmitListeners
        {
            get { return _submitListeners; }
        }

        public ICommandListener CommandListener
        {
            get { return _commandListener; }
        }

        public IDbConnection Connection
        {
            get { return _persistenceContext.Connection; }
        }

        public IPersistenceContext PersistenceContext
        {
            get { return _persistenceContext; }
        }

        internal Session(SessionFactory sessionFactory, IPersistenceContext persistenceContext)
        {
            _sessionFactory = sessionFactory;
            _persistenceContext = persistenceContext;
            _sessionCache = new SessionCache(_persistenceContext);
            _selectListener = new DefaultSelectListener();
            _insertListeners.Add(new DefaultInsertListener());
            _updateListeners.Add(new DefaultUpdateListener());
            _deleteListeners.Add(new DefaultDeleteListener());
            _submitListeners.Add(new DefaultSubmitListener());
            _commandListener = new DefaultCommandListener();
        }

        ~Session()
        {
            _sessionCache.Dispose();
        }

        public object[] Search(Type objType, string selectCommand)
        {
            List<object> result = new List<object>();
            foreach (object item in OnSearch(objType, selectCommand))
            {
                result.Add(item);
            }
            return result.ToArray();
        }

        public T[] Search<T>(string selectCommand)
        {
            List<T> result = new List<T>();
            foreach (object item in OnSearch(typeof(T), selectCommand))
            {
                result.Add((T)item);
            }
            return result.ToArray();
        }

        public void Update(object @object)
        {
            UpdateEvent @event = new UpdateEvent();
            @event.Cache = _sessionCache;
            @event.Entity = @object;
            foreach (IUpdateListener listener in _updateListeners)
            {
                listener.OnUpdate(@event, this);
            }
        }

        public void Delete(object @object)
        {
            UpdateEvent @event = new UpdateEvent();
            @event.Cache = _sessionCache;
            @event.Entity = @object;
            foreach (IDeleteListener listener in _deleteListeners)
            {
                listener.OnDelete(@event, this);
            }
        }

        public void Insert(object @object)
        {
            UpdateEvent @event = new UpdateEvent();
            @event.Cache = _sessionCache;
            @event.Entity = @object;
            foreach (IInsertListener listener in _insertListeners)
            {
                listener.OnInsert(@event, this);
            }
        }

        public void Submit()
        {
            ActionEvent @event = new ActionEvent();
            @event.Cache = _sessionCache;
            foreach (ISubmitListener listener in _submitListeners)
            {
                listener.OnSubmit(@event, this);
            }
        }

        public int ExecuteNonQuery(string commandText)
        {
            CommandEvent @event = new CommandEvent();
            @event.CommandText = commandText;
            @event.PersistenceContext = _persistenceContext;
            @event.IsQuery = false;
            _commandListener.OnExecute(@event, this);
            return (int)@event.Result;
        }

        public DataSet ExecuteQuery(string commandText)
        {
            CommandEvent @event = new CommandEvent();
            @event.CommandText = commandText;
            @event.PersistenceContext = _persistenceContext;
            @event.IsQuery = true;
            _commandListener.OnExecute(@event, this);
            return (DataSet)@event.Result;
        }

        private IEnumerable<object> OnSearch(Type objType, string selectCommand)
        {
            QueryExpression expression = new StatementAnalyst(selectCommand).GetQueryExpression();
            SelectEvent @event = new SelectEvent();
            @event.Cache = _sessionCache;
            @event.QueryExpression = expression;
            @event.SearchType = objType;
            _selectListener.OnSelect(@event, this);
            return (IEnumerable<object>)@event.Result;
        }

        public void Dispose()
        {
            _sessionCache.Dispose();
        }
    }
}
