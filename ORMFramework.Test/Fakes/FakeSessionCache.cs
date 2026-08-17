using ORMFramework.Cache;
using ORMFramework.Statment;

namespace ORMFramework.Test.Fakes;
public sealed class FakeSessionCache : ISessionCache
{
    private readonly List<Type> _searchTypes = new List<Type>();
    private readonly List<QueryExpression> _searchExpressions = new List<QueryExpression>();
    private IEnumerable<object> _searchResult = Array.Empty<object>();

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

    public IEnumerable<object> SearchResult
    {
        get { return _searchResult; }
        set { _searchResult = value; }
    }

    public IEnumerable<object> Search(Type objType, QueryExpression expression)
    {
        _searchTypes.Add(objType);
        _searchExpressions.Add(expression);
        return _searchResult;
    }

    public IPersistenceContext PersistenceContext { get; set; }

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
