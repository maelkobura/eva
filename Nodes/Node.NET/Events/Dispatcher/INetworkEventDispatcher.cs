using Eva.Commons.Events;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Eva.Node.Events.Dispatcher;

public interface INetworkEventDispatcher
{
    void DispatchSignal(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry);
    Task<IMessage?> DispatchSyncAsync(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry);
    Task<IReadOnlyList<IMessage>> DispatchAsync(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry);
}