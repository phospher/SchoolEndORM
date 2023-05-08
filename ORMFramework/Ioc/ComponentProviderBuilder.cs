using System;
using System.Collections.Generic;

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

