using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.WebSockets;
using Eva.AuthorityServer.Server.Node;
using Eva.AuthorityServer.Server.User;
using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Diagnostics.Internal;
using Swan.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Eva.AuthorityServer.Server;

public class AuthorityServerManager
{
    private static ILogger logger = EvaLogger.CreateLogger<AuthorityServerManager>();
    
    public static bool IsInitialized { get; private set; } = false;

    private static WebServer server;
    
    public static void Init()
    {
        if (IsInitialized) return;
        logger.LogInformation("Initializing AuthorityServerManager...");

        Swan.Logging.Logger.NoLogging();
        
        server = new WebServer(o => o
                .WithUrlPrefix($"http://localhost:{Configuration.Content["server:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new NodeWebSocketHandler("/nodes"))
            .WithWebApi("/user/auth", o => o.WithController<UserAuthentificationController>());
            
        
        
        IsInitialized = true;
    }

    public static void Start()
    {
        server.Start();
        logger.LogInformation("Server started on {}", server.Options.UrlPrefixes[0]);
    }
}
