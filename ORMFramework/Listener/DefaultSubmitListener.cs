using ORMFramework.Event;

namespace ORMFramework.Listener;
public class DefaultSubmitListener : ISubmitListener
{

    public void OnSubmit(ActionEvent @event, object sender) => @event.Cache.Commit();
}