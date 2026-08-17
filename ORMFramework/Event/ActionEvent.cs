using ORMFramework.Cache;

namespace ORMFramework.Event;
public class ActionEvent
{
    public object? Result { get; set; }

    public ISessionCache Cache { get; set; } = null!;
}