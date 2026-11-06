using System.Data;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework.Test.Fakes;

public class FakePersistenceContext : IPersistenceContext
{
    public IDbDriverFactory DbDriverFactory
    {
        get => field ?? throw new InvalidOperationException("no database in unit test");
        set => field = value;
    } = null!;

    public IDbConnection Connection
    {
        get => field ?? throw new InvalidOperationException("no database in unit test");
        set => field = value;
    } = null!;

    public IDictionary<string, EntityMapping> Mappings { get; set; } = new Dictionary<string, EntityMapping>();

    public ISQLGenerator SQLGenerator { get; set; } = null!;

    public EntityMapping? GetEntityMappingByClassName(string className)
    {
        return Mappings.TryGetValue(className, out var mapping) ? mapping : null;
    }
}
