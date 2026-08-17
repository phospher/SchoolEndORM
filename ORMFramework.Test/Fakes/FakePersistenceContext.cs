using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework.Test.Fakes;
public class FakePersistenceContext : IPersistenceContext
{
    private IDictionary<string, EntityMapping> _mappings = new Dictionary<string, EntityMapping>();
    private IDbConnection _connection = null!;
    private IDbDriverFactory _dbDriverFactory = null!;

    public IDbDriverFactory DbDriverFactory
    {
        get { return _dbDriverFactory ?? throw new InvalidOperationException("no database in unit test"); }
        set { _dbDriverFactory = value; }
    }

    public IDbConnection Connection
    {
        get { return _connection ?? throw new InvalidOperationException("no database in unit test"); }
        set { _connection = value; }
    }

    public IDictionary<string, EntityMapping> Mappings
    {
        get { return _mappings; }
        set { _mappings = value; }
    }

    public ISQLGenerator SQLGenerator { get; set; } = null!;

    public EntityMapping? GetEntityMappingByClassName(string className)
    {
        return _mappings.TryGetValue(className, out var mapping) ? mapping : null;
    }
}
