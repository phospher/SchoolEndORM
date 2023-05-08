using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ORMFramework.Event
{
    public class CommandEvent
    {

        public string CommandText { get; set; }

        public IPersistenceContext PersistenceContext { get; set; }

        public bool IsQuery { get; set; }

        public object Result { get; set; }
    }
}