using System.Reflection;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.AuthorityServer.Nodes;
using Eva.Commons.System;

namespace Eva.AuthorityServer.Server;

public class RootController : WebApiController{
    
    [Route(HttpVerbs.Get, "/")]
    public object ServerInfo()
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
    
    [Route(HttpVerbs.Get, "/nodes")] 
    public object GetNodes()
    {
        Dictionary<string, string> nodes = new Dictionary<string, string>();
        foreach (var contract in EvaSystem.Singleton<INodeRegistry>().NodeContracts ?? new())
        {
            nodes.Add(contract.Name, contract.Host + ":" + contract.Port);
        }
        return nodes;
    }
    
    
    
}