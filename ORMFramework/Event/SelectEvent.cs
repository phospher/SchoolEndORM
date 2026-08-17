using ORMFramework.Statment;

namespace ORMFramework.Event;
public class SelectEvent : ActionEvent
{

    public QueryExpression QueryExpression { get; set; } = null!;

    public Type SearchType { get; set; } = null!;
}