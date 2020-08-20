using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Cache
{
    public class GlobalCacheResult
    {
        private Guid _objectId;
        private object _value;
        private Dictionary<string, object> _foreignKeys;
        private long _version;

        public Guid ObjectId
        {
            get { return _objectId; }
            set { _objectId = value; }
        }

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public long Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public Dictionary<string, object> ForeignKeys
        {
            get { return _foreignKeys; }
            set { _foreignKeys = value; }
        }

        public GlobalCacheResult(Guid objectId, object Value, long version)
        {
            _objectId = objectId;
            _value = Value;
            _version = version;
            _foreignKeys = new Dictionary<string, object>();
        }
    }
}