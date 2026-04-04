using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Eva.Commons.Messages;
using Eva.Commons.Security.Certificate;
using Eva.Node.Service.Functions;
using Google.Protobuf;

namespace Eva.Node.Network;

public class FunctionsController : WebApiController{
    
    [Route(HttpVerbs.Get, "/")]
    public async Task GetFunctions()
    {
        var panel = FunctionRegistry.Instance.GetPanel();
        var bytes = panel.ToByteArray();
        HttpContext.Response.ContentType = "application/x-protobuf";
        HttpContext.Response.ContentLength64 = bytes.Length;
        await HttpContext.Response.OutputStream.WriteAsync(bytes);
    }
    
    [Route(HttpVerbs.Get, "/{name}")]
    public async Task GetFunction(string name)
    {
        var reg = FunctionRegistry.Instance!;
        var func = reg.Get(name);
        if(func == null) throw new Exception("Function not found");
        var responseBytes = reg.GetDescriptor(func).ToByteArray();
        HttpContext.Response.ContentType = "application/x-protobuf";
        HttpContext.Response.ContentLength64 = responseBytes.Length;
        await HttpContext.Response.OutputStream.WriteAsync(responseBytes);
    }
    [Route(HttpVerbs.Post, "/{name}")]
    public async Task InvokeFunction(string name)
    {
        // Read raw bytes from body
        using var ms = new MemoryStream();
        await HttpContext.Request.InputStream.CopyToAsync(ms);
        var requestBytes = ms.ToArray();

        // Deserialize InvokeRequest from protobuf
        var request = InvokeRequest.Parser.ParseFrom(requestBytes);

        var cert = HttpContext.GetItem<Certificate>("certificate");

        // Create executor
        var executor = FunctionRegistry.Instance.CreateExecutor(name);
        if (executor is null)
        {
            await HttpContext.SendStandardHtmlAsync(404);
            return;
        }

        // Execute
        var response = await executor.ExecuteAsync(request, cert);

        // Serialize response to protobuf and send
        var responseBytes = response.ToByteArray();
        HttpContext.Response.ContentType = "application/x-protobuf";
        HttpContext.Response.ContentLength64 = responseBytes.Length;
        await HttpContext.Response.OutputStream.WriteAsync(responseBytes);
    }
    
    
    
}