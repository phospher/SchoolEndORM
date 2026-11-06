namespace ORMFramework.Configuration;

public class EntityMapping
{
    public string ClassName { get; set; } = "";

    public string TableName { get; set; } = "";

    public IEnumerable<string> Keys
    {
        get => field;
        set => field = value ?? [];
    } = [];

    public IDictionary<string, EntityRelation> Relations
    {
        get => field;
        set => field = value ?? new Dictionary<string, EntityRelation>();
    } = new Dictionary<string, EntityRelation>();
}