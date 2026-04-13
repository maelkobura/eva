using Eva.Commons.Util;
using Eva.Drivers.Abstractions.Drivers;
using Eva.Drivers.Abstractions.Messages;
using Eva.Drivers.Core;
using Eva.Drivers.Core.Configuration;
using Eva.Node.Configuration;
using Eva.Node.Service;
using Eva.Services.Engine;
using Microsoft.Extensions.Logging;

namespace Eva.Services.AI;

public class AIService : EvaService {
    
    private static ILogger logger = EvaLogger.CreateLogger<EvaEngine>();

    [Configurationable]
    public Configuration<DriverConfiguration> Config;
    
    private DriverLoader _driverLoader;
    
    public void Initialize()
    {
        _driverLoader = new DriverLoader(Config.Value);
        _driverLoader.DriverTypes = new[]
        {
            typeof(LargeLanguageModelDriver)
        };
        _driverLoader.DriverPath = @"K:\Projets\Blume\Eva\Services\AI Service\bin\Debug\net9.0\drivers";
        _driverLoader.Load();
    }

    public void Shutdown()
    {
        _driverLoader.Dispose();
    }
}