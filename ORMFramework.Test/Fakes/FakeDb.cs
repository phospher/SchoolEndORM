using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace ORMFramework.Test.Fakes;

public sealed class FakeDbConnection : IDbConnection
{
    public bool IsDisposed { get; private set; }

    public int OpenCount { get; private set; }

    public int CloseCount { get; private set; }

    [AllowNull]
    public string ConnectionString { get; set; } = null!;

    public int ConnectionTimeout => 0;

    public string Database => string.Empty;

    public ConnectionState State { get; private set; } = ConnectionState.Closed;

    public IDbTransaction BeginTransaction() => BeginTransaction(IsolationLevel.Unspecified);

    public IDbTransaction BeginTransaction(IsolationLevel il) => new FakeDbTransaction(this, il);

    public void ChangeDatabase(string databaseName)
    {
    }

    public void Close()
    {
        CloseCount++;
        State = ConnectionState.Closed;
    }

    public IDbCommand CreateCommand() => new FakeDbCommand { Connection = this };

    public void Open()
    {
        OpenCount++;
        State = ConnectionState.Open;
    }

    public void Dispose()
    {
        IsDisposed = true;
        State = ConnectionState.Closed;
    }
}

public sealed class FakeDbCommand : IDbCommand
{
    public bool IsDisposed { get; private set; }

    [AllowNull]
    public string CommandText { get; set; } = null!;

    public int CommandTimeout { get; set; }

    public CommandType CommandType { get; set; }

    public IDbConnection? Connection { get; set; }

    public IDataParameterCollection Parameters => null!;

    public IDbTransaction? Transaction { get; set; }

    public UpdateRowSource UpdatedRowSource { get; set; }

    public int ExecuteNonQueryResult { get; set; } = 1;

    public void Cancel()
    {
    }

    public IDbDataParameter CreateParameter() => null!;

    public int ExecuteNonQuery() => ExecuteNonQueryResult;

    public IDataReader ExecuteReader() => null!;

    public IDataReader ExecuteReader(CommandBehavior behavior) => null!;

    public object ExecuteScalar() => null!;

    public void Prepare()
    {
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public sealed class FakeDbTransaction : IDbTransaction
{
    public FakeDbTransaction(IDbConnection connection, IsolationLevel isolationLevel)
    {
        Connection = connection;
        IsolationLevel = isolationLevel;
    }

    public bool IsDisposed { get; private set; }

    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public IDbConnection Connection { get; }

    public IsolationLevel IsolationLevel { get; }

    public void Commit()
    {
        Committed = true;
    }

    public void Rollback()
    {
        RolledBack = true;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public sealed class FakeDbDataAdapter : IDbDataAdapter
{
    public IDbCommand? SelectCommand { get; set; }

    public IDbCommand? InsertCommand { get; set; }

    public IDbCommand? UpdateCommand { get; set; }

    public IDbCommand? DeleteCommand { get; set; }

    public MissingMappingAction MissingMappingAction { get; set; }

    public MissingSchemaAction MissingSchemaAction { get; set; }

    public ITableMappingCollection TableMappings => null!;

    public int Fill(DataSet dataSet)
    {
        dataSet.Tables.Add(new DataTable());
        return 0;
    }

    public DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType) => null!;

    public IDataParameter[] GetFillParameters() => null!;

    public int Update(DataSet dataSet) => 0;
}

public sealed class FakeDbDriverFactory : IDbDriverFactory
{
    public string ConnectionString { get; set; } = "";

    public FakeDbConnection Connection { get; set; } = new();

    public IDbConnection GetDbConnection() => Connection;

    public IDbConnection GetDbConnection(string connectionString) => Connection;

    public IDbCommand GetDBCommand() => new FakeDbCommand();

    public IDbCommand GetDBCommand(string commandText) => new FakeDbCommand { CommandText = commandText };

    public IDbCommand GetDBCommand(string commandText, IDbConnection connection) => new FakeDbCommand { CommandText = commandText, Connection = connection };

    public IDbDataAdapter GetDbDataAdapter() => new FakeDbDataAdapter();

    public IDbDataAdapter GetDbDataAdapter(string selectCommandText, IDbConnection connection) => new FakeDbDataAdapter { SelectCommand = new FakeDbCommand { CommandText = selectCommandText, Connection = connection } };

    public IDbDataAdapter GetDbDataAdapter(IDbCommand dbCommand) => new FakeDbDataAdapter { SelectCommand = dbCommand };

    public DbCommandBuilder GetDBCommandBuilder() => throw new NotImplementedException();

    public DbCommandBuilder GetDBCommandBuilder(IDbDataAdapter dbDataAdapter) => throw new NotImplementedException();

    public void SetProviderName(string providerName)
    {
    }
}
