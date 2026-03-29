using System.Security.Cryptography.X509Certificates;
using Eva.Node.Authority.Certificate;

namespace Eva.Node.Network;

public class HTTP
{
    public static HttpClient CreateHttpClient(out bool secure)
    {
        secure = false;
        var handler = new HttpClientHandler();

        if (CertificateManager.Instance!.TlsEasCertificate is not null)
        {
            handler.ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
            {
                chain!.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(CertificateManager.Instance.TlsEasCertificate);
                return chain.Build(new X509Certificate2(cert!));
            };
            secure = true;
        }

        return new HttpClient(handler);
    }
}