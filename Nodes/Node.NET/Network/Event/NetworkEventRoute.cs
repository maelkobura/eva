using EmbedIO.WebSockets;
using Eva.Commons.Events;
using Eva.Commons.Util;
using Eva.Node.Events.Bus;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network.Event;

public class NetworkEventRoute : WebSocketModule
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<NetworkEventRoute>();
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, List<IWebSocketContext>> _connections = new();
    private readonly Dictionary<string, TaskCompletionSource<NetworkEventFrame>> _pending = new();
    private readonly Lock _lock = new();
    

    public NetworkEventRoute(string baseRoute) : base(baseRoute, true) { }

    // ── Connection lifecycle ─────────────────────────────────────────────────

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        var eventName = ParseEventName(context);
        Console.WriteLine(eventName);
        if (string.IsNullOrEmpty(eventName))
        {
            logger.LogWarning("WebSocket connection rejected: no event name in URL");
            return context.WebSocket.CloseAsync();
        }

        lock (_lock)
        {
            _connections.TryAdd(eventName, []);
            _connections[eventName].Add(context);
        }

        logger.LogInformation("Node subscribed to event '{Event}'", eventName);
        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
    {
        lock (_lock)
        {
            foreach (var list in _connections.Values)
                list.Remove(context);
        }

        logger.LogInformation("Node unsubscribed (connection closed)");
        return Task.CompletedTask;
    }

    // ── Incoming messages (responses from remote subscribers) ───────────────

    protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        var frame = NetworkEventFrame.Parser.ParseFrom(buffer[..result.Count]);
        if (frame.FrameType != NetworkEventFrameType.Response) return Task.CompletedTask;

        lock (_lock)
        {
            if (_pending.TryGetValue(frame.CorrelationId, out var tcs))
                tcs.TrySetResult(frame);
        }

        return Task.CompletedTask;
    }

    // ── Push (Signal) ────────────────────────────────────────────────────────

    public async Task PushAsync(string eventName, NetworkEventFrame frame)
    {
        var targets = GetTargets(eventName);
        if (targets.Count == 0) return;

        var data = frame.ToByteArray();
        var dead = new List<IWebSocketContext>();

        await Task.WhenAll(targets.Select(async ctx =>
        {
            try { await ctx.WebSocket.SendAsync(data, true); }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to push event '{Event}': {Error}", eventName, ex.Message);
                dead.Add(ctx);
            }
        }));

        RemoveDead(eventName, dead);
    }

    // ── Request (Sync / Async) ───────────────────────────────────────────────

    public async Task<IReadOnlyList<NetworkEventFrame>> RequestAsync(string eventName, NetworkEventFrame frame, NetworkEventType type)
    {
        var targets = GetTargets(eventName);
        Console.WriteLine("nber of target: " + targets.Count);
        if (targets.Count == 0) return [];

        var tasks = targets.Select(ctx => RequestOneAsync(ctx, eventName, frame, type));
        var results = await Task.WhenAll(tasks);

        return results.Where(r => r is not null).Cast<NetworkEventFrame>().ToList();
    }

    private async Task<NetworkEventFrame?> RequestOneAsync(IWebSocketContext ctx, string eventName,
        NetworkEventFrame frame, NetworkEventType type)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<NetworkEventFrame>();

        var outFrame = new NetworkEventFrame
        {
            TypeUrl       = frame.TypeUrl,
            Payload       = frame.Payload,
            FrameType     = NetworkEventFrameType.Request,
            CorrelationId = correlationId,
            EventType = type
        };

        lock (_lock) _pending[correlationId] = tcs;

        try
        {
            await ctx.WebSocket.SendAsync(outFrame.ToByteArray(), true);

            using var cts = new CancellationTokenSource(ResponseTimeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Timeout waiting for response to event '{Event}'", eventName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to request event '{Event}': {Error}", eventName, ex.Message);
            return null;
        }
        finally
        {
            lock (_lock) _pending.Remove(correlationId);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private List<IWebSocketContext> GetTargets(string eventName)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(eventName, out var list) && list.Count > 0
                ? [..list]
                : [];
        }
    }

    private void RemoveDead(string eventName, List<IWebSocketContext> dead)
    {
        if (dead.Count == 0) return;
        lock (_lock)
        {
            if (!_connections.TryGetValue(eventName, out var list)) return;
            foreach (var ctx in dead) list.Remove(ctx);
        }
    }

    private static string? ParseEventName(IWebSocketContext context)
        => context.Headers["X-Event-Name"];
}