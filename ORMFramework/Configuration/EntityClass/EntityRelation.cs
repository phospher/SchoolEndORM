using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class EntityRelation
    {
        public string KeyColum { get; set; }

        public string Property { get; set; }

        public string ReferenceClassName { get; set; }

        public string ReferenceColum { get; set; }

        public RelationType Type { get; set; }
    }
}