namespace Eva.Commons.Security.Certificate;

public record CertificateEntity(string Name, string UniqueId, CertificateType Type, string[] Authorization, long Expiration, bool AuthorityToken);

public enum CertificateType {
    User,
    Node
}