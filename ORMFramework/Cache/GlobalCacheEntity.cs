namespace ORMFramework.Cache;
public class GlobalCacheEntity : CacheEntity
{
    private Queue<Thread> _blockThreads;

    public Lock Lock { get; set; }

    public int ReadCount { get; set; }

    public Queue<Thread> BlockThreads
    {
        get { return _blockThreads; }
    }

    public bool IsDeleted { get; set; }

    public int WriteLockThreadId { get; set; }

    public int ReferenceCount { get; set; }

    public GlobalCacheEntity()
    {
        ReadCount = 0;
        Lock = Lock.None;
        IsDeleted = false;
        _blockThreads = new Queue<Thread>();
    }

    public GlobalCacheEntity(object value)
    {
        Value = value;
        Version = DateTime.UtcNow.Ticks;
        ReadCount = 0;
        Lock = Lock.None;
        _blockThreads = new Queue<Thread>();
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