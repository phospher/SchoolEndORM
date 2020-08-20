using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ORMFramework.Event;

namespace ORMFramework.Listener
{
    class DefaultCommandListener : ICommandListener
    {

        public void OnExecute(CommandEvent @event, object sender)
        {
            IDbConnection conn = @event.PersistenceContext.Connection;
            bool isDbOpened = true;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
                isDbOpened = false;
            }
            if (@event.IsQuery)
            {
                IDbDataAdapter da = @event.PersistenceContext.DbDriverFactory.GetDbDataAdapter(@event.CommandText, conn);
                DataSet ds = new DataSet();
                da.Fill(ds);
                @event.Result = ds;
            }
            else
            {
                IDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @event.CommandText;
                @event.Result = cmd.ExecuteNonQuery();
            }
            if (!isDbOpened)
            {
                conn.Close();
            }
        }
    }
}