using Eva.Commons.Security.Certificate;

namespace Eva.AuthorityServer.Security.Certificate;

public interface ICertificateManager : IDisposable{
    /// <summary>
    /// Generates a certificate package for a given entity.
    /// </summary>
    /// <param name="entity">The entity for which the certificate is generated (UserEntity or NodeContract).</param>
    /// <param name="unixTime">Expiration timestamp in Unix time.</param>
    /// <param name="entityPublicKey">The entity's public key in Base64.</param>
    /// <returns>A signed certificate package.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity type is unsupported.</exception>
    CertificatePackage GenerateCertificate(object entity, long unixTime, string entityPublicKey);
}