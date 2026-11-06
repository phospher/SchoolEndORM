namespace ORMFramework.Cache;

public class CacheEntity
{
    public Guid ObjectId { get; set; }

    public object Value
    {
        get => field;
        set => field = value;
    } = null!;

    public long Version { get; set; }

    public Dictionary<string, object> ForeignKeys
    {
        get => field;
        set => field = value;
    } = [];
}