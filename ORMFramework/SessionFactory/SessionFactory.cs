using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework;
public class SessionFactory : ISessionFactory
{
    private IDbDriverFactory _driverFactory;
    private Dictionary<string, EntityMapping> _mappings;

    public Configuration.Configuration Configuration
    {
        get; private set;
    }

    public void Initialize()
    {
        Initialize(string.Empty);
    }

    public void Initialize(string configFilePath)
    {
        if (_driverFactory != null)
        {
            return;
        }
        _mappings = new Dictionary<string, EntityMapping>();
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
        foreach (EntityMapping map in Configuration.Mappings)
        {
            _mappings.Add(map.ClassName, map);
        }
        LogHelper.LogHelper.InitLog(configFilePath);
    }

    public EntityMapping GetEntityMappingByClassName(string className) => _mappings[className];

    public ISession CreateSession()
    {
        var persistenceContext = new PersistenceContext(_driverFactory, _mappings);
        persistenceContext.SQLGenerator = new SQLGenerator();
        persistenceContext.SQLGenerator.PersistenceContext = persistenceContext;
        return new Session(this, persistenceContext);
    }
}