using System.Security.Cryptography.X509Certificates;

namespace Eva.AuthorityServer.Security.Certificate;

public interface ITlsAuthority : IDisposable
{
    /// <summary>
    /// Current Certificate Authority certificate.
    /// </summary>
    X509Certificate2? CA { get; }

    /// <summary>
    /// Generates a self-signed Certificate Authority (CA) certificate if not already created.
    /// </summary>
    /// <returns>The CA certificate.</returns>
    X509Certificate2 GenerateCA();

    /// <summary>
    /// Issues a certificate for a node using the current CA.
    /// </summary>
    /// <param name="nodeId">The node identifier (DNS or IP).</param>
    /// <returns>A signed X509 certificate for the node.</returns>
    /// <exception cref="Exception">Thrown if the CA is not initialized.</exception>
    X509Certificate2 IssueNodeCertificate(string nodeId);
}