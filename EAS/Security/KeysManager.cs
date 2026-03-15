using Eva.AuthorityServer.System;
using Eva.Commons.Security;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace Eva.AuthorityServer.Security;

public class KeysManager
{
    private static ILogger logger = EvaLogger.CreateLogger<KeysManager>();

    public static string PrivateKeyBase64 { get; private set; }
    public static string PublicKeyBase64 { get; private set; }
    
    public static bool IsInitialized { get; private set; } = false;
    
    public static void Init()
    {
        if (IsInitialized) return;
        logger.LogInformation("Initializing KeysManager...");
        GenerateKeys();
        logger.LogInformation("Public key: {}", 
            PublicKeyBase64);
        logger.LogInformation("Private key: {}", 
            Boolean.Parse(Configuration.Content["security:keys:showPrivateKey"] ?? "false") ? PrivateKeyBase64 : "hidden");
        IsInitialized = true;
    }

    private static void GenerateKeys()
    {
        logger.LogInformation("Generating Keys...");

        // Utilisation de la nouvelle classe KeysManagement
        var (privateBase64, publicBase64) = KeysManagement.GenerateKeyPair();

        // Stockage ou utilisation selon ton code
        PrivateKeyBase64 = privateBase64;
        PublicKeyBase64 = publicBase64;
        
    }
    
    
}