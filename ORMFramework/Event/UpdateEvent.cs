using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Event
{
    public class UpdateEvent : ActionEvent
    {
        private object _entity;

        public object Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }
    }
}