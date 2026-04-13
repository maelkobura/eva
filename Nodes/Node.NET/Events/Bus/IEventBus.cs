using Eva.Commons.Events;
using Eva.Node.Events.Dispatcher;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Eva.Node.Events.Bus;

public interface IEventBus : IDisposable
{
    public TypeRegistry TypeRegistry { get; }
    
    
    public Task RegisterListener(Listener listener);
    public void UnregisterListener(Listener listener);
    void EmitSignal(string eventName, bool networked = true);
    void EmitSignal<T>(string eventName, T payload, bool networked = true) where T : IMessage;
    Task<T?> EmitSync<T>(string eventName, T payload, bool networked = true) where T : IMessage;
    Task<IReadOnlyList<T>> EmitAsync<T>(string eventName, T payload, bool networked = true) where T : IMessage;
    void EmitSignal(string eventName, IMessage payload, bool networked = true);
    Task<IMessage?> HandleNetworkRequestAsync(string eventName, NetworkEventFrame frame);
}