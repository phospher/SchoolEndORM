namespace ORMFramework.Cache;
public class GlobalCacheResult
{
    public Guid ObjectId { get; set; }

    public object Value { get; set; }

    public long Version { get; set; }

    public Dictionary<string, object> ForeignKeys { get; set; }

    public GlobalCacheResult(Guid objectId, object Value, long version)
    {
        ObjectId = objectId;
        this.Value = Value;
        Version = version;
        ForeignKeys = new Dictionary<string, object>();
    }
}