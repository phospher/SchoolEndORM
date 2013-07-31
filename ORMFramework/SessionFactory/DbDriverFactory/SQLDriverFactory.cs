using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace ORMFramework.DbDriverFactory {
    public class SQLDriverFactory : IDbDriverFactory {
        private string _connectionString;

        public string ConnectionString {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public SQLDriverFactory () { }

        public SQLDriverFactory ( string connectionString ) {
            _connectionString = connectionString;
        }

        public IDbConnection GetDbConnection () {
            if ( string.IsNullOrEmpty ( _connectionString ) ) {
                throw new Exception ( "ConnectionString can not be null or empty" );
            }

            return new SqlConnection ( _connectionString );
        }

        public IDbConnection GetDbConnection ( string connectionString ) {
            return new SqlConnection ( connectionString );
        }

        public IDbCommand GetDBCommand () {
            return new SqlCommand ();
        }

        public IDbCommand GetDBCommand ( string commandText ) {
            return new SqlCommand ( commandText );
        }

        public IDbCommand GetDBCommand ( string commandText, IDbConnection connection ) {
            if ( connection is SqlConnection ) {
                return new SqlCommand ( commandText, ( SqlConnection ) connection );
            } else {
                throw new Exception ( "connection must be SqlConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter () {
            return new SqlDataAdapter ();
        }

        public IDbDataAdapter GetDbDataAdapter ( string selectCommandText, IDbConnection connection ) {
            if ( connection is SqlConnection ) {
                return new SqlDataAdapter ( selectCommandText, ( SqlConnection ) connection );
            } else {
                throw new Exception ( "connection must be SqlConnection" );
            }
        }

        public IDbDataAdapter GetDbDataAdapter ( IDbCommand dbCommand ) {
            if ( dbCommand is SqlCommand ) {
                return new SqlDataAdapter ( ( SqlCommand ) dbCommand );
            } else {
                throw new Exception ( "dbCommand must be SqlCommand" );
            }
        }

        public DbCommandBuilder GetDBCommandBuilder () {
            return new SqlCommandBuilder ();
        }

        public DbCommandBuilder GetDBCommandBuilder ( IDbDataAdapter dbDataAdapter ) {
            if ( dbDataAdapter is SqlDataAdapter ) {
                return new SqlCommandBuilder ( ( SqlDataAdapter ) dbDataAdapter );
            } else {
                throw new Exception ( "dbDataAdapter must be SqlDataAdapter" );
            }
        }
    }
}
