using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework.Test.Fakes;

public class FakePersistenceContext : IPersistenceContext
{
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

    public IDictionary<string, EntityMapping> Mappings { get; set; } = new Dictionary<string, EntityMapping>();

    public ISQLGenerator SQLGenerator { get; set; } = null!;

    public EntityMapping? GetEntityMappingByClassName(string className)
    {
        return Mappings.TryGetValue(className, out var mapping) ? mapping : null;
    }
}
