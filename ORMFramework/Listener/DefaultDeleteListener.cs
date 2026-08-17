using ORMFramework.Event;

namespace ORMFramework.Listener;
public sealed class DefaultDeleteListener : IDeleteListener
{

    public void OnDelete(UpdateEvent @event, object sender) => @event.Cache.Delete(@event.Entity);
}