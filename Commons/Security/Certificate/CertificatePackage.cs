namespace Eva.Commons.Security.Certificate;

public record CertificatePackage(Certificate EntityCertificateUnit, Certificate AuthorityCertificateUnit, string PrivateKey);