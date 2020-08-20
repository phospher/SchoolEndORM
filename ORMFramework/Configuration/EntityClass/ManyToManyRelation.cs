using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class ManyToManyRelation : EntityRelation
    {
        private string _referenceTableName;
        private string _referenceClassKeyColum;
        private string _referenceClassColum;

        public string ReferenceTableName
        {
            get { return _referenceTableName; }
            set { _referenceTableName = value; }
        }

        public string ReferenceClassKeyColum
        {
            get { return _referenceClassKeyColum; }
            set { _referenceClassKeyColum = value; }
        }

        public string ReferenceClassColum
        {
            get { return _referenceClassColum; }
            set { _referenceClassColum = value; }
        }
    }
}