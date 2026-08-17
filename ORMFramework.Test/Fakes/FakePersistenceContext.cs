using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework.Test.Fakes;
public class FakePersistenceContext : IPersistenceContext
{
    private IDictionary<string, EntityMapping> _mappings = new Dictionary<string, EntityMapping>();
    private IDbConnection _connection;
    private IDbDriverFactory _dbDriverFactory;

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

    public ISQLGenerator SQLGenerator { get; set; }

    public EntityMapping GetEntityMappingByClassName(string className)
    {
        if (_mappings.TryGetValue(className, out EntityMapping mapping))
        {
            return mapping;
        }
        return null;
    }
}
