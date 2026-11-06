using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework;

public sealed class SessionFactory : ISessionFactory
{
    private IDbDriverFactory _driverFactory = null!;
    private Dictionary<string, EntityMapping> _mappings = null!;

    public Configuration.Configuration Configuration
    {
        get => field;
        private set => field = value;
    } = null!;

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

    public EntityMapping GetEntityMappingByClassName(string className) => _mappings[className];

    public ISession CreateSession()
    {
        var persistenceContext = new PersistenceContext(_driverFactory, _mappings);
        persistenceContext.SQLGenerator = new SQLGenerator();
        persistenceContext.SQLGenerator.PersistenceContext = persistenceContext;
        return new Session(this, persistenceContext);
    }
}