using ORMFramework.Event;

namespace ORMFramework.Listener;
public class DefaultUpdateListener : IUpdateListener
{

    public void OnUpdate(UpdateEvent @event, object sender) => @event.Cache.Update(@event.Entity);
}