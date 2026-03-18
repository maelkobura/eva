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
}