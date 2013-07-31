using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Statment;

namespace ORMFramework.SQL {
    public interface ISQLGenerator {
        IPersistenceContext PersistenceContext { get;set;}
        string GetSelectSQL ( Type type );
        string GetSelectSQL ( Type type, QueryExpression queryExpression );
        string GetUpdateSQL ( object oldObject, object newObject );
        string GetDeleteSQL ( object @object );
        string GetInsertSQL ( object @object );
    }
}
