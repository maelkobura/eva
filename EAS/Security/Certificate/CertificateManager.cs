using System.Text;
using System.Text.Json;
using EmbedIO;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.User;
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

    private static PublicKey publicKey;
    private static Key privateKey;

    public static void Init(PublicKey publicKey, Key privateKey)
    {
        if (IsInitialized) return;
        CertificateManager.publicKey = publicKey;
        CertificateManager.privateKey = privateKey;
        IsInitialized = true;
    }

    public static string GenerateCertificateForUser(UserEntity user, long unixTime)
    {
        // Header
        var headerJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "EdDSA",
            crv = "Ed25519",
            typ = "JWT"
        });

        // Payload
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = user.Username,
            pub = Convert.ToBase64String(publicKey.Export(KeyBlobFormat.RawPublicKey)),
            exp = unixTime,
            type = CertificateType.User,
            roles = user.Authorizations.ToArray()
        });

        var header = Base64.Base64UrlEncode(headerJson);
        var payload = Base64.Base64UrlEncode(payloadJson);

        // Signing input = "header.payload"
        var signingInput = Encoding.UTF8.GetBytes($"{header}.{payload}");

        // Signature Ed25519 via NSec
        var signature = SignatureAlgorithm.Ed25519.Sign(privateKey, signingInput);

        var token = $"{header}.{payload}.{Base64.Base64UrlEncode(signature)}";
        logger.LogInformation("Generated certificate for user {} (expiration: {}s)", user.Username, unixTime);
        return token;
    }
    
    public static string GenerateCertificateForNode(NodeContract node, long unixTime)
    {
        // Header
        var headerJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "EdDSA",
            crv = "Ed25519",
            typ = "JWT"
        });

        // Payload
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = node.Name,
            pub = Convert.ToBase64String(publicKey.Export(KeyBlobFormat.RawPublicKey)),
            exp = unixTime,
            type = CertificateType.Node,
            roles = node.Authorization.ToArray()
        });

        var header = Base64.Base64UrlEncode(headerJson);
        var payload = Base64.Base64UrlEncode(payloadJson);

        // Signing input = "header.payload"
        var signingInput = Encoding.UTF8.GetBytes($"{header}.{payload}");

        // Signature Ed25519 via NSec
        var signature = SignatureAlgorithm.Ed25519.Sign(privateKey, signingInput);

        var token = $"{header}.{payload}.{Base64.Base64UrlEncode(signature)}";
        logger.LogInformation("Generated certificate for node {} (expiration: {}s)", node.Name, unixTime);
        return token;
    }
    

    public static CertificateEntity? ValidateCertificate(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
            var signature = Base64.Base64UrlDecode(parts[2]);

            if (!SignatureAlgorithm.Ed25519.Verify(publicKey, signingInput, signature))
                return null;

            var cert = CertificateUtil.ParseTokenPayload(token);
            if (cert == null) return null;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > cert.Expiration)
                return null;

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