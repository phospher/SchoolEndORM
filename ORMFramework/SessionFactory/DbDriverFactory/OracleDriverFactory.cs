using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;

namespace ORMFramework.DbDriverFactory {
    public class OracleDriverFactory : IDbDriverFactory {
        private string _connectionString;

        public string ConnectionString {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public OracleDriverFactory () { }

        public OracleDriverFactory ( string connectionString ) {
            _connectionString = connectionString;
        }

        public IDbConnection GetDbConnection () {
            if ( string.IsNullOrEmpty ( _connectionString ) ) {
                throw new Exception ( "ConnectionString can not be null or empty" );
            }

            return new OracleConnection ( _connectionString );
        }

        public IDbConnection GetDbConnection ( string connectionString ) {
            return new OracleConnection ( connectionString );
        }

        public IDbCommand GetDBCommand () {
            return new OracleCommand ();
        }

        public IDbCommand GetDBCommand ( string commandText ) {
            return new OracleCommand ( commandText );
        }

        public IDbCommand GetDBCommand ( string commandText, IDbConnection connection ) {
            if ( connection is OracleConnection ) {
                return new OracleCommand ( commandText, ( OracleConnection ) connection );
            } else {
                throw new Exception ( "connection must be OracleConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter () {
            return new OracleDataAdapter ();
        }

        public IDbDataAdapter GetDbDataAdapter ( string selectCommandText, IDbConnection connection ) {
            if ( connection is OracleConnection ) {
                return new OracleDataAdapter ( selectCommandText, ( OracleConnection ) connection );
            } else {
                throw new Exception ( "connection must be OracleConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter ( IDbCommand dbCommand ) {
            if ( dbCommand is OracleCommand ) {
                return new OracleDataAdapter ( ( OracleCommand ) dbCommand );
            } else {
                throw new Exception ( "dbCommand must be OracleCommand" );
            }
        }

        public DbCommandBuilder GetDBCommandBuilder () {
            return new OracleCommandBuilder ();
        }

        public DbCommandBuilder GetDBCommandBuilder ( IDbDataAdapter dbDataAdapter ) {
            if ( dbDataAdapter is OracleDataAdapter ) {
                return new OracleCommandBuilder ( ( OracleDataAdapter ) dbDataAdapter );
            } else {
                throw new Exception ( "dbDataAdapter must be OracleDataAdapter" );
            }
        }
    }
}
