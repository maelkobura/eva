using System.Text;
using System.Text.Json;
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
            type = "User",
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

            var payloadJson = Base64.Base64UrlDecode(parts[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

            if (payload is null) return null;

            var exp = payload["exp"].GetInt64();
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return null;

            var name = payload["sub"].GetString()!;
            var type = Enum.Parse<CertificateType>(payload["type"].GetString()!);
            var roles = payload["roles"].EnumerateArray().Select(r => r.GetString()!).ToArray();

            return new CertificateEntity(name, type, roles, exp);
        }
        catch
        {
            return null;
        }
    }
}