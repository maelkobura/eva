using System.Text;
using System.Text.Json;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.User;
using Eva.Commons.Security.Certificate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Eva.AuthorityServer.Server.User;

public class UserAuthentificationController : WebApiController {
    
    [Route(HttpVerbs.Post, "/")]
    public async Task<Object> Authentificate()
    {
        var body = await HttpContext.GetRequestBodyAsStringAsync();
        
        JObject obj = JObject.Parse(body);
        var ser = JsonSerializer.Create();

        string username = (string)obj["username"];
        string code = (string)obj["code"];

        try
        {
            var user = UserAuthenticator.Login(username, code);
            var cert = CertificateManager.GenerateCertificate(user, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);
            
            HttpContext.Response.StatusCode = 200;
            return new {code=200, cert=cert.Certificate, eas=cert.AuthorityCertificate, prv=cert.PrivateKey};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }
    
    [Route(HttpVerbs.Get, "/validate")]
    public async Task<Object> Validate()
    {
        try
        {
            var body = CertificateManager.GetCertificate(HttpContext);
            CertificateEntity? cert = CertificateManager.ValidateCertificate(body);
            if (cert == null)
            {
                throw new Exception("Invalid token or expirated");
            }
            string name = cert.Name;
            string type = cert.Type.ToString();
            string[] auth = cert.Authorization;
        
            HttpContext.Response.StatusCode = 200;
            return new {code=200,name, type, authorizations=auth};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }
    
}