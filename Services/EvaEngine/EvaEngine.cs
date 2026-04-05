using Eva.Commons.Util;
using Eva.Node.Configuration;
using Eva.Node.Network;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Logging;

namespace Eva.Services.Engine;

public class EvaEngine : EvaService {
    
    private static ILogger logger = EvaLogger.CreateLogger<EvaEngine>();

    [Configurationable]
    public Configuration<EngineConfiguration> Config;
    
    public void Initialize()
    {
        logger.LogInformation("Hello from EvaEngine (Assistant: " + Config.Value.AssistantName + ")");
    }

    public void Shutdown()
    {
        logger.LogInformation("Goodbye from EvaEngine");
    }
    
    [EvaFunction(Description = "Prompt Eva", Keywords = new[] { "ai", "runtime" })]
    public string Prompt(string username)
    {
        return "[" + Config.Value.AssistantName + "] " + EvaServices.Call<string>("user.get_user_data", username).Result;
    }
}