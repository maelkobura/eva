namespace Eva.Commons.Security;

using NSec.Cryptography;
using System;
using System.Text;

public class KeysManagement
{
    // Génère une paire de clés et renvoie la clé privée et publique en Base64
    public static (string PrivateKeyBase64, string PublicKeyBase64) GenerateKeyPair()
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        using var privateKey = new Key(algorithm, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });

        // Exporter la clé privée et publique en Base64
        string privateBase64 = Convert.ToBase64String(privateKey.Export(KeyBlobFormat.RawPrivateKey));
        string publicBase64 = Convert.ToBase64String(privateKey.PublicKey.Export(KeyBlobFormat.RawPublicKey));

        return (privateBase64, publicBase64);
    }

    // Signe un message avec une clé privée Base64
    public static string SignMessage(string privateKeyBase64, string message)
    {
        var privateBytes = Convert.FromBase64String(privateKeyBase64);
        using var privateKey = Key.Import(SignatureAlgorithm.Ed25519, privateBytes, KeyBlobFormat.RawPrivateKey);

        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] signatureBytes = SignatureAlgorithm.Ed25519.Sign(privateKey, messageBytes);

        return Convert.ToBase64String(signatureBytes);
    }

    // Vérifie la signature d'un message avec une clé publique Base64
    public static bool VerifySignature(string publicKeyBase64, string message, string signatureBase64)
    {
        var publicBytes = Convert.FromBase64String(publicKeyBase64);
        var publicKey = Key.Import(SignatureAlgorithm.Ed25519, publicBytes, KeyBlobFormat.RawPublicKey).PublicKey;

        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

        return SignatureAlgorithm.Ed25519.Verify(publicKey, messageBytes, signatureBytes);
    }
}