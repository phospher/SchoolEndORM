using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Cache;

namespace ORMFramework.Event
{
    public class ActionEvent
    {
        private object _result;
        private ISessionCache _cache;

        public object Result
        {
            get { return _result; }
            set { _result = value; }
        }

        public ISessionCache Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }
    }
}