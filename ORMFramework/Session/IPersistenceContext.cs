using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework;

public interface IPersistenceContext
{
    IDbDriverFactory DbDriverFactory { get; }
    IDbConnection Connection { get; }
    IDictionary<string, EntityMapping> Mappings { get; set; }
    ISQLGenerator SQLGenerator { get; set; }
    EntityMapping? GetEntityMappingByClassName(string className);
}