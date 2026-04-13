using Eva.Commons.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eva.Commons.System;

public class SystemConfiguration
{

    private static ILogger logger = EvaLogger.CreateLogger<SystemConfiguration>();
    public static IConfigurationRoot Content { get; private set; }

    public static bool IsInitialized { get; private set; } = false;

    public static void Init(String configPath, Dictionary<string, string> overrideConfig, Stream? templateStream)
    {
        if (IsInitialized) return;

        logger.LogInformation("Initializing Configuration...");
        if(templateStream == null) logger.LogWarning("No template provided, configuration might be incomplete.");
        
        if (!File.Exists(configPath) && templateStream != null)
        {
            logger.LogInformation("Configuration file not found, generating default configuration.");
            using var reader = new StreamReader(templateStream);
            string defaultYaml = reader.ReadToEnd();
            File.WriteAllText(configPath, defaultYaml);
        }

        var configBuilder = new ConfigurationBuilder();
            
        if(templateStream != null) configBuilder.AddYamlStream(templateStream);
            
        configBuilder.AddYamlFile(configPath)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrideConfig.ToDictionary(
                kvp => kvp.Key.Replace('.', ':'),
                kvp => kvp.Value
            )!);
        
        Content = configBuilder.Build();
        
        IsInitialized = true;
    }
    
#if TEST
    // uniquement visible pour le build de test
    public static void SetContentForTest(IConfigurationRoot config)
    {
        Content = config;
    }
#endif
}