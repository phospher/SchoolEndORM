namespace ORMFramework.Configuration;

public class Configuration
{
    public required string ConnectionString { get; set; }

    public required string ProviderName { get; set; }

    public IList<EntityMapping> Mappings { get; } = [];
}