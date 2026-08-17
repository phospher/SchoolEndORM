using ORMFramework.Statment;

namespace ORMFramework.Event;
public class SelectEvent : ActionEvent
{

    public QueryExpression QueryExpression { get; set; }

    public Type SearchType { get; set; }
}