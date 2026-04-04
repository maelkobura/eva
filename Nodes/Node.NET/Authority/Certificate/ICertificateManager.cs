using System;
using System.Security.Cryptography.X509Certificates;

namespace Eva.Node.Authority.Certificate;

/// <summary>
/// Interface for managing node and EAS certificates.
/// </summary>
public interface ICertificateManager : IDisposable
{
    /// <summary>
    /// Generates the EVA certificate for the node.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    void GenerateEvaCertificate(string serviceName);

    /// <summary>
    /// Generates the TLS certificates for the node and EAS.
    /// </summary>
    void GenerateTlsCertificate();

    /// <summary>
    /// Raw node certificate string.
    /// </summary>
    string CertificateRaw { get; }

    /// <summary>
    /// Raw EAS certificate string.
    /// </summary>
    string EasCertificateRaw { get; }

    /// <summary>
    /// Node certificate unit object.
    /// </summary>
    Commons.Security.Certificate.Certificate CertificateUnit { get; }

    /// <summary>
    /// EAS certificate unit object.
    /// </summary>
    Commons.Security.Certificate.Certificate EasCertificateUnit { get; }

    /// <summary>
    /// Node private key.
    /// </summary>
    string PrivateKey { get; }

    /// <summary>
    /// EAS public key.
    /// </summary>
    string EasPublicKey { get; }

    /// <summary>
    /// Node TLS certificate.
    /// </summary>
    X509Certificate2? TlsNodeCertificate { get; }

    /// <summary>
    /// EAS TLS certificate.
    /// </summary>
    X509Certificate2? TlsEasCertificate { get; }
}