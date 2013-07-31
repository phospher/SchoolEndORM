using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Event;
using ORMFramework.Cache;

namespace ORMFramework.Listener {
    public class DefaultUpdateListener : IUpdateListener {

        public void OnUpdate ( UpdateEvent @event, object sender ) {
            @event.Cache.Update ( @event.Entity );
        }
    }
}
