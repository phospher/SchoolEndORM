using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Cache;
using ORMFramework.Event;

namespace ORMFramework.Listener
{
    public class DefaultUpdateListener : IUpdateListener
    {

        public void OnUpdate(UpdateEvent @event, object sender)
        {
            @event.Cache.Update(@event.Entity);
        }
    }
}