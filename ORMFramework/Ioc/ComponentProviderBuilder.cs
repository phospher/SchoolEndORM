using ORMFramework.Cache;
using ORMFramework.Listener;
using ORMFramework.SQL;

namespace ORMFramework.Ioc;

public sealed class ComponentProviderBuilder
{
    private readonly Dictionary<Type, Type> _componentImplTypes = new Dictionary<Type, Type>();

    public ComponentProviderBuilder()
    {
        _componentImplTypes[typeof(ISession)] = typeof(Session);
        _componentImplTypes[typeof(IDbDriverFactory)] = typeof(DefaultDbDriverFactory);
        _componentImplTypes[typeof(IPersistenceContext)] = typeof(PersistenceContext);
        _componentImplTypes[typeof(ISQLGenerator)] = typeof(SQLGenerator);
        _componentImplTypes[typeof(ISessionCache)] = typeof(SessionCache);
        _componentImplTypes[typeof(ISelectListener)] = typeof(DefaultSelectListener);
        _componentImplTypes[typeof(IInsertListener)] = typeof(DefaultInsertListener);
        _componentImplTypes[typeof(IUpdateListener)] = typeof(DefaultUpdateListener);
        _componentImplTypes[typeof(IDeleteListener)] = typeof(DefaultDeleteListener);
        _componentImplTypes[typeof(ICommandListener)] = typeof(DefaultCommandListener);
    }

    public ComponentProviderBuilder SetSession<T>() where T : ISession
    {
        _componentImplTypes[typeof(ISession)] = typeof(T);
        return this;
    }

    public ComponentProviderBuilder SetDbDriverFactory<T>() where T : IDbDriverFactory
    {
        _componentImplTypes[typeof(IDbDriverFactory)] = typeof(T);
        return this;
    }

    public IComponentProvider Build() => new DefaultComponentProvider(_componentImplTypes);
}

