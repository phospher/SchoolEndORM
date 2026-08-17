namespace ORMFramework.Configuration;
public class EntityRelation
{
    public required string KeyColum { get; set; }

    public required string Property { get; set; }

    public required string ReferenceClassName { get; set; }

    public required string ReferenceColum { get; set; }

    public RelationType Type { get; set; }
}