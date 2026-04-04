namespace Eva.AuthorityServer.Security;

public interface IKeysManager : IDisposable{
    /// <summary>
    /// Private key encoded in Base64
    /// </summary>
    string PrivateKeyBase64 { get; }

    /// <summary>
    /// Public key encoded in Base64
    /// </summary>
    string PublicKeyBase64 { get; }
    
}