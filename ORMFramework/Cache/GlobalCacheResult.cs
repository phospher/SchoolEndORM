namespace ORMFramework.Cache;

public class GlobalCacheResult(Guid objectId, object value, long version)
{
    public Guid ObjectId { get; set; } = objectId;

    public object Value { get; set; } = value;

    public long Version { get; set; } = version;

    public Dictionary<string, object> ForeignKeys { get; set; } = [];
}