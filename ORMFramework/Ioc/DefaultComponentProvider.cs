using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ORMFramework.SQL;
using ORMFramework.Cache;
using ORMFramework.Listener;

namespace ORMFramework.Ioc
{
    public class DefaultComponentProvider : IComponentProvider
    {
        private readonly IDictionary<Type, Type> componentImplDict;

        private readonly IDictionary<Type, ServiceLifetime> componentLiftimeDict = new Dictionary<Type, ServiceLifetime>()
        {
            { typeof(ISession), ServiceLifetime.Transient },
            { typeof(IDbDriverFactory), ServiceLifetime.Transient },
            { typeof(IPersistenceContext), ServiceLifetime.Transient },
            { typeof(ISQLGenerator), ServiceLifetime.Transient },
            { typeof(ISessionCache), ServiceLifetime.Transient },
            { typeof(ISelectListener), ServiceLifetime.Singleton },
            { typeof(IInsertListener), ServiceLifetime.Singleton },
            { typeof(IUpdateListener), ServiceLifetime.Singleton },
            { typeof(IDeleteListener), ServiceLifetime.Singleton },
            { typeof(ICommandListener), ServiceLifetime.Singleton },
            { typeof(ISubmitListener), ServiceLifetime.Singleton }
        };

        private readonly ServiceProvider serviceProvider;

        public DefaultComponentProvider(IDictionary<Type, Type> componentImplDict)
        {
            this.componentImplDict = componentImplDict;

            ServiceCollection services = new ServiceCollection();

            foreach(KeyValuePair<Type,Type> componentImpl in this.componentImplDict)
            {
                switch (this.componentLiftimeDict[componentImpl.Key])
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(componentImpl.Key, componentImpl.Value);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(componentImpl.Key, componentImpl.Value);
                        break;
                    default:
                        services.AddTransient(componentImpl.Key, componentImpl.Value);
                        break;
                }
            }

            this.serviceProvider = services.BuildServiceProvider();
        }

        public T GetComponent<T>()
        {
            return this.serviceProvider.GetService<T>();
        }
    }
}

