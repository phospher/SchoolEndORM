namespace ORMFramework.Configuration;

public class EntityMapping
{
    private IEnumerable<string> _keys = [];
    private IDictionary<string, EntityRelation> _relations = new Dictionary<string, EntityRelation>();

    public string ClassName { get; set; } = "";

    public string TableName { get; set; } = "";

    public IEnumerable<string> Keys
    {
        get => _keys;
        set => _keys = value ?? [];
    }

    public IDictionary<string, EntityRelation> Relations
    {
        get => _relations;
        set => _relations = value ?? new Dictionary<string, EntityRelation>();
    }
}