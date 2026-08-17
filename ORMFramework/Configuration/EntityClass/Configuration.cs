namespace ORMFramework.Configuration;
public class Configuration
{
    public string ConnectionString { get; set; }

    public string ProviderName { get; set; }

    public IList<EntityMapping> Mappings { get; } = [];
}