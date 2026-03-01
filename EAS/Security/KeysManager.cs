using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace Eva.AuthorityServer.Security;

public class KeysManager
{
    private static ILogger logger = EvaLogger.CreateLogger<KeysManager>();
    
    private static Key PrivateKey;
    private static PublicKey PublicKey;
    
    public static bool IsInitialized { get; private set; } = false;
    
    public static void Init()
    {
        if (IsInitialized) return;
        logger.LogInformation("Initializing KeysManager...");
        GenerateKeys();
        logger.LogInformation("Public key: {}", 
            BitConverter.ToString(PublicKey.Export(KeyBlobFormat.RawPublicKey)).Replace("-", ""));
        logger.LogInformation("Private key: {}", 
            Boolean.Parse(Configuration.Content["security:keys:showPrivateKey"] ?? "false") ? BitConverter.ToString(PrivateKey.Export(KeyBlobFormat.RawPrivateKey)).Replace("-", "") : "hidden");
        IsInitialized = true;
    }

    private static void GenerateKeys()
    {
        logger.LogInformation("Generating Keys...");
        var algorithm = SignatureAlgorithm.Ed25519;
        PrivateKey = new Key(algorithm, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });
        PublicKey = PrivateKey.PublicKey;
    }
    
    
}