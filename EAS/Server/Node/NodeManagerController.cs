using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Nodes;
using Eva.Commons.Security.Certificate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Eva.AuthorityServer.Server.Node;

public class NodeManagerController : WebApiController {
    
    [Route(HttpVerbs.Get, "/")]
    public async Task<string> GetNodes()
    {
        return "success";
    }
    
    [Route(HttpVerbs.Post,"/")]
    public async Task<Object> AddNode()
    {
        if (!HttpContext.GetItem<CertificateEntity>("certificate").Authorization.Contains("*")) //TODO: Make integration with perms "admin.node.register"
        {
            HttpContext.Response.StatusCode = 401;
            return new {code = 401, message = "Unauthorized"};
        }
        var body = await HttpContext.GetRequestBodyAsStringAsync();
        JObject obj = JObject.Parse(body);
        
        string? name = (string)obj["name"];
        string[]? auth = obj["authorization"]?.ToObject<string[]>();
        
        // TODO: Check if user have same permission than register request

        try
        {
            if (name == null || auth == null)
            {
                throw new Exception("Invalid request");
            }
            NodeRegistry.Instance?.CreateContract(name, auth);
        }catch(Exception e)
        {
            HttpContext.Response.StatusCode = 500;
            return new {code = 500, message = e.Message};
        }
        HttpContext.Response.StatusCode = 200;
        return new {code = 200, message = "Node created", name};
        
    }
    
    
    
}