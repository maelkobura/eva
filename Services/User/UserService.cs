using Eva.Commons.Util;
using Eva.Node.Service;
using Microsoft.Extensions.Logging;

namespace Eva.Services.User;

public class UserService : EvaService {
    
    private static ILogger logger = EvaLogger.CreateLogger<UserService>();
    
    public void Initialize()
    {
        logger.LogInformation("Hello from UserService");
    }

    public void Shutdown()
    {
        logger.LogInformation("Goodbye from UserService");
    }
}