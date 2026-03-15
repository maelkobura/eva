using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace Eva.AuthorityServer.Server.Node;

public class NodeManagerController : WebApiController {
    
    [Route(HttpVerbs.Get, "/")]
    public async Task<string> GetNodes()
    {
        return "success";
    }
    
}