using ORMFramework.Event;

namespace ORMFramework.Listener;
public class DefaultDeleteListener : IDeleteListener
{

    public void OnDelete(UpdateEvent @event, object sender)
    {
        @event.Cache.Delete(@event.Entity);
    }
}