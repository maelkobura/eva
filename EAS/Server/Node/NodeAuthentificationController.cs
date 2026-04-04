using System.Security.Cryptography.X509Certificates;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.Security.TLS;
using Eva.AuthorityServer.User;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Eva.AuthorityServer.Server.Node;

public class NodeAuthentificationController : WebApiController{
    
    [Route(HttpVerbs.Post, "/")]
    public async Task<Object> Authentificate()
    {
        var body = await HttpContext.GetRequestBodyAsStringAsync();
        
        JObject obj = JObject.Parse(body);

        string serviceName = (string)obj["service"];
        string token = (string)obj["token"];
        string publicKey = (string)obj["publickey"];

        try
        {
            var nodeContract = EvaSystem.Singleton<INodeRegistry>().GetContractByNameAndValidate(serviceName, token);
            var cert = EvaSystem.Singleton<ICertificateManager>().GenerateCertificate(nodeContract, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600, publicKey);
            
            HttpContext.Response.StatusCode = 200;
            return new {code=200, cert=cert.EntityCertificateUnit.ToByteString().ToBase64(), eas=cert.AuthorityCertificateUnit.ToByteString().ToBase64(), pub=EvaSystem.Singleton<IKeysManager>().PublicKeyBase64};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }

    [Route(HttpVerbs.Get, "/tls")]
    public async Task<Object> GenerateTLSCert()
    {
        try
        {
            var body = ConnectionUtil.GetCertificate(HttpContext);
            var cert = CertificateUtil.ParseCertificateBase64(body);
            if (!CertificateUtil.CheckCertificate(cert, EvaSystem.Singleton<IKeysManager>().PublicKeyBase64))
            {
                throw new Exception("Invalid token or expirated");
            }

            var contract = EvaSystem.Singleton<INodeRegistry>().GetContractByName(cert!.Payload.Content.Subject);
            
            
            
            var nodeCert = CAManager.Instance!.IssueNodeCertificate(contract.Host);
            
            HttpContext.Response.StatusCode = 200;
            return new {code=200,nodeCert=Convert.ToBase64String(nodeCert.Export(X509ContentType.Pfx)),easCert=Convert.ToBase64String(CAManager.Instance.CA!.Export(X509ContentType.Cert))};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }
    
}