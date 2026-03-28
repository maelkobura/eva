using EmbedIO;
using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class NetworkManager
{
    public static NetworkManager? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<NetworkManager>();
    
    private WebServer server;

    public static void Init()
    {
        if (Instance != null) return;
        Instance = new NetworkManager();
    }

    public NetworkManager()
    {
        
        bool enableTls = Configuration.Content["debug:skip-tls"] != "true";
        
        if(!enableTls)
        {
            logger.LogWarning("TLS Server is disabled. Be careful to activate in production environment");
        }
        
        Swan.Logging.Logger.NoLogging();
        server = new WebServer(o => o
                .WithUrlPrefix($"http{(!enableTls ? "" : "s")}://localhost:{Configuration.Content["network:self:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO)
                .WithCertificate(!enableTls ? null : CertificateManager.Instance.TlsNodeCertificate))
            .WithLocalSessionManager()
            .WithModule(new HandshakeRoute("/handshake", true));
    }

    public void Start()
    {
        server.Start();
        logger.LogInformation("Server started on {}", server.Options.UrlPrefixes[0]);
    }
}