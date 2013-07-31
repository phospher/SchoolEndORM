using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Event;

namespace ORMFramework.Listener {
    public class DefaultInsertListener : IInsertListener {

        public void OnInsert ( UpdateEvent @event, object sender ) {
            @event.Cache.Insert ( @event.Entity );
        }
    }
}
