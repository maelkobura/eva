using Eva.Commons.Util;
using Eva.Node.Network;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Logging;

namespace Eva.Services.Engine;

public class EvaEngine : EvaService {
    
    private static ILogger logger = EvaLogger.CreateLogger<EvaEngine>();
    
    public void Initialize()
    {
        logger.LogInformation("Hello from EvaEngine");
    }

    public void Shutdown()
    {
        logger.LogInformation("Goodbye from EvaEngine");
    }
    
    [EvaFunction(Description = "Prompt Eva", Keywords = new[] { "ai", "runtime" })]
    public string Prompt(string username)
    {
        return "[EVA] " + EvaServices.Call<string>("user.get_user_data", username).Result;
    }
}