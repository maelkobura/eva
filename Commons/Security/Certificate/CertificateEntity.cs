namespace Eva.Commons.Security.Certificate;

public record CertificateEntity(string Name, CertificateType Type, string[] Authorization, long Expiration);

public enum CertificateType {
    User,
    Node
}