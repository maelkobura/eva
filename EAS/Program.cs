
using System.Runtime.InteropServices;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.User;
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
        
        var options = new OptionSet {
            { "c|config=", "Config path", n => configPath = n },
            { "n|nodes=", "Directory of Node Contract", n => nodeDir = n },
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
        
        Console.WriteLine($"Config path: {configPath}");
        Console.WriteLine($"Node directory: {nodeDir}");
        
        EvaLogger.Init();
        var log = EvaLogger.CreateLogger<Program>();
        log.LogInformation("Initializing EAS Systems...");
        log.LogInformation("Current Runtime: {}", RuntimeInformation.FrameworkDescription);
        
        //Load Config
        
        KeysManager.Init();
        UserAuthenticator.Init();
        //CertificateManager
        //NodeRegistry
        //PermissionsManager
        //EAS Server
    }
}