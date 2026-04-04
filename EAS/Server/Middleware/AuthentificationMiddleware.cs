using System.Text;
using EmbedIO;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.System;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
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
            if (Configuration.Content["debug:authentification:skip"] == "true")
            {

                ctx.Items["certificate"] = null; //TODO Create a real debug cert

            } else if (_excludedPaths.Any(p => ctx.RequestedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                
            } else {
                var certRaw = ConnectionUtil.GetCertificate(ctx);
                var cert = CertificateUtil.ParseCertificateBase64(certRaw);
                if (!CertificateUtil.CheckCertificate(cert, EvaSystem.Singleton<IKeysManager>().PublicKeyBase64))
                {
                    throw new Exception("Invalid token or expirated");
                }
                ctx.Items["certificate"] = cert;
            
            }
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
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