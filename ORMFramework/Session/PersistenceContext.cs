using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework;
public class PersistenceContext : IPersistenceContext
{
    private IDbDriverFactory _driverFactory;
    private IDbConnection _connection;

    public IDictionary<string, EntityMapping> Mappings { get; set; }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State == ConnectionState.Closed)
            {
                _connection = _driverFactory.GetDbConnection();
            }
            return _connection;
        }
    }

    public ISQLGenerator SQLGenerator { get; set; }

    public IDbDriverFactory DbDriverFactory
    {
        get { return _driverFactory; }
    }

    public PersistenceContext(IDbDriverFactory driverFactory, ISQLGenerator sqlGenerator)
    {
        _driverFactory = driverFactory;
        SQLGenerator = sqlGenerator;
    }

    public PersistenceContext(IDbDriverFactory driverFactory, IDictionary<string, EntityMapping> mappings)
    {
        _driverFactory = driverFactory;
        Mappings = mappings;
    }

    public EntityMapping GetEntityMappingByClassName(string className)
    {
        if (Mappings != null && Mappings.ContainsKey(className))
        {
            return Mappings[className];
        }
        else
        {
            return null;
        }
    }
}