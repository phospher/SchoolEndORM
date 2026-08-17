namespace ORMFramework.Event;
public class CommandEvent
{

    public string CommandText { get; set; } = "";

    public IPersistenceContext PersistenceContext { get; set; } = null!;

    public bool IsQuery { get; set; }

    public object? Result { get; set; }
}