using System.Text;
using EmbedIO;
using Eva.AuthorityServer.System;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Type = Eva.Commons.Security.Certificate.Type;

namespace Eva.Node.Network.Middleware;

public class AuthentificationMiddleware : WebModuleBase{
    
    private static readonly ILogger logger = EvaLogger.CreateLogger<AuthentificationMiddleware>();
    
    private readonly List<string> _excludedPaths = new List<string>();
    public AuthentificationMiddleware(string baseRoute) : base(baseRoute)
    {
        _excludedPaths.Add("/handshake");
    }

    protected override async Task OnRequestAsync(IHttpContext ctx)
    {
        try
        {
            if (Configuration.Content["debug:authentification:skip"] == "true")
            {
                ctx.Items["certificate"] = null; //TODO Create a real debug cert
                return;
            }

            if (_excludedPaths.Any(p => ctx.RequestedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return;

            var cert = CertificateUtil.ParseCertificateBase64(ConnectionUtil.GetCertificate(ctx));

            if (cert.Payload.Header.Type == Type.NodeTrust &&
                !CertificateUtil.CheckCertificate(cert, CertificateManager.Instance!.EasPublicKey))
                throw new Exception("Invalid token or expired");

            if (ConnectionUtil.IsBorrowCertificate(ctx))
            {
                var borrowCert = CertificateUtil.ParseCertificateBase64(ConnectionUtil.GetBorrowCertificate(ctx));

                if (!CertificateUtil.CheckBorrowCertificate(borrowCert, cert, CertificateManager.Instance!.EasPublicKey))
                    throw new Exception("Invalid borrow token or expired");

                ctx.Items["certificate"] = borrowCert;
            }
            else
            {
                ctx.Items["certificate"] = cert;
            }
        }
        catch (Exception e)
        {
            logger.LogTrace("Node Authentification denied: " + e.ToString());
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