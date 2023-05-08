using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Event
{
    public class UpdateEvent : ActionEvent
    {
        public object Entity { get; set; }
    }
}