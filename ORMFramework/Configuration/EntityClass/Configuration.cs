namespace ORMFramework.Configuration;
public class Configuration
{
    private readonly IList<EntityMapping> _mappings = new List<EntityMapping>();

    public string ConnectionString { get; set; }

    public string ProviderName { get; set; }

    public IList<EntityMapping> Mappings
    {
        get
        {
            return _mappings;
        }
    }
}