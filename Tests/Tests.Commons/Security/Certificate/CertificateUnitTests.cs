using Eva.Commons.Security.Certificate;
using Google.Protobuf;
using NSec.Cryptography;

public class CertificateUtilTests
{
    private (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        using var key = new Key(algorithm, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });

        var privateKey = Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey));
        var publicKey = Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey));

        return (privateKey, publicKey);
    }

    private CertificatePayload CreatePayload(string publicKey)
    {
        return new CertificatePayload
        {
            Content = new CertificateContent
            {
                SignaturePublicKey = publicKey,
                // ajoute d'autres champs si nécessaire
            }
        };
    }

    [Fact]
    public void Sign_And_Verify_Should_Work()
    {
        var (priv, pub) = GenerateKeyPair();
        var payload = CreatePayload(pub);

        var cert = CertificateUtil.SignCertificate(payload, priv);

        bool isValid = CertificateUtil.CheckCertificate(cert);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_With_Wrong_Key_Should_Fail()
    {
        var (priv1, pub1) = GenerateKeyPair();
        var (priv2, pub2) = GenerateKeyPair();

        var payload = CreatePayload(pub1);
        var cert = CertificateUtil.SignCertificate(payload, priv1);

        bool isValid = CertificateUtil.CheckCertificate(cert, pub2);

        Assert.False(isValid);
    }

    [Fact]
    public void ParseCertificateBase64_Should_Work()
    {
        var (priv, pub) = GenerateKeyPair();
        var payload = CreatePayload(pub);

        var cert = CertificateUtil.SignCertificate(payload, priv);

        var bytes = cert.ToByteArray();
        var base64 = Convert.ToBase64String(bytes);

        var parsed = CertificateUtil.ParseCertificateBase64(base64);

        Assert.NotNull(parsed);
        Assert.Equal(cert.Signature, parsed!.Signature);
    }

    [Fact]
    public void Parse_Invalid_Base64_Should_Return_Null()
    {
        var result = CertificateUtil.ParseCertificateBase64("invalid_base64");

        Assert.Null(result);
    }

    [Fact]
    public void Verify_Null_Certificate_Should_Fail()
    {
        bool result = CertificateUtil.CheckCertificate(null);

        Assert.False(result);
    }

    [Fact]
    public void Tampered_Payload_Should_Fail_Verification()
    {
        var (priv, pub) = GenerateKeyPair();
        var payload = CreatePayload(pub);

        var cert = CertificateUtil.SignCertificate(payload, priv);

        // Tamper le payload
        cert.Payload.Content.SignaturePublicKey = "tampered";

        bool isValid = CertificateUtil.CheckCertificate(cert);

        Assert.False(isValid);
    }
}