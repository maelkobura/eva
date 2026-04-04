using EmbedIO;
using EmbedIO.WebApi;
using Eva.AuthorityServer.System;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Eva.Node.Network.Middleware;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class InternalNetworkManager : INetworkManager
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<InternalNetworkManager>();

    private readonly WebServer _server;

    // === Conserver exactement le constructeur tel quel ===
    public InternalNetworkManager()
    {
        bool enableTls = Configuration.Content["debug:skip-tls"] != "true";

        if (!enableTls)
        {
            logger.LogWarning("TLS Server is disabled. Be careful to activate in production environment");
        }

        Swan.Logging.Logger.NoLogging();
        _server = new WebServer(o => o
                .WithUrlPrefix($"http{(!enableTls ? "" : "s")}://localhost:{Configuration.Content["network:self:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO)
                .WithCertificate(!enableTls ? null : EvaSystem.Singleton<ICertificateManager>().TlsNodeCertificate))
            .WithLocalSessionManager()
            .WithModule(new AuthentificationMiddleware("/"))
            .WithModule(new HandshakeRoute("/handshake", true))
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
}