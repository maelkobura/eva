using System.Reflection;
using Eva.Commons.Util;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Eva.AuthorityServer.System;

public class Configuration
{

    private static ILogger logger = EvaLogger.CreateLogger<Configuration>();
    public static IConfigurationRoot Content { get; private set; }

    public static bool IsInitialized { get; private set; } = false;

    public static void Init(String configPath, Dictionary<string, string> overrideConfig)
    {
        if (IsInitialized) return;

        logger.LogInformation("Initializing Configuration...");
        
        if (!File.Exists(configPath))
        {
            logger.LogInformation("Configuration file not found, generating default configuration.");
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Eva.AuthorityServer.config.default.yml");
            using var reader = new StreamReader(stream!);
            string defaultYaml = reader.ReadToEnd();
            File.WriteAllText(configPath, defaultYaml);
        }
        
        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Eva.AuthorityServer.config.default.yml");

        var configBuilder = new ConfigurationBuilder()
            .AddYamlStream(templateStream)
            .AddYamlFile(configPath)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrideConfig.ToDictionary(
                kvp => kvp.Key.Replace('.', ':'),
                kvp => kvp.Value
            )!);
        
        Content = configBuilder.Build();
        
        IsInitialized = true;
    }
}