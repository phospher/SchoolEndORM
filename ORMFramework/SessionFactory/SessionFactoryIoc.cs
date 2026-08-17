using ORMFramework.Configuration;
using ORMFramework.Ioc;

namespace ORMFramework;
public sealed class SessionFactoryIoc : ISessionFactory
{
    private IDbDriverFactory _driverFactory;
    private Dictionary<string, EntityMapping> _mappings;
    private IComponentProvider _componentProvider;

    public Configuration.Configuration Configuration
    {
        get; private set;
    }

    public void Initialize() => Initialize(string.Empty);

    public void Initialize(string configFilePath) => Initialize(configFilePath, new ComponentProviderBuilder().Build());

    public void Initialize(string configFilePath, IComponentProvider componentProvider)
    {
        _componentProvider = componentProvider;
        if (_driverFactory != null)
        {
            return;
        }
        if (string.IsNullOrEmpty(configFilePath))
        {
            Configuration = new ConfigManager().GetSystemConfiguration();
        }
        else
        {
            Configuration = new ConfigManager(configFilePath).GetSystemConfiguration();
        }
        _driverFactory = new DefaultDbDriverFactory(Configuration.ConnectionString,
            Configuration.ProviderName);
        _mappings = Configuration.Mappings.ToDictionary(m => m.ClassName);
        LogHelper.LogHelper.InitLog(configFilePath);
    }

    public EntityMapping GetEntityMappingByClassName(string className)
    {
        return _mappings[className];
    }

    public ISession CreateSession()
    {
        ISession session = _componentProvider.GetComponent<ISession>();
        session.SessionFactory = this;
        session.PersistenceContext.DbDriverFactory.ConnectionString = Configuration.ConnectionString;
        session.PersistenceContext.DbDriverFactory.SetProviderName(Configuration.ProviderName);
        session.PersistenceContext.Mappings = _mappings;
        session.PersistenceContext.SQLGenerator.PersistenceContext = session.PersistenceContext;

        return session;
    }
}

