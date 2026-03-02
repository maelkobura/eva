using System.Text.Json;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Eva.AuthorityServer.Server.User;

public class UserController : WebApiController {
    
    [Route(HttpVerbs.Post, "/auth")]
    public async Task<string> PostJsonData()
    {
        var body = await HttpContext.GetRequestBodyAsStringAsync();
        
        JObject obj = JObject.Parse(body);
        var ser = JsonSerializer.Create();

        string username = (string)obj["username"];
        string code = (string)obj["code"];

        try
        {
            var user = UserAuthenticator.Login(username, code);
            
            HttpContext.Response.StatusCode = 200;
            string cert = CertificateManager.GenerateCertificateForUser(user, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);
            return cert;
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 403;
            return JsonConvert.SerializeObject(new {code = 403, message = e.Message});
        }
    }
    
}