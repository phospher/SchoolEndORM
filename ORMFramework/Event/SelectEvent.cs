using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Statment;

namespace ORMFramework.Event {
    public class SelectEvent : ActionEvent {
        private QueryExpression _queryExpression;
        private Type _searchType;

        public QueryExpression QueryExpression {
            get { return _queryExpression; }
            set { _queryExpression = value; }
        }

        public Type SearchType {
            get { return _searchType; }
            set { _searchType = value; }
        }
    }
}
