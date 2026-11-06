using System.Collections;
using System.Text;
using ORMFramework.Cache;
using ORMFramework.Event;
using ORMFramework.Listener;
using ORMFramework.SQL;
using ORMFramework.Statment;

namespace ORMFramework;

public sealed class LoadList<T> : IList<T>
{
    private List<T>? _items;

    public ISessionCache SessionCache { get; set; } = null!;

    public string ForeignKeyName { get; set; } = "";

    public List<object> ForeignKeyValues { get; }

    public ISelectListener SelectListener { get; }

    private List<T> Items => _items ??= LoadItems();

    public LoadList()
    {
        ForeignKeyValues = new List<object>();
        SelectListener = new DefaultSelectListener();
    }

    public int IndexOf(T item) => Items.IndexOf(item);

    public void Insert(int index, T item) => Items.Insert(index, item);

    public void RemoveAt(int index) => Items.RemoveAt(index);

    public T this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    public void Add(T item) => Items.Add(item);

    public void Clear() => Items.Clear();

    public bool Contains(T item) => Items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

    public int Count => Items.Count;

    public bool IsReadOnly => false;

    public bool Remove(T item) => Items.Remove(item);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

    private List<T> LoadItems()
    {
        var statement = new StringBuilder();
        var @event = new SelectEvent();
        var items = new List<T>();
        for (int i = 0; i < ForeignKeyValues.Count; i++)
        {
            statement.Append(ForeignKeyName);
            statement.Append("==");
            statement.Append(SqlLiteral.Format(ForeignKeyValues[i]));
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
        foreach (object item in (IEnumerable<object>)@event.Result!)
        {
            items.Add((T)item);
        }
        return items;
    }

}