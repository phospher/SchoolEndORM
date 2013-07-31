using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using ORMFramework.Listener;
using ORMFramework.Statment;
using ORMFramework.Event;
using ORMFramework.Cache;

namespace ORMFramework {
    public class LoadList<T> : IList<T> {
        private List<T> _items;
        private ISessionCache _sessionCache;
        private string _foreignKeyName;
        private List<object> _foreignKeyValues;
        private ISelectListener _selectListener;

        public ISessionCache SessionCache {
            get { return _sessionCache; }
            set { _sessionCache = value; }
        }

        public string ForeignKeyName {
            get { return _foreignKeyName; }
            set { _foreignKeyName = value; }
        }

        public List<object> ForeignKeyValues {
            get { return _foreignKeyValues; }
        }

        public ISelectListener SelectListener {
            get { return _selectListener; }
        }

        public LoadList () {
            _items = null;
            _foreignKeyValues = new List<object> ();
            _selectListener = new DefaultSelectListener ();
        }

        public int IndexOf ( T item ) {
            if ( _items == null ) {
                GetItems ();
            }
            return _items.IndexOf ( item );
        }

        public void Insert ( int index, T item ) {
            if ( _items == null ) {
                GetItems ();
            }
            _items.Insert ( index, item );
        }

        public void RemoveAt ( int index ) {
            if ( _items == null ) {
                GetItems ();
            }
            _items.RemoveAt ( index );
        }

        public T this[int index] {
            get {
                if ( _items == null ) {
                    GetItems ();
                }
                return _items[index];
            }
            set {
                if ( _items == null ) {
                    GetItems ();
                }
                _items[index] = value;
            }
        }

        public void Add ( T item ) {
            if ( _items == null ) {
                GetItems ();
            }
            _items.Add ( item );
        }

        public void Clear () {
            if ( _items == null ) {
                GetItems ();
            }
            _items.Clear ();
        }

        public bool Contains ( T item ) {
            if ( _items == null ) {
                GetItems ();
            }
            return _items.Contains ( item );
        }

        public void CopyTo ( T[] array, int arrayIndex ) {
            if ( _items == null ) {
                GetItems ();
            }
            _items.CopyTo ( array, arrayIndex );
        }

        public int Count {
            get {
                if ( _items == null ) {
                    GetItems ();
                }
                return _items.Count;
            }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove ( T item ) {
            if ( _items == null ) {
                GetItems ();
            }
            return _items.Remove ( item );
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator () {
            if ( _items == null ) {
                GetItems ();
            }
            return _items.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator () {
            if ( _items == null ) {
                GetItems ();
            }
            return _items.GetEnumerator ();
        }

        private void GetItems () {
            StringBuilder statement = new StringBuilder ();
            QueryExpression expression;
            SelectEvent @event = new SelectEvent ();
            _items = new List<T> ();
            for ( int i = 0; i < _foreignKeyValues.Count; i++ ) {
                statement.Append ( ForeignKeyName );
                statement.Append ( "==" );
                if ( _foreignKeyValues[i] is string || _foreignKeyValues[i] is char ) {
                    statement.Append ( string.Format ( "'{0}'", _foreignKeyValues[i] ) );
                } else if ( _foreignKeyValues[i] is DateTime ) {
                    statement.Append ( string.Format ( "'{0}'", ( ( DateTime ) _foreignKeyValues[i] ).ToString ( "yyyy-MM-dd HH:mm:ss" ) ) );
                } else {
                    statement.Append ( _foreignKeyValues[i] );
                }
                if ( i < _foreignKeyValues.Count - 1 ) {
                    statement.Append ( " || " );
                }
            }
            expression = new StatementAnalyst ( statement.ToString () ).GetQueryExpression ();
            @event.Cache = _sessionCache;
            @event.QueryExpression = expression;
            @event.SearchType = typeof ( T );
            _selectListener.OnSelect ( @event, this );
            foreach ( object item in ( IEnumerable<object> ) @event.Result ) {
                _items.Add ( ( T ) item );
            }
        }

    }
}
