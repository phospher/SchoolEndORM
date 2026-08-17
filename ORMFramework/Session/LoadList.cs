using System.Collections;
using System.Text;
using ORMFramework.Cache;
using ORMFramework.Event;
using ORMFramework.Listener;
using ORMFramework.Statment;

namespace ORMFramework;
public class LoadList<T> : IList<T>
{
    private List<T> _items;

    public ISessionCache SessionCache { get; set; }

    public string ForeignKeyName { get; set; }

    public List<object> ForeignKeyValues { get; }

    public ISelectListener SelectListener { get; }

    public LoadList()
    {
        _items = null;
        ForeignKeyValues = new List<object>();
        SelectListener = new DefaultSelectListener();
    }

    public int IndexOf(T item)
    {
        if (_items == null)
        {
            GetItems();
        }
        return _items.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        if (_items == null)
        {
            GetItems();
        }
        _items.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        if (_items == null)
        {
            GetItems();
        }
        _items.RemoveAt(index);
    }

    public T this[int index]
    {
        get
        {
            if (_items == null)
            {
                GetItems();
            }
            return _items[index];
        }
        set
        {
            if (_items == null)
            {
                GetItems();
            }
            _items[index] = value;
        }
    }

    public void Add(T item)
    {
        if (_items == null)
        {
            GetItems();
        }
        _items.Add(item);
    }

    public void Clear()
    {
        if (_items == null)
        {
            GetItems();
        }
        _items.Clear();
    }

    public bool Contains(T item)
    {
        if (_items == null)
        {
            GetItems();
        }
        return _items.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (_items == null)
        {
            GetItems();
        }
        _items.CopyTo(array, arrayIndex);
    }

    public int Count
    {
        get
        {
            if (_items == null)
            {
                GetItems();
            }
            return _items.Count;
        }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public bool Remove(T item)
    {
        if (_items == null)
        {
            GetItems();
        }
        return _items.Remove(item);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        if (_items == null)
        {
            GetItems();
        }
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (_items == null)
        {
            GetItems();
        }
        return _items.GetEnumerator();
    }

    private void GetItems()
    {
        var statement = new StringBuilder();
        var @event = new SelectEvent();
        _items = new List<T>();
        for (int i = 0; i < ForeignKeyValues.Count; i++)
        {
            statement.Append(ForeignKeyName);
            statement.Append("==");
            if (ForeignKeyValues[i] is string || ForeignKeyValues[i] is char)
            {
                statement.Append(string.Format("'{0}'", ForeignKeyValues[i]));
            }
            else if (ForeignKeyValues[i] is DateTime)
            {
                statement.Append(string.Format("'{0}'", ((DateTime)ForeignKeyValues[i]).ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else
            {
                statement.Append(ForeignKeyValues[i]);
            }
            if (i < ForeignKeyValues.Count - 1)
            {
                statement.Append(" || ");
            }
        }
        QueryExpression expression = new StatementAnalyst(statement.ToString()).GetQueryExpression();
        @event.Cache = SessionCache;
        @event.QueryExpression = expression;
        @event.SearchType = typeof(T);
        SelectListener.OnSelect(@event, this);
        foreach (object item in (IEnumerable<object>)@event.Result)
        {
            _items.Add((T)item);
        }
    }

}