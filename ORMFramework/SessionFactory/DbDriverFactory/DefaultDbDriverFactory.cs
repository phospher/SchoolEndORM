using System;
using System.Data;
using System.Data.Common;

namespace ORMFramework.SessionFactory.DbDriverFactory
{
    public class DefaultDbDriverFactory : IDbDriverFactory
    {

        public string ConnectionString { get; set; }

        private DbProviderFactory _dbProviderFactory;

        public DefaultDbDriverFactory(string providerName) : this(string.Empty, providerName)
        {

        }

        public DefaultDbDriverFactory(string connectionString, string providerName)
        {
            this.ConnectionString = connectionString;
            this._dbProviderFactory = DbProviderFactories.GetFactory(providerName);
        }

        public IDbConnection GetDbConnection()
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new Exception("ConnectionString can not be null or empty");
            }
            return this.GetDbConnection(this.ConnectionString);
        }

        public IDbConnection GetDbConnection(string connectionString)
        {
            IDbConnection conn = this._dbProviderFactory.CreateConnection();
            conn.ConnectionString = connectionString;
            return conn;
        }

        public IDbCommand GetDBCommand()
        {
            return this._dbProviderFactory.CreateCommand();
        }

        public IDbCommand GetDBCommand(string commandText)
        {
            IDbCommand cmd = this.GetDBCommand();
            cmd.CommandText = commandText;
            return cmd;
        }

        public IDbCommand GetDBCommand(string commandText, IDbConnection connection)
        {
            IDbCommand cmd = this.GetDBCommand(commandText);
            cmd.Connection = connection;
            return cmd;
        }

        public IDbDataAdapter GetDbDataAdapter()
        {
            return this._dbProviderFactory.CreateDataAdapter();
        }

        public IDbDataAdapter GetDbDataAdapter(string selectCommandText, IDbConnection connection)
        {
            IDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = selectCommandText;
            IDbDataAdapter dataAdapter = this.GetDbDataAdapter();
            dataAdapter.SelectCommand = cmd;
            return dataAdapter;
        }

        public IDbDataAdapter GetDbDataAdapter(IDbCommand dbCommand)
        {
            IDbDataAdapter dataAdapter = this.GetDbDataAdapter();
            dataAdapter.SelectCommand = dbCommand;
            return dataAdapter;
        }

        public DbCommandBuilder GetDBCommandBuilder()
        {
            throw new NotImplementedException();
        }

        public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter)
        {
            throw new NotImplementedException();
        }
    }
}
