using Eva.Commons.Util;
using Eva.Node.Service;
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
}