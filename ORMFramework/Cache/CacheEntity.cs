namespace ORMFramework.Cache;
public class CacheEntity
{
    public Guid ObjectId { get; set; }

    public object Value { get; set; } = null!;

    public long Version { get; set; }

    public Dictionary<string, object> ForeignKeys { get; set; } = [];
}