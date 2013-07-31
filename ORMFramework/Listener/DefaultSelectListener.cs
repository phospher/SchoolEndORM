using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Cache;
using ORMFramework.Event;

namespace ORMFramework.Listener {
    public class DefaultSelectListener : ISelectListener {

        public void OnSelect ( SelectEvent @event, object sender ) {
            ISessionCache sessionCache = @event.Cache;
            @event.Result = sessionCache.Search ( @event.SearchType, @event.QueryExpression );
        }
    }
}
