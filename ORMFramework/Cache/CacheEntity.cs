using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Cache
{
    public class CacheEntity
    {
        public Guid ObjectId { get; set; }

        public object Value { get; set; }

        public long Version { get; set; }

        public Dictionary<string, object> ForeignKeys { get; set; }
    }
}