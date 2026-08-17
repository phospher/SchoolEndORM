namespace ORMFramework.Configuration;
public class ManyToManyRelation : EntityRelation
{
    public string ReferenceTableName { get; set; }

    public string ReferenceClassKeyColum { get; set; }

    public string ReferenceClassColum { get; set; }
}