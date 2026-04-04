using System.Text;
using System.Text.Json;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.User;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Google.Protobuf;
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
        string publicKey = (string)obj["pub"];

        try
        {
            var user = EvaSystem.Singleton<IUserAuthenticator>().Login(username, code);
            var cert = EvaSystem.Singleton<ICertificateManager>().GenerateCertificate(user, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600, publicKey);
            
            HttpContext.Response.StatusCode = 200;
            return new {code=200, cert=cert.EntityCertificateUnit.ToByteString().ToBase64(), eas=cert.AuthorityCertificateUnit.ToByteString().ToBase64(), pub=EvaSystem.Singleton<IKeysManager>().PublicKeyBase64};
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
            var body = ConnectionUtil.GetCertificate(HttpContext);
            var cert = CertificateUtil.ParseCertificateBase64(body);
            if (!CertificateUtil.CheckCertificate(cert, EvaSystem.Singleton<IKeysManager>().PublicKeyBase64))
            {
                throw new Exception("Invalid token or expirated");
            }
        
            HttpContext.Response.StatusCode = 200;
            return new {code=200,certificate=JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(JsonFormatter.Default.Format(cert))))};
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return new {code = 403, message = e.Message};
        }
    }
    
}