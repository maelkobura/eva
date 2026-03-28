using Eva.Commons.Util;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Logging;

namespace Eva.Services.User;

public class UserService : EvaService {
    
    private static ILogger logger = EvaLogger.CreateLogger<UserService>();
    
    public void Initialize()
    {
        logger.LogInformation("Hello from UserService");
        FunctionRegistry.Instance!.RegisterLambda("update_user_data", "Update user data", new[] { "user", "data" }, () =>
        {
            //Do nothing
        });
    }

    public void Shutdown()
    {
        logger.LogInformation("Goodbye from UserService");
    }

    [EvaFunction(Description = "Get user data", Keywords = new[] { "user", "data" })]
    public void GetUserData()
    {
        //Do nothing for now
    }
}