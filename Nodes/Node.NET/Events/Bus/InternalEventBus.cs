using System.Reflection;
using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events.Dispatcher;
using Eva.Node.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Eva.Node.Events.Bus;

internal record SignalHandler(object Instance, MethodInfo Method, Type? ParamType);
internal record SyncHandler(object Instance, MethodInfo Method, Type ParamType, int Priority);
internal record AsyncHandler(object Instance, MethodInfo Method, Type ParamType);

public class InternalEventBus : IEventBus
{
    

    private readonly Dictionary<string, List<SignalHandler>> _signals    = new();
    private readonly Dictionary<string, List<SyncHandler>>  _syncEvents  = new();
    private readonly Dictionary<string, List<AsyncHandler>> _asyncEvents = new();
    
    
    private INetworkEventDispatcher? _networkDispatcher;
    private INetworkEventSubscriber? _networkSubscriber;

    public void SetNetworkDispatcher(INetworkEventDispatcher dispatcher)
        => _networkDispatcher = dispatcher;

    public void SetNetworkSubscriber(INetworkEventSubscriber subscriber)
        => _networkSubscriber = subscriber;

    private NetworkEventFrame BuildFrame<T>(string eventName, T payload, NetworkEventFrameType type) where T : IMessage
        => new NetworkEventFrame
        {
            TypeUrl  = payload.Descriptor.FullName,
            Payload  = payload.ToByteString(),
            FrameType = type
        };

    public async Task RegisterListener(Listener listener)
    {
        var methods = listener.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<EventAttribute>() is null) continue;

            if (method.GetCustomAttribute<SignalEventAttribute>() is { } signalAttr)
            {
                RegisterSignalHandler(listener, method, signalAttr);
                if (signalAttr.EventName.StartsWith('@') && _networkSubscriber is not null)
                    _networkSubscriber.SubscribeAsync(signalAttr.EventName);
            }
            else if (method.GetCustomAttribute<SyncEventAttribute>() is { } syncAttr)
            {
                RegisterSyncHandler(listener, method, syncAttr);
                if (syncAttr.EventName.StartsWith('@') && _networkSubscriber is not null)
                    _networkSubscriber.SubscribeAsync(syncAttr.EventName);
            }
            else if (method.GetCustomAttribute<AsyncEventAttribute>() is { } asyncAttr)
            {
                RegisterAsyncHandler(listener, method, asyncAttr);
                if (asyncAttr.EventName.StartsWith('@') && _networkSubscriber is not null)
                    _networkSubscriber.SubscribeAsync(asyncAttr.EventName);
            }
        }
    }

    private void RegisterSignalHandler(Listener listener, MethodInfo method, SignalEventAttribute attr)
    {
        var parameters = method.GetParameters();
        if (parameters.Length > 1)
            throw new InvalidOperationException(
                $"[SignalEvent] '{method.Name}' must have at most one parameter.");

        Type? paramType = null;
        if (parameters.Length == 1)
        {
            paramType = parameters[0].ParameterType;
            if (!typeof(IMessage).IsAssignableFrom(paramType))
                throw new InvalidOperationException(
                    $"[SignalEvent] '{method.Name}' parameter must be a Protobuf IMessage.");
        }

        _signals.TryAdd(attr.EventName, []);
        _signals[attr.EventName].Add(new(listener, method, paramType));
    }

    private void RegisterSyncHandler(Listener listener, MethodInfo method, SyncEventAttribute attr)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw new InvalidOperationException(
                $"[SyncEvent] '{method.Name}' must have exactly one parameter.");

        var paramType = parameters[0].ParameterType;
        if (!typeof(IMessage).IsAssignableFrom(paramType))
            throw new InvalidOperationException(
                $"[SyncEvent] '{method.Name}' parameter must be a Protobuf IMessage.");

        var returnType = method.ReturnType;
        if (!typeof(IMessage).IsAssignableFrom(returnType))
            throw new InvalidOperationException(
                $"[SyncEvent] '{method.Name}' must return an IMessage. Got '{returnType.Name}'.");

        _syncEvents.TryAdd(attr.EventName, []);
        _syncEvents[attr.EventName].Add(new(listener, method, paramType, attr.Priority));
        _syncEvents[attr.EventName].Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    private void RegisterAsyncHandler(Listener listener, MethodInfo method, AsyncEventAttribute attr)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw new InvalidOperationException(
                $"[AsyncEvent] '{method.Name}' must have exactly one parameter.");

        var paramType = parameters[0].ParameterType;
        if (!typeof(IMessage).IsAssignableFrom(paramType))
            throw new InvalidOperationException(
                $"[AsyncEvent] '{method.Name}' parameter must be a Protobuf IMessage.");

        var returnType = method.ReturnType;
        var isDirectMessage = typeof(IMessage).IsAssignableFrom(returnType);
        var isTaskOfMessage = returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            && typeof(IMessage).IsAssignableFrom(returnType.GetGenericArguments()[0]);

        if (!isDirectMessage && !isTaskOfMessage)
            throw new InvalidOperationException(
                $"[AsyncEvent] '{method.Name}' must return an IMessage or Task<IMessage>. Got '{returnType.Name}'.");

        _asyncEvents.TryAdd(attr.EventName, []);
        _asyncEvents[attr.EventName].Add(new(listener, method, paramType));
    }

    // ── Unregister ───────────────────────────────────────────────────────────

    public void UnregisterListener(Listener listener)
    {
        foreach (var list in _signals.Values)     list.RemoveAll(h => h.Instance == listener);
        foreach (var list in _syncEvents.Values)   list.RemoveAll(h => h.Instance == listener);
        foreach (var list in _asyncEvents.Values)  list.RemoveAll(h => h.Instance == listener);
    }

    // ── Emit Signal ──────────────────────────────────────────────────────────

    public void EmitSignal(string eventName, bool networked = true)
        => DispatchSignal(eventName, null, null, networked);

    public void EmitSignal<T>(string eventName, T payload, bool networked = true) where T : IMessage
        => DispatchSignal(eventName, payload, typeof(T), networked);

    private void DispatchSignal(string eventName, IMessage? payload, Type? payloadType, bool networked = true)
    {
        if (eventName.StartsWith('@') && networked)
        {
            if (_networkDispatcher is null)
                throw new InvalidOperationException("Network dispatcher not set.");
            var frame = BuildFrame(eventName, payload, NetworkEventFrameType.Signal);
            _networkDispatcher.DispatchSignal(eventName, frame, EvaSystem.Singleton<ITypeRegistration>().Registry);
            return;
        }

        if (!_signals.TryGetValue(eventName, out var handlers)) return;

        foreach (var (instance, method, paramType) in handlers)
        {
            if (paramType is null) { method.Invoke(instance, []); continue; }

            if (payload is null)
                throw new InvalidOperationException(
                    $"Handler '{method.Name}' expects '{paramType.Name}' but signal sent no payload.");

            if (!paramType.IsAssignableFrom(payloadType))
                throw new InvalidOperationException(
                    $"Handler '{method.Name}' expects '{paramType.Name}' but got '{payloadType!.Name}'.");

            method.Invoke(instance, [payload]);
        }
    }

    // ── Emit Sync ────────────────────────────────────────────────────────────

    public async Task<T?> EmitSync<T>(string eventName, T payload, bool networked = true) where T : IMessage
    {
        if (eventName.StartsWith('@') && networked)
        {
            if (_networkDispatcher is null)
                throw new InvalidOperationException("Network dispatcher not set.");
            var frame = BuildFrame(eventName, payload, NetworkEventFrameType.Request);
            return (T?) await _networkDispatcher.DispatchSyncAsync(eventName, frame, EvaSystem.Singleton<ITypeRegistration>().Registry);
        }

        if (!_syncEvents.TryGetValue(eventName, out var handlers)) return payload;

        IMessage? current = payload;

        foreach (var (instance, method, paramType, _) in handlers)
        {
            if (!paramType.IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException(
                    $"[SyncEvent] '{method.Name}' expects '{paramType.Name}' but got '{typeof(T).Name}'.");

            var curr = (IMessage?)method.Invoke(instance, [current]);

            if (curr is null) throw new CancellationException(current);
            current = curr;
        }

        return (T?)current;
    }

    // ── Emit Async ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<T>> EmitAsync<T>(string eventName, T payload, bool networked = true) where T : IMessage
    {
        if (eventName.StartsWith('@') && networked)
        {
            if (_networkDispatcher is null)
                throw new InvalidOperationException("Network dispatcher not set.");
            var frame = BuildFrame(eventName, payload, NetworkEventFrameType.Request);
            var resultss = await _networkDispatcher.DispatchAsync(eventName, frame, EvaSystem.Singleton<ITypeRegistration>().Registry);
            return resultss.Cast<T>().ToList();
        }
 
        if (!_asyncEvents.TryGetValue(eventName, out var handlers)) return [];
 
        var tasks = handlers
            .Where(h => h.ParamType.IsAssignableFrom(typeof(T)))
            .Select(h => Task.Run(async () =>
            {
                var raw = h.Method.Invoke(h.Instance, new object?[] { payload });

                if (raw is Task<T> taskT)
                    return await taskT;

                if (raw is T msg)
                    return msg;

                if (raw is Task task)
                {
                    await task;
                }

                return default(T);
            }));
 
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).Cast<T>().ToList();
    }
    
    public void EmitSignal(string eventName, IMessage payload, bool networked = true)
        => DispatchSignal(eventName, payload, payload.GetType(), networked);
    

    public async Task<IMessage?> HandleNetworkRequestAsync(string eventName, NetworkEventFrame frame)
    {
        // Sync handlers en priorité
        if (_syncEvents.ContainsKey(eventName))
        {
            IMessage? payload = DeserializeFrame(frame);
            if (payload is null) return null;
            return EmitSyncRaw(eventName, payload);
        }

        // Async handlers sinon
        if (_asyncEvents.ContainsKey(eventName))
        {
            IMessage? payload = DeserializeFrame(frame);
            if (payload is null) return null;
            var results = await EmitAsyncRaw(eventName, payload);
            return results.FirstOrDefault();
        }

        return null;
    }
    private IMessage? EmitSyncRaw(string eventName, IMessage payload)
    {
        if (!_syncEvents.TryGetValue(eventName, out var handlers)) return payload;

        IMessage? current = payload;

        foreach (var (instance, method, paramType, _) in handlers)
        {
            if (!paramType.IsAssignableFrom(payload.GetType()))
                throw new InvalidOperationException(
                    $"[SyncEvent] '{method.Name}' expects '{paramType.Name}' " +
                    $"but got '{payload.GetType().Name}'.");

            var result = (IMessage?)method.Invoke(instance, [current]);
            if (result is null) throw new CancellationException(current);
            current = result;
        }

        return current;
    }

    private async Task<IReadOnlyList<IMessage>> EmitAsyncRaw(string eventName, IMessage payload)
    {
        if (!_asyncEvents.TryGetValue(eventName, out var handlers)) return [];

        var tasks = handlers
            .Where(h => h.ParamType.IsAssignableFrom(payload.GetType()))
            .Select(h => Task.Run(async () =>
            {
                var raw = h.Method.Invoke(h.Instance, [payload]);

                if (raw is Task<IMessage> taskMsg)
                    return await taskMsg;

                if (raw is IMessage msg)
                    return msg;

                if (raw is Task task)
                    await task;

                return (IMessage?)null;
            }));

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).Cast<IMessage>().ToList();
    }

    private IMessage? DeserializeFrame(NetworkEventFrame frame)
    {
        if (string.IsNullOrEmpty(frame.TypeUrl) || frame.Payload.IsEmpty) return null;
        var descriptor = EvaSystem.Singleton<ITypeRegistration>().Registry.Find(frame.TypeUrl);
        return descriptor?.Parser.ParseFrom(frame.Payload);
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _signals.Clear();
        _syncEvents.Clear();
        _asyncEvents.Clear();
    }
}