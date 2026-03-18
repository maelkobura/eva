using System.Reflection;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace Eva.AuthorityServer.Server;

public class RootController : WebApiController{
    
    [Route(HttpVerbs.Get, "/")]
    public async Task<object> ServerInfo()
    {
        var ass = Assembly.GetExecutingAssembly();
        var md = ass.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value);
        return new
        {
            name= md["Name"],
            version= md["Version"],
            versionType= md["Version Info"],
            author= md["Author"],
            url= md["URL"]
        };
    }
    
}