using Microsoft.Extensions.DependencyInjection;
using ORMFramework.SQL;
using ORMFramework.Cache;
using ORMFramework.Listener;

namespace ORMFramework.Ioc;
public sealed class DefaultComponentProvider(IDictionary<Type, Type> componentImplDict) : IComponentProvider
{
    private static readonly Dictionary<Type, ServiceLifetime> _componentLifetimes = new()
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

    private readonly ServiceProvider _serviceProvider = BuildServiceProvider(componentImplDict);

    private static ServiceProvider BuildServiceProvider(IDictionary<Type, Type> componentImplDict)
    {
        var services = new ServiceCollection();

        foreach (KeyValuePair<Type, Type> componentImpl in componentImplDict)
        {
            switch (_componentLifetimes[componentImpl.Key])
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

        return services.BuildServiceProvider();
    }

    public T GetComponent<T>() => _serviceProvider.GetService<T>();
}

