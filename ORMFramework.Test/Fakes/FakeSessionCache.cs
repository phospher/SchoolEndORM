using ORMFramework.Cache;
using ORMFramework.Statment;

namespace ORMFramework.Test.Fakes;

public sealed class FakeSessionCache : ISessionCache
{
    private readonly List<Type> _searchTypes = new List<Type>();
    private readonly List<QueryExpression> _searchExpressions = new List<QueryExpression>();

    public int SearchCallCount
    {
        get { return _searchExpressions.Count; }
    }

    public IReadOnlyList<Type> SearchTypes
    {
        get { return _searchTypes; }
    }

    public IReadOnlyList<QueryExpression> SearchExpressions
    {
        get { return _searchExpressions; }
    }

    public IEnumerable<object> SearchResult { get; set; } = Array.Empty<object>();

    public IEnumerable<object> Search(Type objType, QueryExpression? expression)
    {
        _searchTypes.Add(objType);
        _searchExpressions.Add(expression!);
        return SearchResult;
    }

    public IPersistenceContext PersistenceContext { get; set; } = null!;

    public void Commit()
    {
    }

    public void Delete(object @object)
    {
    }

    public void GetReferenceClasses(object soruceObject)
    {
    }

    public void Insert(object @object)
    {
    }

    public void Update(object @object)
    {
    }

    public void Dispose()
    {
    }
}
