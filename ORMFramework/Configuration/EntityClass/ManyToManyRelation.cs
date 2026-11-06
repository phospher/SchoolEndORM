namespace ORMFramework.Configuration;

public class ManyToManyRelation : EntityRelation
{
    public required string ReferenceTableName { get; set; }

    public required string ReferenceClassKeyColum { get; set; }

    public required string ReferenceClassColum { get; set; }
}