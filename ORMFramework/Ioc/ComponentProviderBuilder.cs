using System;
using System.Collections.Generic;
using ORMFramework.Cache;
using ORMFramework.Listener;
using ORMFramework.SQL;

namespace ORMFramework.Ioc
{
    public class ComponentProviderBuilder
    {
        private readonly IDictionary<Type, Type> componentImplTypes = new Dictionary<Type, Type>();

        public ComponentProviderBuilder()
        {
            componentImplTypes[typeof(ISession)] = typeof(Session);
            componentImplTypes[typeof(IDbDriverFactory)] = typeof(DefaultDbDriverFactory);
            componentImplTypes[typeof(IPersistenceContext)] = typeof(PersistenceContext);
            componentImplTypes[typeof(ISQLGenerator)] = typeof(SQLGenerator);
            componentImplTypes[typeof(ISessionCache)] = typeof(SessionCache);
            componentImplTypes[typeof(ISelectListener)] = typeof(DefaultSelectListener);
            componentImplTypes[typeof(IInsertListener)] = typeof(DefaultInsertListener);
            componentImplTypes[typeof(IUpdateListener)] = typeof(DefaultUpdateListener);
            componentImplTypes[typeof(IDeleteListener)] = typeof(DefaultDeleteListener);
            componentImplTypes[typeof(ICommandListener)] = typeof(DefaultCommandListener);
        }

        public ComponentProviderBuilder SetSession<T>() where T : ISession
        {
            this.componentImplTypes[typeof(Session)] = typeof(T);
            return this;
        }

        public ComponentProviderBuilder SetDbDriverFactory<T>() where T : IDbDriverFactory
        {
            this.componentImplTypes[typeof(IDbDriverFactory)] = typeof(T);
            return this;
        }

        public IComponentProvider build()
        {
            return new DefaultComponentProvider(this.componentImplTypes);
        }
    }
}

