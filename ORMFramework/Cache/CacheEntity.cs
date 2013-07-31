using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Cache
{
    public class CacheEntity
    {
        private Guid _objectId;
        private object _value;
        private long _version;
        private Dictionary<string, object> _foreignKeys;

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
    }
}
