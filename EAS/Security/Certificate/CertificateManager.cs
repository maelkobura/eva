using System.Text;
using System.Text.Json;
using EmbedIO;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.User;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Jose;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace Eva.AuthorityServer.Security.Certificate;

public class CertificateManager
{
    private static ILogger logger = EvaLogger.CreateLogger<CertificateManager>();
    
    public static bool IsInitialized { get; private set; } = false;

    private static string publicKey;
    private static string privateKey;

    public static void Init(string publicKey, string privateKey)
    {
        if (IsInitialized) return;
        CertificateManager.publicKey = publicKey;
        CertificateManager.privateKey = privateKey;
        IsInitialized = true;
    }

public static CertificatePackage GenerateCertificate(object entity, long unixTime)
{
    string subject;
    string[] roles;
    CertificateType type;

    switch (entity)
    {
        case UserEntity user:
            subject = user.Username;
            roles = user.Authorizations.ToArray();
            type = CertificateType.User;
            break;

        case NodeContract node:
            subject = node.Name;
            roles = node.Authorization.ToArray();
            type = CertificateType.Node;
            break;

        default:
            throw new ArgumentException("Entity type not supported for certificate generation");
    }

    // Génération de la paire de clés
    var (userPublicKey, userPrivateKey) = KeysManagement.GenerateKeyPair();

    // --- Création du JWT public ---
    var headerJson = JsonSerializer.SerializeToUtf8Bytes(new
    {
        alg = "EdDSA",
        crv = "Ed25519",
        typ = "EVACERT"
    });

    var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
    {
        sub = subject,
        pub = userPublicKey,
        exp = unixTime,
        uid = Base64.GenerateToken(),
        type,
        roles,
        eas = false
    });

    var header = Base64.Base64UrlEncode(headerJson);
    var payload = Base64.Base64UrlEncode(payloadJson);

    var signature = KeysManagement.SignMessage(userPrivateKey, $"{header}.{payload}");
    var publicToken = $"{header}.{payload}.{Base64.Base64UrlEncode(Convert.FromBase64String(signature))}";

    // --- Création du JWT privé ---
    var privatePayloadJson = JsonSerializer.SerializeToUtf8Bytes(new
    {
        sub = subject,
        pub = userPublicKey,
        exp = unixTime,
        uid = Base64.GenerateToken(),
        type,
        roles,
        eas = true
    });

    var privatePayload = Base64.Base64UrlEncode(privatePayloadJson);
    var privateSignature = KeysManagement.SignMessage(userPrivateKey, $"{header}.{privatePayload}");
    var privateToken = $"{header}.{privatePayload}.{Base64.Base64UrlEncode(Convert.FromBase64String(privateSignature))}";

    logger.LogInformation(
        "Generated certificate for {} {} (expiration: {}s)",
        type,
        subject,
        unixTime
    );

    return new CertificatePackage(publicToken, privateToken, userPrivateKey);
}
    

    public static CertificateEntity? ValidateCertificate(string token, bool eastoken = false)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            if (!KeysManagement.VerifySignature(publicKey, $"{parts[0]}.{parts[1]}", parts[2]))
                return null;

            var cert = CertificateUtil.ParseTokenPayload(token);
            if (cert == null) return null;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > cert.Expiration)
                return null;

            if (eastoken && !cert.AuthorityToken)
            {
                return null;
            }
            
            return cert;
        }
        catch
        {
            return null;
        }
    }

    public static string GetCertificate(IHttpContext context)
    {
        // Récupérer l'en-tête Authorization
        var authHeader = context.Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new Exception("No Cert found");
        }
        
        return authHeader.Substring("Bearer ".Length).Trim();
    }
}