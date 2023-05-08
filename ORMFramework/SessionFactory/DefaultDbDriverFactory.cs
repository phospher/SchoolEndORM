using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Collections.Concurrent;
using System.Configuration.Provider;

namespace ORMFramework
{
    public class DefaultDbDriverFactory : IDbDriverFactory
    {

        public string ConnectionString { get; set; }

        private DbProviderFactory _dbProviderFactory;

        private readonly static IDictionary<string, object> _registedProviderName = new ConcurrentDictionary<string, object>();

        public DefaultDbDriverFactory()
        {
            
        }

        public DefaultDbDriverFactory(string providerName) : this(string.Empty, providerName)
        {

        }

        public DefaultDbDriverFactory(string connectionString, string providerName)
        {
            this.ConnectionString = connectionString;
            this.SetProviderName(providerName);
        }

        public void SetProviderName(string providerName)
        {
            if (!_registedProviderName.ContainsKey(providerName))
            {
                DbProviderFactories.RegisterFactory(providerName, providerName);
                _registedProviderName.Add(providerName, new object());
            }
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
            return this._dbProviderFactory.CreateCommandBuilder();
        }

        public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter)
        {
            if (dbDataAdapter is DbDataAdapter)
            {
                DbCommandBuilder dbCommandBuilder = this.GetDBCommandBuilder();
                dbCommandBuilder.DataAdapter = (DbDataAdapter)dbDataAdapter;
                return dbCommandBuilder;
            }
            else
            {
                throw new Exception("dbDataAdapter must be DbDataAdapter");
            }
        }
    }
}
