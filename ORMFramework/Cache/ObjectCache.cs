using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Cache {
    public abstract class ObjectCache {
        private Dictionary<Type, List<GlobalCacheEntity>> _objTable = new Dictionary<Type, List<GlobalCacheEntity>> ();
        private Dictionary<Guid, GlobalCacheEntity> _objList = new Dictionary<Guid, GlobalCacheEntity> ();

        protected Dictionary<Type, List<GlobalCacheEntity>> ObjTable {
            get { return _objTable; }
        }

        protected Dictionary<Guid, GlobalCacheEntity> ObjList {
            get { return _objList; }
        }
    }
}
