using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class EntityMapping
    {
        private IEnumerable<string> _keys;
        private IDictionary<string, EntityRelation> _relations;

        public string ClassName { get; set; }

        public string TableName { get; set; }

        public IEnumerable<string> Keys
        {
            get
            {
                if (_keys == null)
                {
                    _keys = new List<string>();
                }
                return _keys;
            }
            set { _keys = value; }
        }

        public IDictionary<string, EntityRelation> Relations
        {
            get
            {
                if (_relations == null)
                {
                    _relations = new Dictionary<string, EntityRelation>();
                }
                return _relations;
            }
            set { _relations = value; }
        }
    }
}