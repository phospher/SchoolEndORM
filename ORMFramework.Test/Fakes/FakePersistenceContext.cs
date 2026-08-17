using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework.Test.Fakes;
public class FakePersistenceContext : IPersistenceContext
{
    private IDictionary<string, EntityMapping> _mappings = new Dictionary<string, EntityMapping>();

    public IDbDriverFactory DbDriverFactory => throw new InvalidOperationException("no database in unit test");

    public IDbConnection Connection => throw new InvalidOperationException("no database in unit test");

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
