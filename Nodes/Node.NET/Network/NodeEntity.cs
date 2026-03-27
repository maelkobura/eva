using Eva.Commons.Security.Certificate;
using NSec.Cryptography;

namespace Eva.Node.Network;

public class NodeEntity {
    
    public string Name { get;}
    public string DisplayName;
    public string Address { get; }
    
    public Certificate? NodeTrustCertificate;

    public string PrivateKey;

    public NodeEntity(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public bool IsExpirated()
    {
        return NodeTrustCertificate == null || NodeTrustCertificate.Payload.Content.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}