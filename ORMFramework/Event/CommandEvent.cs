using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ORMFramework.Event
{
    public class CommandEvent
    {
        private string _commandText;
        private IPersistenceContext _persistenceContext;
        private bool _isQuery;
        private object _result;

        public string CommandText
        {
            get { return _commandText; }
            set { _commandText = value; }
        }

        public IPersistenceContext PersistenceContext
        {
            get { return _persistenceContext; }
            set { _persistenceContext = value; }
        }

        public bool IsQuery
        {
            get { return _isQuery; }
            set { _isQuery = value; }
        }

        public object Result
        {
            get { return _result; }
            set { _result = value; }
        }
    }
}