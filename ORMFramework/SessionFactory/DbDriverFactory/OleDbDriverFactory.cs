using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace ORMFramework.DbDriverFactory {
    public class OleDbDriverFactory : IDbDriverFactory {
        private string _connectionString;

        public string ConnectionString {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public OleDbDriverFactory () { }

        public OleDbDriverFactory ( string connectionString ) {
            _connectionString = connectionString;
        }

        public IDbConnection GetDbConnection () {
            if ( string.IsNullOrEmpty ( _connectionString ) ) {
                throw new Exception ( "ConnectionString can not be null or empty" );
            }

            return new OleDbConnection ( _connectionString );
        }

        public IDbConnection GetDbConnection ( string connectionString ) {
            return new OleDbConnection ( connectionString );
        }

        public IDbCommand GetDBCommand () {
            return new OleDbCommand ();
        }

        public IDbCommand GetDBCommand ( string commandText ) {
            return new OleDbCommand ( commandText );
        }

        public IDbCommand GetDBCommand ( string commandText, IDbConnection connection ) {
            if ( connection is OleDbConnection ) {
                return new OleDbCommand ( commandText, ( OleDbConnection ) connection );
            } else {
                throw new Exception ( "connection must be OleDbConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter () {
            return new OleDbDataAdapter ();
        }

        public IDbDataAdapter GetDbDataAdapter ( string selectCommandText, IDbConnection connection ) {
            if ( connection is OleDbConnection ) {
                return new OleDbDataAdapter ( selectCommandText, ( OleDbConnection ) connection );
            } else {
                throw new Exception ( "connection must be OleDbConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter ( IDbCommand dbCommand ) {
            if ( dbCommand is OleDbCommand ) {
                return new OleDbDataAdapter ( ( OleDbCommand ) dbCommand );
            } else {
                throw new Exception ( "dbCommand must be OleDbCommand" );
            }
        }

        public DbCommandBuilder GetDBCommandBuilder () {
            return new OleDbCommandBuilder ();
        }

        public DbCommandBuilder GetDBCommandBuilder ( IDbDataAdapter dbDataAdapter ) {
            if ( dbDataAdapter is OleDbDataAdapter ) {
                return new OleDbCommandBuilder ( ( OleDbDataAdapter ) dbDataAdapter );
            } else {
                throw new Exception ( "dbDataAdapter must be OleDbDataAdapter" );
            }
        }
    }
}
