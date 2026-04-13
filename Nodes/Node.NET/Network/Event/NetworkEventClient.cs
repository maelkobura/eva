using System.Net.WebSockets;
using Eva.Commons.Events;
using Eva.Commons.Util;
using Eva.Node.Events.Bus;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Swan;

namespace Eva.Node.Network.Event;

public class NetworkEventClient : IAsyncDisposable
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<NetworkEventClient>();
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(2);

    private readonly string _nodeUrl;
    private readonly NodeEntity _node;
    private readonly string _eventName;
    private readonly IEventBus _eventBus;
    private readonly TypeRegistry _typeRegistry;
    private readonly CancellationTokenSource _cts = new();

    public NetworkEventClient(NodeEntity node, string nodeUrl, string eventName, IEventBus eventBus, TypeRegistry typeRegistry)
    {
        _nodeUrl      = nodeUrl;
        _node         = node;
        _eventName    = eventName;
        _eventBus     = eventBus;
        _typeRegistry = typeRegistry;
    }

    public Task StartAsync() => Task.Run(() => RunAsync(_cts.Token));

    private async Task RunAsync(CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(2);

        while (!ct.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();

            try
            {
                //TODO Tls
                var uri = new Uri($"ws://{_nodeUrl}/evnt");
                ws.Options.SetRequestHeader("X-Event-Name", _eventName);
                ws.Options.SetRequestHeader("Authorization", $"Bearer {_node.NodeTrustCertificate.ToByteArray().ToBase64()}");
                await ws.ConnectAsync(uri, ct);

                logger.LogInformation("Subscribed to event '{Event}' on {Node}", _eventName, _nodeUrl);
                delay = TimeSpan.FromSeconds(2);

                await ReceiveLoopAsync(ws, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Connection to '{Event}' on {Node} lost: {Error}. Retrying in {Delay}s",
                    _eventName, _nodeUrl, ex.Message, delay.TotalSeconds);
            }

            await Task.Delay(delay, ct);
            delay = delay * 2 > MaxRetryDelay ? MaxRetryDelay : delay * 2;
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) return;
                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var frame = NetworkEventFrame.Parser.ParseFrom(ms.ToArray());
            
            switch (frame.FrameType)
            {
                case NetworkEventFrameType.Signal:
                    DispatchSignal(frame);
                    break;

                case NetworkEventFrameType.Request:
                    var response = await HandleRequestAsync(frame);
                    await ws.SendAsync(response.ToByteArray(), WebSocketMessageType.Binary, true, ct);
                    break;
            }
        }
    }

    // ── Signal ───────────────────────────────────────────────────────────────

    private void DispatchSignal(NetworkEventFrame frame)
    {
        if (string.IsNullOrEmpty(frame.TypeUrl))
        {
            _eventBus.EmitSignal(_eventName);
            return;
        }

        var message = DeserializePayload(frame);
        if (message is null) return;

        _eventBus.EmitSignal(_eventName, message, false);
    }

    // ── Request (Sync / Async) ───────────────────────────────────────────────

    private async Task<NetworkEventFrame> HandleRequestAsync(NetworkEventFrame frame)
    {
        logger.LogInformation("New event emitted from " + frame.TypeUrl);
        
        IMessage? result = null;

        try
        {
            var payload = DeserializePayload(frame);
            
            if (payload is not null)
            {
                var concreteType = payload.GetType();
                switch (frame.EventType)
                {
                    case NetworkEventType.Sync:
                        var syncMethod = _eventBus.GetType()
                            .GetMethod(nameof(IEventBus.EmitSync))!
                            .MakeGenericMethod(concreteType);
                        var syncTask = (Task)syncMethod.Invoke(_eventBus, [_eventName, payload, false])!;
                        await syncTask;
                        result = ((dynamic)syncTask).Result as IMessage;
                        break;
                    case NetworkEventType.Async:
                        var asyncMethod = _eventBus.GetType()
                            .GetMethod(nameof(IEventBus.EmitAsync))!
                            .MakeGenericMethod(concreteType);
                        var asyncTask = (Task)asyncMethod.Invoke(_eventBus, [_eventName, payload, false])!;
                        await asyncTask;
                        var asyncResults = ((dynamic)asyncTask).Result as IEnumerable<IMessage>;
                        result = asyncResults?.FirstOrDefault();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Error handling request for event '{Event}': {Error}", _eventName, ex.Message);
        }

        return new NetworkEventFrame
        {
            FrameType     = NetworkEventFrameType.Response,
            CorrelationId = frame.CorrelationId,
            TypeUrl       = result?.Descriptor.FullName ?? "",
            Payload       = result?.ToByteString() ?? ByteString.Empty
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IMessage? DeserializePayload(NetworkEventFrame frame)
    {
        if (string.IsNullOrEmpty(frame.TypeUrl) || frame.Payload.IsEmpty) return null;

        var descriptor = _typeRegistry.Find(frame.TypeUrl);
        if (descriptor is null)
        {
            logger.LogWarning("Unknown type '{TypeUrl}' for event '{Event}'", frame.TypeUrl, _eventName);
            return null;
        }

        return descriptor.Parser.ParseFrom(frame.Payload);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}