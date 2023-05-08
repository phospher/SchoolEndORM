using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework
{
    public class PersistenceContext : IPersistenceContext
    {
        private IDbDriverFactory _driverFactory;
        private IDbConnection _connection;

        public IDictionary<string, EntityMapping> Mappings { get; set; }

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

        public ISQLGenerator SQLGenerator { get; set; }

        public IDbDriverFactory DbDriverFactory
        {
            get { return _driverFactory; }
        }

        public PersistenceContext(IDbDriverFactory driverFactory, ISQLGenerator sqlGenerator)
        {
            this._driverFactory = driverFactory;
            this.SQLGenerator = sqlGenerator;
        }

        public PersistenceContext(IDbDriverFactory driverFactory, IDictionary<string, EntityMapping> mappings)
        {
            this._driverFactory = driverFactory;
            this.Mappings = mappings;
        }

        public EntityMapping GetEntityMappingByClassName(string className)
        {
            if (this.Mappings != null && this.Mappings.ContainsKey(className))
            {
                return this.Mappings[className];
            }
            else
            {
                return null;
            }
        }
    }
}