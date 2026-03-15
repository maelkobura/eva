using System.Text;
using EmbedIO;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.System;
using Eva.Commons.Security.Certificate;
using Newtonsoft.Json;

namespace Eva.AuthorityServer.Server.Middleware;

public class AuthentificationMiddleware : WebModuleBase{
    
    private readonly List<string> _excludedPaths = new List<string>();
    public AuthentificationMiddleware(string baseRoute) : base(baseRoute)
    {
        _excludedPaths.Add("/user/auth");
        _excludedPaths.Add("/node/auth");
    }

    protected override async Task OnRequestAsync(IHttpContext ctx)
    {
        try
        {
            Console.WriteLine(ctx.RequestedPath);
            if (Configuration.Content["debug:authentification:skip"] == "true") {
                
                ctx.Items["certificate"] = new CertificateEntity("debug.user", "EVA", CertificateType.User, new string[] { "*" }, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600, true);
                
            } else if (_excludedPaths.Any(p => ctx.RequestedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                
            } else {
                var certRaw = CertificateManager.GetCertificate(ctx);
                var cert = CertificateManager.ValidateCertificate(certRaw, true);
                if (cert == null)
                {
                    throw new Exception("Invalid token or expirated");
                }
                ctx.Items["certificate"] = cert;
            
            }
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            ctx.Response.StatusCode = 403;
            await ctx.SendStringAsync(
                JsonConvert.SerializeObject(new { code = 403, message = e.Message }),
                "application/json",
                Encoding.UTF8
            );
        }
    }

    public override bool IsFinalHandler { get; } = false;
}