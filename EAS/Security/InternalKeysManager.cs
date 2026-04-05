using Eva.AuthorityServer.System;
using Eva.Commons.Security;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace Eva.AuthorityServer.Security;

public class InternalKeysManager : IKeysManager{
    private static ILogger logger = EvaLogger.CreateLogger<InternalKeysManager>();

    public string PrivateKeyBase64 { get; private set; }
    public string PublicKeyBase64 { get; private set; }

    public InternalKeysManager()
    {
        GenerateKeys();
        logger.LogInformation("Public key: {}", 
            PublicKeyBase64);
        logger.LogInformation("Private key: {}", 
            Boolean.Parse(SystemConfiguration.Content["security:keys:showPrivateKey"] ?? "false") ? PrivateKeyBase64 : "hidden");
    }

    private void GenerateKeys()
    {
        logger.LogInformation("Generating Keys...");

        // Utilisation de la nouvelle classe KeysManagement
        var (privateBase64, publicBase64) = KeysManagement.GenerateKeyPair();

        // Stockage ou utilisation selon ton code
        PrivateKeyBase64 = privateBase64;
        PublicKeyBase64 = publicBase64;
        
    }
    
    public void Dispose(){}
}