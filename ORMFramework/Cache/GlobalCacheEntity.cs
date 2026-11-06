namespace ORMFramework.Cache;

public class GlobalCacheEntity : CacheEntity
{
    public Lock Lock { get; set; }

    public int ReadCount { get; set; }

    public Queue<Thread> BlockThreads { get; } = new();

    public bool IsDeleted { get; set; }

    public int WriteLockThreadId { get; set; }

    public int ReferenceCount { get; set; }

    public GlobalCacheEntity()
    {
        ReadCount = 0;
        Lock = Lock.None;
        IsDeleted = false;
    }

    public GlobalCacheEntity(object value)
    {
        Value = value;
        Version = DateTime.UtcNow.Ticks;
        ReadCount = 0;
        Lock = Lock.None;
        ForeignKeys = new Dictionary<string, object>();
        ObjectId = Guid.NewGuid();
        IsDeleted = false;
        WriteLockThreadId = -1;
        ReferenceCount = 0;
    }
}

public enum Lock
{
    None,
    ReadLock,
    WriteLock
}