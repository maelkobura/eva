using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Eva.Commons.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.AuthorityServer.Security.Certificate;

internal class InternalTlsAuthority : ITlsAuthority
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalTlsAuthority>();

    public X509Certificate2? CA { get; private set; }

    public X509Certificate2 GenerateCA()
    {
        if (CA is not null) return CA;

        using var rsa = RSA.Create(4096);

        var request = new CertificateRequest(
            "CN=EAS CA, O=EvaNetwork",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true
            )
        );

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: true
            )
        );

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(10)
        );

        CA = cert;
        return cert;
    }

    public X509Certificate2 IssueNodeCertificate(string nodeId)
    {
        if (CA is null)
            throw new Exception("CA not initialized");

        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={nodeId}, O=MyCluster",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false)
        );

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true
            )
        );

        var san = new SubjectAlternativeNameBuilder();

        if (IPAddress.TryParse(nodeId, out var ip))
            san.AddIpAddress(ip);
        else
            san.AddDnsName(nodeId);

        if (bool.Parse(SystemConfiguration.Content["debug.tls.allow-loopback"] ?? "false"))
            san.AddDnsName("localhost");

        request.CertificateExtensions.Add(san.Build());

        var cert = request.Create(
            CA,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(90),
            Guid.NewGuid().ToByteArray()
        );

        return cert.CopyWithPrivateKey(rsa);
    }

    public void Dispose()
    {
        CA?.Dispose();
    }
}