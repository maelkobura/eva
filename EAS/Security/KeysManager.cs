using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace Eva.AuthorityServer.Security;

public class KeysManager
{
    private static ILogger logger = EvaLogger.CreateLogger<KeysManager>();

    public static Key PrivateKey { get; private set; }
    public static PublicKey PublicKey { get; private set; }
    
    public static bool IsInitialized { get; private set; } = false;
    
    public static void Init()
    {
        if (IsInitialized) return;
        logger.LogInformation("Initializing KeysManager...");
        GenerateKeys();
        logger.LogInformation("Public key: {}", 
            Convert.ToBase64String(PublicKey.Export(KeyBlobFormat.NSecPublicKey)));
        logger.LogInformation("Private key: {}", 
            Boolean.Parse(Configuration.Content["security:keys:showPrivateKey"] ?? "false") ? Convert.ToBase64String(PrivateKey.Export(KeyBlobFormat.NSecPrivateKey)) : "hidden");
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