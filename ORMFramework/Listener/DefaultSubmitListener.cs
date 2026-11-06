using ORMFramework.Event;

namespace ORMFramework.Listener;

public sealed class DefaultSubmitListener : ISubmitListener
{

    public void OnSubmit(ActionEvent @event, object sender) => @event.Cache.Commit();
}