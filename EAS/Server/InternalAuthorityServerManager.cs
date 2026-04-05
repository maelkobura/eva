using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.WebSockets;
using Eva.AuthorityServer.Server.Middleware;
using Eva.AuthorityServer.Server.Node;
using Eva.AuthorityServer.Server.User;
using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Diagnostics.Internal;
using Swan.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Eva.AuthorityServer.Server;

public class InternalAuthorityServerManager : IAuthorityServerManager {
    private static ILogger logger = EvaLogger.CreateLogger<InternalAuthorityServerManager>();

    private WebServer server;
    
    public InternalAuthorityServerManager()
    {
        Swan.Logging.Logger.NoLogging();

        server = new WebServer(o => o
                .WithUrlPrefix($"http://localhost:{SystemConfiguration.Content["server:port"]}/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new AuthentificationMiddleware("/"))
            .WithWebApi("/node/manage", o => o.WithController<NodeManagerController>())
            .WithWebApi("/node/auth", o => o.WithController<NodeAuthentificationController>())
            .WithWebApi("/user/auth", o => o.WithController<UserAuthentificationController>())
            .WithWebApi("/", o => o.WithController<RootController>());
        
    }

    public void Start()
    {
        server.Start();
        logger.LogInformation("Server started on {}", server.Options.UrlPrefixes[0]);
    }

    public void Dispose()
    {
        //TODO Server stop
    }
}
