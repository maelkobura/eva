
using System.Reflection;
using System.Runtime.InteropServices;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.Security.TLS;
using Eva.AuthorityServer.Server;
using Eva.AuthorityServer.System;
using Eva.AuthorityServer.User;
using Eva.Commons.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace Eva.AuthorityServer;

class Program
{
    static void Main(string[] args)
    {
        string configPath = "conf.yml";
        string nodeDir = "Contract";
        var configOverride = new Dictionary<string, string>();
        
        var options = new OptionSet {
            { "c|config=", "Config path", n => configPath = n },
            { "n|nodes=", "Directory of Node Contract", n => nodeDir = n },
            { "p|prop=:", "Property key value", (key, value) =>
                {
                    configOverride[key] = value;
                }
            }
        };
        
        try
        {
            options.Parse(args);
        }
        catch (OptionException e)
        {
            Console.WriteLine($"Failed to read options: {e.Message}");
            return;
        }
        
        
        EvaLogger.Init("EAS");
        var log = EvaLogger.CreateLogger<Program>();
        log.LogInformation("Initializing EAS Systems...");
        log.LogInformation("Current Runtime: {}", RuntimeInformation.FrameworkDescription);
        
        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Eva.AuthorityServer.config.default.yml");
        Configuration.Init(configPath, configOverride, templateStream);

        EvaSystem.AddSingleton<IKeysManager,InternalKeysManager>();
        EvaSystem.AddSingleton<IUserAuthenticator,InternalUserAuthenticator>();
        CertificateManager.Init(EvaSystem.Singleton<IKeysManager>().PublicKeyBase64, EvaSystem.Singleton<IKeysManager>().PrivateKeyBase64);
        NodeRegistry.Init(nodeDir);
        //PermissionsManager
        EvaSystem.AddSingleton<IAuthorityServerManager, InternalAuthorityServerManager>();
        CAManager.Init();
        CAManager.Instance!.GenerateCA();
        
        EvaSystem.Singleton<IAuthorityServerManager>().Start();
        Console.ReadKey(true);
    }
}