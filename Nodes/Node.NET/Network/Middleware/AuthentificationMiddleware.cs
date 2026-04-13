using System.Text;
using EmbedIO;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Newtonsoft.Json;
using Type = Eva.Commons.Security.Certificate.Type;

namespace Eva.Node.Network.Middleware;

public class AuthentificationMiddleware : WebModuleBase{
    
    private readonly List<string> _excludedPaths = new List<string>();
    public AuthentificationMiddleware(string baseRoute) : base(baseRoute)
    {
        _excludedPaths.Add("/handshake");
    }

    protected override async Task OnRequestAsync(IHttpContext ctx)
    {
        try
        {
            if (SystemConfiguration.Content["debug:authentification:skip"] == "true")
            {

                ctx.Items["certificate"] = null; //TODO Create a real debug cert

            } else if (_excludedPaths.Any(p => ctx.RequestedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                
            } else {
                
                var certRaw = ConnectionUtil.GetCertificate(ctx);
                var cert = CertificateUtil.ParseCertificateBase64(certRaw);
                if (cert.Payload.Header.Type == Type.NodeTrust && !CertificateUtil.CheckCertificate(cert,
                        EvaSystem.Singleton<ICertificateManager>().CertificateUnit.Payload.Content.EntityPublicKey))
                {
                    throw new Exception("Invalid token or expirated");
                }
                
                if (ConnectionUtil.IsBorrowCertificate(ctx))
                {
                    var borrowCertRaw = ConnectionUtil.GetBorrowCertificate(ctx);
                    var borrowCert = CertificateUtil.ParseCertificateBase64(borrowCertRaw);
                    
                    if (!CertificateUtil.CheckBorrowCertificate(borrowCert, cert, EvaSystem.Singleton<ICertificateManager>().EasPublicKey))
                    {
                        throw new Exception("Invalid borrow token or expirated");
                    }
                    
                    ctx.Items["certificate"] = borrowCert!;
                    
                }
                else
                {
                    ctx.Items["certificate"] = cert;
                }
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