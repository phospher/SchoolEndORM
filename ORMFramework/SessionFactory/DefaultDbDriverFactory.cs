using System.Data;
using System.Data.Common;
using System.Collections.Concurrent;

namespace ORMFramework;
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
        ConnectionString = connectionString;
        SetProviderName(providerName);
    }

    public void SetProviderName(string providerName)
    {
        if (!_registedProviderName.ContainsKey(providerName))
        {
            DbProviderFactories.RegisterFactory(providerName, providerName);
            _registedProviderName.Add(providerName, new object());
        }
        _dbProviderFactory = DbProviderFactories.GetFactory(providerName);
    }

    public IDbConnection GetDbConnection()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new Exception("ConnectionString can not be null or empty");
        }
        return GetDbConnection(ConnectionString);
    }

    public IDbConnection GetDbConnection(string connectionString)
    {
        IDbConnection conn = _dbProviderFactory.CreateConnection();
        conn.ConnectionString = connectionString;
        return conn;
    }

    public IDbCommand GetDBCommand()
    {
        return _dbProviderFactory.CreateCommand();
    }

    public IDbCommand GetDBCommand(string commandText)
    {
        IDbCommand cmd = GetDBCommand();
        cmd.CommandText = commandText;
        return cmd;
    }

    public IDbCommand GetDBCommand(string commandText, IDbConnection connection)
    {
        IDbCommand cmd = GetDBCommand(commandText);
        cmd.Connection = connection;
        return cmd;
    }

    public IDbDataAdapter GetDbDataAdapter()
    {
        return _dbProviderFactory.CreateDataAdapter();
    }

    public IDbDataAdapter GetDbDataAdapter(string selectCommandText, IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = selectCommandText;
        IDbDataAdapter dataAdapter = GetDbDataAdapter();
        dataAdapter.SelectCommand = cmd;
        return dataAdapter;
    }

    public IDbDataAdapter GetDbDataAdapter(IDbCommand dbCommand)
    {
        IDbDataAdapter dataAdapter = GetDbDataAdapter();
        dataAdapter.SelectCommand = dbCommand;
        return dataAdapter;
    }

    public DbCommandBuilder GetDBCommandBuilder()
    {
        return _dbProviderFactory.CreateCommandBuilder();
    }

    public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter)
    {
        if (dbDataAdapter is DbDataAdapter)
        {
            DbCommandBuilder dbCommandBuilder = GetDBCommandBuilder();
            dbCommandBuilder.DataAdapter = (DbDataAdapter)dbDataAdapter;
            return dbCommandBuilder;
        }
        else
        {
            throw new Exception("dbDataAdapter must be DbDataAdapter");
        }
    }
}
