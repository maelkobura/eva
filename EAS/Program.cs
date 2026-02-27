using Eva.AuthorityServer.Security;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.AuthorityServer;

class Program
{
    static void Main(string[] args)
    {
        EvaLogger.Init();
        var log = EvaLogger.CreateLogger<Program>();
        log.LogInformation("Initializing EAS Systems...");
        
        KeysManager.Init();
        
    }
}