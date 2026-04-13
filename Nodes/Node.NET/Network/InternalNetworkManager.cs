using EmbedIO;
using EmbedIO.WebApi;
using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Eva.Node.Events.Dispatcher;
using Eva.Node.Network.Event;
using Eva.Node.Network.Middleware;
using Eva.Node.Network.RemoteTerminal;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class InternalNetworkManager : INetworkManager, INetworkEventDispatcher
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<InternalNetworkManager>();

    private readonly WebServer _server;
    private readonly NetworkEventRoute _eventRoute;
    
    public InternalNetworkManager()
    {
        bool enableTls = SystemConfiguration.Content["debug:skip-tls"] != "true";

        if (!enableTls)
        {
            logger.LogWarning("TLS Server is disabled. Be careful to activate in production environment");
        }

        Swan.Logging.Logger.NoLogging();
        
        _eventRoute = new NetworkEventRoute("/evnt");
        
        _server = new WebServer(o => o
                .WithUrlPrefix($"http{(!enableTls ? "" : "s")}://localhost:{SystemConfiguration.Content["network:self:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO)
                .WithCertificate(!enableTls ? null : EvaSystem.Singleton<ICertificateManager>().TlsNodeCertificate))
            .WithLocalSessionManager()
            .WithModule(new AuthentificationMiddleware("/"))
            .WithModule(new HandshakeRoute("/handshake", true))
            .WithModule(new RemoteTerminalRoute("/terms", true))
            .WithModule(_eventRoute)
            .WithWebApi("/funcs", o => o.WithController<FunctionsController>());
    }

    public void Start()
    {
        _server.Start();
        logger.LogInformation("Server started on {Url}", _server.Options.UrlPrefixes[0]);
    }

    public void Stop()
    {
        _server.Dispose();
        logger.LogInformation("Server stopped");
    }

    public void Dispose()
    {
        Stop();
    }
    
    public async void DispatchSignal(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry)
        => await _eventRoute.PushAsync(eventName, frame);

    public async Task<IMessage?> DispatchSyncAsync(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry)
    {
        var frames = await _eventRoute.RequestAsync(eventName, frame, NetworkEventType.Sync);
        var first = frames.FirstOrDefault();
        if (first is null || first.Payload.IsEmpty) return null;

        var descriptor = typeRegistry.Find(first.TypeUrl);
        return descriptor?.Parser.ParseFrom(first.Payload);
    }

    public async Task<IReadOnlyList<IMessage>> DispatchAsync(string eventName, NetworkEventFrame frame, TypeRegistry typeRegistry)
    {
        var frames = await _eventRoute.RequestAsync(eventName, frame, NetworkEventType.Async);

        return frames
            .Where(f => !f.Payload.IsEmpty)
            .Select(f =>
            {
                var descriptor = typeRegistry.Find(f.TypeUrl);
                return descriptor?.Parser.ParseFrom(f.Payload);
            })
            .Where(m => m is not null)
            .Cast<IMessage>()
            .ToList();
    }
}