using ORMFramework.Event;

namespace ORMFramework.Listener;
public sealed class DefaultUpdateListener : IUpdateListener
{

    public void OnUpdate(UpdateEvent @event, object sender) => @event.Cache.Update(@event.Entity);
}