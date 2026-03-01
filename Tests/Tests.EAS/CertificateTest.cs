using Eva.AuthorityServer.Security.Certificate;
using Eva.AuthorityServer.User;
using NSec.Cryptography;
using System.Text;
using System.Text.Json;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;

namespace Tests.EAS;

public class CertificateTest
{
    private static readonly object _initLock = new();
    private static bool _initialized = false;
    private static Key _sharedKey;
    private static PublicKey _sharedPubKey;

    public CertificateTest()
    {
        EvaLogger.Init("EAS TEST");
    }
    
    // Parce que CertificateManager est statique et n'init qu'une fois,
    // on partage la même clé entre tous les tests
    private void EnsureInitialized()
    {
        lock (_initLock)
        {
            if (_initialized) return;
            _sharedKey = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            });
            _sharedPubKey = _sharedKey.PublicKey;
            CertificateManager.Init(_sharedPubKey, _sharedKey);
            _initialized = true;
        }
    }

    private UserEntity CreateTestUser() => new UserEntity
    {
        Username = "testuser",
        Authorizations = new[] { "testAuth.testValue", "authTest.*" }
    };

    private static Dictionary<string, JsonElement> DecodePayload(string token)
    {
        var parts = token.Split('.');
        var padded = parts[1].Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    private static Dictionary<string, JsonElement> DecodeHeader(string token)
    {
        var parts = token.Split('.');
        var padded = parts[0].Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    [Fact]
    public void Init_ShouldInitializeOnce()
    {
        EnsureInitialized();
        Assert.True(CertificateManager.IsInitialized);

        // Appel ultérieur ne doit pas réinitialiser
        CertificateManager.Init(_sharedPubKey, _sharedKey);
        Assert.True(CertificateManager.IsInitialized);
    }

    [Fact]
    public void GenerateCertificateForUser_ShouldReturnValidJwtStructure()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(3, token.Split('.').Length); // header.payload.signature
    }

    [Fact]
    public void GenerateCertificateForUser_ShouldIncludeCorrectHeader()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);
        var header = DecodeHeader(token);

        Assert.Equal("EdDSA", header["alg"].GetString());
        Assert.Equal("JWT", header["typ"].GetString());
        Assert.Equal("Ed25519", header["crv"].GetString());
    }

    [Fact]
    public void GenerateCertificateForUser_ShouldIncludeRolesAndPubKey()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);
        var payload = DecodePayload(token);

        Assert.Equal("testuser", payload["sub"].GetString());
        Assert.NotNull(payload["pub"].GetString());
        Assert.Equal(expiry, payload["exp"].GetInt64());

        var roles = payload["roles"].EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("testAuth.testValue", roles);
        Assert.Contains("authTest.*", roles);
    }

    [Fact]
    public void ValidateCertificate_ShouldReturnEntityForValidToken()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);
        var certificate = CertificateManager.ValidateCertificate(token);

        Assert.NotNull(certificate);
        Assert.Equal("testuser", certificate.Name);
        Assert.Equal(CertificateType.User, certificate.Type);
        Assert.Equal(expiry, certificate.Expiration);
        Assert.Contains("testAuth.testValue", certificate.Authorization);
    }

    [Fact]
    public void ValidateCertificate_ShouldReturnNullForExpiredToken()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);
        Assert.Null(CertificateManager.ValidateCertificate(token));
    }

    [Fact]
    public void ValidateCertificate_ShouldReturnNullForTamperedToken()
    {
        EnsureInitialized();
        var user = CreateTestUser();
        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

        string token = CertificateManager.GenerateCertificateForUser(user, expiry);
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.{parts[1]}AAAA.{parts[2]}";

        Assert.Null(CertificateManager.ValidateCertificate(tamperedToken));
    }
}