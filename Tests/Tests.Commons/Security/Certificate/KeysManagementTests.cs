using System;
using Xunit;
using Eva.Commons.Security;
using NSec.Cryptography;

public class KeysManagementTests
{
    [Fact]
    public void GenerateKeyPair_Should_Return_Valid_Base64_Keys()
    {
        var (privateKey, publicKey) = KeysManagement.GenerateKeyPair();

        Assert.False(string.IsNullOrWhiteSpace(privateKey));
        Assert.False(string.IsNullOrWhiteSpace(publicKey));

        // Vérifie que c'est du Base64 valide
        var privateBytes = Convert.FromBase64String(privateKey);
        var publicBytes = Convert.FromBase64String(publicKey);

        Assert.NotEmpty(privateBytes);
        Assert.NotEmpty(publicBytes);
    }

    [Fact]
    public void Generated_Keys_Should_Be_Valid_For_Ed25519()
    {
        var (privateKeyBase64, publicKeyBase64) = KeysManagement.GenerateKeyPair();

        var privateBytes = Convert.FromBase64String(privateKeyBase64);
        var publicBytes = Convert.FromBase64String(publicKeyBase64);

        using var privateKey = Key.Import(SignatureAlgorithm.Ed25519, privateBytes, KeyBlobFormat.RawPrivateKey);
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, publicBytes, KeyBlobFormat.RawPublicKey);

        byte[] data = System.Text.Encoding.UTF8.GetBytes("test-data");

        byte[] signature = SignatureAlgorithm.Ed25519.Sign(privateKey, data);

        bool isValid = SignatureAlgorithm.Ed25519.Verify(publicKey, data, signature);

        Assert.True(isValid);
    }

    [Fact]
    public void Generated_KeyPairs_Should_Be_Different()
    {
        var (priv1, pub1) = KeysManagement.GenerateKeyPair();
        var (priv2, pub2) = KeysManagement.GenerateKeyPair();

        Assert.NotEqual(priv1, priv2);
        Assert.NotEqual(pub1, pub2);
    }

    [Fact]
    public void Private_And_Public_Key_Should_Not_Be_Equal()
    {
        var (privateKey, publicKey) = KeysManagement.GenerateKeyPair();

        Assert.NotEqual(privateKey, publicKey);
    }

    [Fact]
    public void Generated_Key_Should_Have_Expected_Length()
    {
        var (privateKey, publicKey) = KeysManagement.GenerateKeyPair();

        var privateBytes = Convert.FromBase64String(privateKey);
        var publicBytes = Convert.FromBase64String(publicKey);

        // Ed25519 specs
        Assert.Equal(32, publicBytes.Length);
        Assert.True(privateBytes.Length == 32 || privateBytes.Length == 64);
    }
}