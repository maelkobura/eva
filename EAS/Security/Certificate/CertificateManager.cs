using System.Text.Json;
using EmbedIO;
using Eva.AuthorityServer.Nodes;
using Eva.AuthorityServer.User;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Google.Protobuf.Collections;
using Jose;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using Type = Eva.Commons.Security.Certificate.Type;
using Version = Eva.Commons.Security.Certificate.Version;

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
        EntityType type;
        
        switch (entity)
        {
            case UserEntity user:
                subject = user.Username;
                roles = user.Authorizations.ToArray();
                type = EntityType.User;
                break;

            case NodeContract node:
                subject = node.Name;
                roles = node.Authorization.ToArray();
                type = EntityType.Node;
                break;

            default:
                throw new ArgumentException("Entity type not supported for certificate generation");
        }
        
        var (userPublicKey, userPrivateKey) = KeysManagement.GenerateKeyPair();

        CertificateHeader baseHeader = new CertificateHeader();
        baseHeader.Algorithm = "Ed25519";
        baseHeader.Version = Version.V1;
        
        CertificateContent content = new();
        content.Issuer = "EAS";
        content.Subject = subject;
        content.UniqueId = Base64.GenerateToken();
        content.Expiration = unixTime;
        content.EntityPublicKey = userPublicKey;
        content.Authorization.Add(roles);
        
        CertificatePayload entPayload = new();
        CertificateHeader entHeader = new(baseHeader);
        entHeader.Type = Type.Entity;
        entPayload.Header = entHeader;
        entPayload.Content = content;
        
        CertificatePayload easPayload = new();
        CertificateHeader easHeader = new(baseHeader);
        easHeader.Type = Type.Eas;
        easPayload.Header = easHeader;
        easPayload.Content = content;

        var entCert = CertificateUtil.SignCertificate(entPayload, privateKey, publicKey);
        var easCert = CertificateUtil.SignCertificate(easPayload, privateKey, publicKey);

        return new CertificatePackage(entCert, easCert, userPrivateKey);
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