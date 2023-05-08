using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Cache
{
    public class GlobalCacheResult
    {
        public Guid ObjectId { get; set; }

        public object Value { get; set; }

        public long Version { get; set; }

        public Dictionary<string, object> ForeignKeys { get; set; }

        public GlobalCacheResult(Guid objectId, object Value, long version)
        {
            this.ObjectId = objectId;
            this.Value = Value;
            this.Version = version;
            this.ForeignKeys = new Dictionary<string, object>();
        }
    }
}