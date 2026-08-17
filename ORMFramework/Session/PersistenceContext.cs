using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework;
public sealed class PersistenceContext : IPersistenceContext
{
    private IDbConnection? _connection;

    public IDictionary<string, EntityMapping> Mappings { get; set; } = new Dictionary<string, EntityMapping>();

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State == ConnectionState.Closed)
            {
                _connection = DbDriverFactory.GetDbConnection();
            }
            return _connection;
        }
    }

    public ISQLGenerator SQLGenerator { get; set; } = null!;

    public IDbDriverFactory DbDriverFactory { get; }

    public PersistenceContext(IDbDriverFactory driverFactory, ISQLGenerator sqlGenerator)
    {
        DbDriverFactory = driverFactory;
        SQLGenerator = sqlGenerator;
    }

    public PersistenceContext(IDbDriverFactory driverFactory, IDictionary<string, EntityMapping> mappings)
    {
        DbDriverFactory = driverFactory;
        Mappings = mappings;
    }

    public EntityMapping? GetEntityMappingByClassName(string className)
    {
        return Mappings != null && Mappings.TryGetValue(className, out var mapping) ? mapping : null;
    }
}