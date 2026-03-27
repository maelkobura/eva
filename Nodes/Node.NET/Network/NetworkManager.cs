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
        Swan.Logging.Logger.NoLogging();
        server = new WebServer(o => o
                .WithUrlPrefix($"https://localhost:{Configuration.Content["network:self:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO)
                .WithCertificate(CertificateManager.Instance.TlsNodeCertificate))
            .WithLocalSessionManager()
            .WithModule(new HandshakeRoute("/handshake", true));
    }

    public void Start()
    {
        server.Start();
        logger.LogInformation("Server started on {}", server.Options.UrlPrefixes[0]);
    }
}