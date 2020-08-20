using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ORMFramework.Configuration;

namespace ORMFramework
{
    public class PersistenceContext : IPersistenceContext
    {
        private IDbDriverFactory _driverFactory;
        private IDbConnection _connection;
        private Dictionary<string, EntityMapping> _mappings;

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    _connection = _driverFactory.GetDbConnection();
                }
                return _connection;
            }
        }

        public IDbDriverFactory DbDriverFactory
        {
            get { return _driverFactory; }
        }

        public PersistenceContext(IDbDriverFactory driverFactory, Dictionary<string, EntityMapping> mappings)
        {
            _driverFactory = driverFactory;
            _mappings = mappings;
        }

        public EntityMapping GetEntityMappingByClassName(string className)
        {
            if (_mappings != null && _mappings.ContainsKey(className))
            {
                return _mappings[className];
            }
            else
            {
                return null;
            }
        }
    }
}