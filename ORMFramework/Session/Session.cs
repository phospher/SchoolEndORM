using System.Data;
using ORMFramework.Cache;
using ORMFramework.Event;
using ORMFramework.Listener;
using ORMFramework.Statment;

namespace ORMFramework;

public sealed class Session : ISession, IDisposable
{
    private ISessionCache _sessionCache;

    private bool _disposed;

    public ISessionFactory? SessionFactory { get; set; }

    public ISelectListener SelectListener { get; }

    public List<IInsertListener> InsertListeners { get; } = new();

    public List<IDeleteListener> DeleteListeners { get; } = new();

    public List<IUpdateListener> UpdateListeners { get; } = new();

    public List<ISubmitListener> SubmitListeners { get; } = new();

    public ICommandListener CommandListener { get; }

    public IDbConnection Connection => PersistenceContext.Connection;

    public IPersistenceContext PersistenceContext { get; }


    public Session(IPersistenceContext persistenceContext) : this(null, persistenceContext)
    {

    }

    public Session(ISessionFactory? sessionFactory, IPersistenceContext persistenceContext) : this(sessionFactory, persistenceContext, new SessionCache(), new DefaultSelectListener(),
        new DefaultInsertListener(), new DefaultUpdateListener(), new DefaultDeleteListener(), new DefaultSubmitListener(), new DefaultCommandListener())
    {

    }

    public Session(ISessionFactory? sessionFactory, IPersistenceContext persistenceContext, ISessionCache sessionCache, ISelectListener selectListener, IInsertListener insertListener,
        IUpdateListener updateListener, IDeleteListener deleteListener, ISubmitListener submitListener, ICommandListener commandListener)
    {
        SessionFactory = sessionFactory;
        PersistenceContext = persistenceContext;
        _sessionCache = sessionCache;
        _sessionCache.PersistenceContext = persistenceContext;
        SelectListener = selectListener;
        InsertListeners.Add(insertListener);
        UpdateListeners.Add(updateListener);
        DeleteListeners.Add(deleteListener);
        SubmitListeners.Add(submitListener);
        CommandListener = commandListener;
    }

    public object[] Search(Type objType, string selectCommand) => OnSearch(objType, selectCommand).ToArray();

    public T[] Search<T>(string selectCommand) => OnSearch(typeof(T), selectCommand).Cast<T>().ToArray();

    public void Update(object @object)
    {
        var @event = new UpdateEvent();
        @event.Cache = _sessionCache;
        @event.Entity = @object;
        foreach (IUpdateListener listener in UpdateListeners)
        {
            listener.OnUpdate(@event, this);
        }
    }

    public void Delete(object @object)
    {
        var @event = new UpdateEvent();
        @event.Cache = _sessionCache;
        @event.Entity = @object;
        foreach (IDeleteListener listener in DeleteListeners)
        {
            listener.OnDelete(@event, this);
        }
    }

    public void Insert(object @object)
    {
        var @event = new UpdateEvent();
        @event.Cache = _sessionCache;
        @event.Entity = @object;
        foreach (IInsertListener listener in InsertListeners)
        {
            listener.OnInsert(@event, this);
        }
    }

    public void Submit()
    {
        var @event = new ActionEvent();
        @event.Cache = _sessionCache;
        foreach (ISubmitListener listener in SubmitListeners)
        {
            listener.OnSubmit(@event, this);
        }
    }

    public int ExecuteNonQuery(string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        var @event = new CommandEvent();
        @event.CommandText = commandText;
        @event.PersistenceContext = PersistenceContext;
        @event.IsQuery = false;
        CommandListener.OnExecute(@event, this);
        return (int)@event.Result!;
    }

    public DataSet ExecuteQuery(string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        var @event = new CommandEvent();
        @event.CommandText = commandText;
        @event.PersistenceContext = PersistenceContext;
        @event.IsQuery = true;
        CommandListener.OnExecute(@event, this);
        return (DataSet)@event.Result!;
    }

    private IEnumerable<object> OnSearch(Type objType, string selectCommand)
    {
        QueryExpression expression = new StatementAnalyst(selectCommand).GetQueryExpression();
        var @event = new SelectEvent();
        @event.Cache = _sessionCache;
        @event.QueryExpression = expression;
        @event.SearchType = objType;
        SelectListener.OnSelect(@event, this);
        return (IEnumerable<object>)@event.Result!;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _sessionCache.Dispose();
    }
}