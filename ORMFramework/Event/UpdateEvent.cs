namespace ORMFramework.Event;
public class UpdateEvent : ActionEvent
{
    public object Entity { get; set; } = null!;
}