using NSec.Cryptography;

namespace Eva.Commons.Security;

public class SignatureUtil
{
    public static byte[] SignInt(int value, string privateKeyBase64)
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        // decode clé privée
        byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

        using var privateKey = Key.Import(
            algorithm,
            privateKeyBytes,
            KeyBlobFormat.RawPrivateKey);

        byte[] data = BitConverter.GetBytes(value);

        return algorithm.Sign(privateKey, data);
    }
    
    
    public static bool VerifyIntSignature(int value, byte[] signature, string publicKeyBase64)
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        // decode clé publique
        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

        var publicKey = PublicKey.Import(
            algorithm,
            publicKeyBytes,
            KeyBlobFormat.RawPublicKey);

        byte[] data = BitConverter.GetBytes(value);

        return algorithm.Verify(publicKey, data, signature);
    }
}