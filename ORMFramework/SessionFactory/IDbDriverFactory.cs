using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

namespace ORMFramework {
    public interface IDbDriverFactory {
        string ConnectionString { get;set;}

        IDbConnection GetDbConnection ();
        IDbConnection GetDbConnection ( string connectionString );
        IDbCommand GetDBCommand ();
        IDbCommand GetDBCommand ( string commandText );
        IDbCommand GetDBCommand ( string commandText, IDbConnection connection );
        IDbDataAdapter GetDbDataAdapter ();
        IDbDataAdapter GetDbDataAdapter ( string selectCommandText, IDbConnection connection );
        IDbDataAdapter GetDbDataAdapter ( IDbCommand dbCommand );
        DbCommandBuilder GetDBCommandBuilder ();
        DbCommandBuilder GetDBCommandBuilder ( IDbDataAdapter dbDataAdapter );
    }
}
