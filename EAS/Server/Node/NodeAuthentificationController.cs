using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.User;
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

        try
        {
            var nodeContract = NodeRegistry.Instance.GetContractByNameAndValidate(serviceName, token);
            var cert = CertificateManager.GenerateCertificate(nodeContract, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);
            
            HttpContext.Response.StatusCode = 200;
            return new {code=200, cert=cert.Certificate, eas=cert.AuthorityCertificate, prv=cert.PrivateKey};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }
}