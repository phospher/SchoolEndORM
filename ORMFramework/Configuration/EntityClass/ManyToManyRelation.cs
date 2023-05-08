using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class ManyToManyRelation : EntityRelation
    {
        public string ReferenceTableName { get; set; }

        public string ReferenceClassKeyColum { get; set; }

        public string ReferenceClassColum { get; set; }
    }
}