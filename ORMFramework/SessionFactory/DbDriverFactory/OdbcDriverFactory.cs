using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Text;

namespace ORMFramework.DbDriverFactory
{
    public class OdbcDriverFactory : IDbDriverFactory
    {
        private string _connectionString;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public OdbcDriverFactory() { }

        public OdbcDriverFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetDbConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new Exception("ConnectionString can not be null or empty");
            }

            return new OdbcConnection(_connectionString);
        }

        public IDbConnection GetDbConnection(string connectionString)
        {
            return new OdbcConnection(connectionString);
        }

        public IDbCommand GetDBCommand()
        {
            return new OdbcCommand();
        }

        public IDbCommand GetDBCommand(string commandText)
        {
            return new OdbcCommand(commandText);
        }

        public IDbCommand GetDBCommand(string commandText, IDbConnection connection)
        {
            if (connection is OdbcConnection)
            {
                return new OdbcCommand(commandText, (OdbcConnection)connection);
            }
            else
            {
                throw new Exception("connection must be OdbcConnection");
            }
        }

        public IDbDataAdapter GetDbDataAdapter()
        {
            return new OdbcDataAdapter();
        }

        public IDbDataAdapter GetDbDataAdapter(string selectCommandText, IDbConnection connection)
        {
            if (connection is OdbcConnection)
            {
                return new OdbcDataAdapter(selectCommandText, (OdbcConnection)connection);
            }
            else
            {
                throw new Exception("connection must be OdbcConnection");
            }
        }

        public IDbDataAdapter GetDbDataAdapter(IDbCommand dbCommand)
        {
            if (dbCommand is OdbcCommand)
            {
                return new OdbcDataAdapter((OdbcCommand)dbCommand);
            }
            else
            {
                throw new Exception("dbCommand must be OdbcCommand");
            }
        }

        public DbCommandBuilder GetDBCommandBuilder()
        {
            return new OdbcCommandBuilder();
        }

        public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter)
        {
            if (dbDataAdapter is OdbcDataAdapter)
            {
                return new OdbcCommandBuilder((OdbcDataAdapter)dbDataAdapter);
            }
            else
            {
                throw new Exception("dbDataAdapter must be OdbcDataAdapter");
            }
        }
    }
}