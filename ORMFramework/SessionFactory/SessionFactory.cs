using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.DbDriverFactory;
using ORMFramework.Cache;

namespace ORMFramework {
    public class SessionFactory {
        private IDbDriverFactory _driverFactory;
        private Dictionary<string, EntityMapping> _mappings;
        private Configuration.Configuration _configuration;

        public Configuration.Configuration Configuration {
            get { return _configuration; }
        }

        public void Initialize () {
            Initialize ( string.Empty );
        }

        public void Initialize ( string configFilePath ) {
            if ( _driverFactory != null ) {
                return;
            }
            _mappings = new Dictionary<string, EntityMapping> ();
            if ( string.IsNullOrEmpty ( configFilePath ) ) {
                _configuration = new ConfigManager ().GetSystemConfiguration ();
            } else {
                _configuration = new ConfigManager ( configFilePath ).GetSystemConfiguration ();
            }
            switch ( _configuration.DatabaseType ) {
                case DatabaseType.Odbc: _driverFactory = new OdbcDriverFactory ();
                    break;
                case DatabaseType.OleDb: _driverFactory = new OleDbDriverFactory ();
                    break;
                case DatabaseType.Oracle: _driverFactory = new OracleDriverFactory ();
                    break;
                case DatabaseType.SQLServer: _driverFactory = new SQLDriverFactory ();
                    break;
            }
            _driverFactory.ConnectionString = _configuration.ConnectionString;
            foreach ( EntityMapping map in _configuration.Mappings ) {
                _mappings.Add ( map.ClassName, map );
            }
            LogHelper.LogHelper.InitLog ( configFilePath );
        }

        public EntityMapping GetEntityMappingByClassName ( string className ) {
            return _mappings[className];
        }

        public ISession CreateSession () {
            IPersistenceContext persistenceContext = new PersistenceContext ( _driverFactory, _mappings );
            return new Session ( this, persistenceContext );
        }
    }
}
