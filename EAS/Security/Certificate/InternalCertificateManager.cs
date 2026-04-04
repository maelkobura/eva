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

public class InternalCertificateManager : ICertificateManager{
    private static ILogger logger = EvaLogger.CreateLogger<InternalCertificateManager>();

    private string publicKey;
    private string privateKey;

    public InternalCertificateManager(string publicKey, string privateKey)
    {
       this.publicKey = publicKey;
       this.privateKey = privateKey;
    }
    
    public CertificatePackage GenerateCertificate(object entity, long unixTime, string entityPublicKey)
    {
        string subject;
        string[] roles;
        EntityType entityType;
        
        switch (entity)
        {
            case UserEntity user:
                subject = user.Username;
                roles = user.Authorizations.ToArray();
                entityType = EntityType.User;
                break;

            case InternalNodeContract node:
                subject = node.Name;
                roles = node.Authorization.ToArray();
                entityType = EntityType.Node;
                break;

            default:
                throw new ArgumentException("Entity type not supported for certificate generation");
        }

        CertificateHeader baseHeader = new CertificateHeader();
        baseHeader.Algorithm = "Ed25519";
        baseHeader.Version = Version.V1;
        
        CertificateContent content = new();
        content.Issuer = "EAS";
        content.Subject = subject;
        content.EntityType = entityType;
        content.UniqueId = Base64.GenerateToken();
        content.Expiration = unixTime;
        content.EntityPublicKey = entityPublicKey;
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

        return new CertificatePackage(entCert, easCert);
    }

    public void Dispose(){}
}