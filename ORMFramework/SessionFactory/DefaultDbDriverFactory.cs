using System.Data;
using System.Data.Common;
using System.Collections.Concurrent;

namespace ORMFramework;

public sealed class DefaultDbDriverFactory : IDbDriverFactory
{

    public string ConnectionString { get; set; } = "";

    private DbProviderFactory _dbProviderFactory = null!;

    private readonly static ConcurrentDictionary<string, byte> _registeredProviderName = new();

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
        if (_registeredProviderName.TryAdd(providerName, 0))
        {
            DbProviderFactories.RegisterFactory(providerName, providerName);
        }
        _dbProviderFactory = DbProviderFactories.GetFactory(providerName);
    }

    public IDbConnection GetDbConnection()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString can not be null or empty");
        }
        return GetDbConnection(ConnectionString);
    }

    public IDbConnection GetDbConnection(string connectionString)
    {
        IDbConnection conn = _dbProviderFactory.CreateConnection() ?? throw new InvalidOperationException("DbProviderFactory returned a null connection");
        conn.ConnectionString = connectionString;
        return conn;
    }

    public IDbCommand GetDBCommand()
    {
        return _dbProviderFactory.CreateCommand() ?? throw new InvalidOperationException("DbProviderFactory returned a null command");
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
        return _dbProviderFactory.CreateDataAdapter() ?? throw new InvalidOperationException("DbProviderFactory returned a null data adapter");
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
        return _dbProviderFactory.CreateCommandBuilder() ?? throw new InvalidOperationException("DbProviderFactory returned a null command builder");
    }

    public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter)
    {
        if (dbDataAdapter is not DbDataAdapter adapter)
        {
            throw new ArgumentException("dbDataAdapter must be DbDataAdapter", nameof(dbDataAdapter));
        }
        DbCommandBuilder dbCommandBuilder = GetDBCommandBuilder();
        dbCommandBuilder.DataAdapter = adapter;
        return dbCommandBuilder;
    }
}
