using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class EntityRelation
    {
        private string _keyColum;
        private string _Property;
        private string _referenceClassName;
        private string _referenceColum;
        private RelationType _type;

        public string KeyColum
        {
            get { return _keyColum; }
            set { _keyColum = value; }
        }

        public string Property
        {
            get { return _Property; }
            set { _Property = value; }
        }

        public string ReferenceClassName
        {
            get { return _referenceClassName; }
            set { _referenceClassName = value; }
        }

        public string ReferenceColum
        {
            get { return _referenceColum; }
            set { _referenceColum = value; }
        }

        public RelationType Type
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}