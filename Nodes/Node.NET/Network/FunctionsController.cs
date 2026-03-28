using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.Node.Service.Functions;
using Google.Protobuf;

namespace Eva.Node.Network;

public class FunctionsController : WebApiController{
    
    [Route(HttpVerbs.Get, "/")]
    public object GetFunctions()
    {
        return Convert.ToBase64String(FunctionRegistry.Instance!.GetPanel().ToByteArray());
    }
    
}