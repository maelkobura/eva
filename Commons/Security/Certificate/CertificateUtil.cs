using System.Text;
using System.Text.Json;
using Eva.Commons.Util;
using Google.Protobuf;
using NSec.Cryptography;

namespace Eva.Commons.Security.Certificate;

public class CertificateUtil
{
    public static Certificate? ParseCertificateBase64(string rawCert)
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(rawCert);
            return Certificate.Parser.ParseFrom(bytes);
        }
        catch (Exception e) {
            return null;
        }
    }

    public static Certificate SignCertificate(CertificatePayload payload, string privateKeyBase64, string? publicKeyBase64 = null)
    {
        if (!string.IsNullOrEmpty(publicKeyBase64))
            payload.Content.SignaturePublicKey = publicKeyBase64;

        byte[] data = payload.ToByteArray();
        var privateBytes = Convert.FromBase64String(privateKeyBase64);

        using var privateKey = Key.Import(SignatureAlgorithm.Ed25519, privateBytes, KeyBlobFormat.RawPrivateKey);

        byte[] signatureBytes = SignatureAlgorithm.Ed25519.Sign(privateKey, data);

        return new Certificate
        {
            Payload = payload,
            Signature = Convert.ToBase64String(signatureBytes)
        };
    }

    public static bool CheckCertificate(Certificate? cert, string? publicKeyBase64 = null)
    {
        if (cert == null) return false;

        try
        {
            var publicBytes = Convert.FromBase64String(publicKeyBase64 ?? cert.Payload.Content.SignaturePublicKey);

            var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, publicBytes, KeyBlobFormat.RawPublicKey);

            var data = cert.Payload.ToByteArray();

            return SignatureAlgorithm.Ed25519.Verify(publicKey, data, Convert.FromBase64String(cert.Signature));
        }
        catch
        {
            return false;
        }
    }
    
    public static bool CheckBorrowCertificate(Certificate? borrowCert, Certificate? nodeTrustCert, string? publicKeyBase64 = null)
    {
        if (borrowCert == null || nodeTrustCert == null) return false;

        // Check that it's actually a borrow cert
        if (borrowCert.Payload.Header.Type != Type.Borrow) return false;

        // Check expiration
        if (borrowCert.Payload.Content.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return false;

        // Validate node trust cert with EAS public key
        if (!CheckCertificate(nodeTrustCert, publicKeyBase64)) return false;

        // Check that the base certificate exists
        var baseCert = borrowCert.Payload.Content.BaseCertificate;
        if (baseCert == null) return false;

        // Validate user cert with EAS public key
        if (!CheckCertificate(baseCert, publicKeyBase64)) return false;

        // Check that the node trust cert subject matches the user cert subject
        // (proves that node A is the intended recipient of the borrow cert)
        if (nodeTrustCert.Payload.Content.Subject != baseCert.Payload.Content.Subject) return false;

        // Validate borrow cert signature using user's signature public key
        if (!CheckCertificate(borrowCert, baseCert.Payload.Content.SignaturePublicKey)) return false;

        // Check that borrow cert authorizations are a subset of the user cert authorizations
        if (!borrowCert.Payload.Content.Authorization.All(a => Authorizations.Has(baseCert, a))) return false;

        return true;
    }
}