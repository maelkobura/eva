namespace Eva.Node.Events.Dispatcher;

public interface INetworkEventSubscriber
{
    Task SubscribeAsync(string eventName);
}