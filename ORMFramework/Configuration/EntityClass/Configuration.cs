using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class Configuration
    {
        private DatabaseType _databaseType;
        private string _connectionString;
        private IList<EntityMapping> _mappings;

        public DatabaseType DatabaseType
        {
            get { return _databaseType; }
            set { _databaseType = value; }
        }

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public IList<EntityMapping> Mappings
        {
            get
            {
                if (_mappings == null)
                {
                    _mappings = new List<EntityMapping>();
                }
                return _mappings;
            }
        }
    }
}