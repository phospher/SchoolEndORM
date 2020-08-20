using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ORMFramework.Cache
{
    public class GlobalCacheEntity : CacheEntity
    {
        private Lock _lock;
        private int _readCount;
        private Queue<Thread> _blockThreads;
        private bool _isDeleted;
        private int _writeLockThreadId;
        private int _referenceCount;

        public Lock Lock
        {
            get { return _lock; }
            set { _lock = value; }
        }

        public int ReadCount
        {
            get { return _readCount; }
            set { _readCount = value; }
        }

        public Queue<Thread> BlockThreads
        {
            get { return _blockThreads; }
        }

        public bool IsDeleted
        {
            get { return _isDeleted; }
            set { _isDeleted = value; }
        }

        public int WriteLockThreadId
        {
            get { return _writeLockThreadId; }
            set { _writeLockThreadId = value; }
        }

        public int ReferenceCount
        {
            get { return _referenceCount; }
            set { _referenceCount = value; }
        }

        public GlobalCacheEntity()
        {
            _readCount = 0;
            _lock = Lock.None;
            _isDeleted = false;
            _blockThreads = new Queue<Thread>();
        }

        public GlobalCacheEntity(object value)
        {
            Value = value;
            Version = DateTime.UtcNow.Ticks;
            _readCount = 0;
            _lock = Lock.None;
            _blockThreads = new Queue<Thread>();
            ForeignKeys = new Dictionary<string, object>();
            ObjectId = Guid.NewGuid();
            _isDeleted = false;
            _writeLockThreadId = -1;
            _referenceCount = 0;
        }
    }

    public enum Lock
    {
        None,
        ReadLock,
        WriteLock
    }
}