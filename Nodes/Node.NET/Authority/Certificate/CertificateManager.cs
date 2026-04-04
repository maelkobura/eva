using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using Eva.AuthorityServer.System;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swan.Formatters;

namespace Eva.Node.Authority.Certificate;

public class CertificateManager
{
    public static CertificateManager? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<CertificateManager>();

    public static void Init(string token)
    {
        if (Instance != null) return;
        Instance = new CertificateManager(token);
    }

    private readonly string token;
    public string CertificateRaw { get; private set; }
    public string EasCertificateRaw{ get; private set; }
    public Commons.Security.Certificate.Certificate CertificateUnit{ get; private set; }
    public Commons.Security.Certificate.Certificate EasCertificateUnit{ get; private set; }
    public string PrivateKey{ get; private set; }
    public string EasPublicKey{ get; private set; }
    public X509Certificate2? TlsNodeCertificate{ get; private set; }
    public X509Certificate2? TlsEasCertificate{ get; private set; }
    
    private CertificateManager(string token)
    {
        this.token = token;
    }
    
    public void GenerateEvaCertificate(string serviceName)
    {
        if (CertificateRaw == null)
        {
            logger.LogInformation("Creating node certificate...");
        }
        else
        {
            logger.LogInformation("Refreshing node certificate...");
        }

        var (userPrivateKey, userPublicKey) = KeysManagement.GenerateKeyPair();
        
        string json = JsonConvert.SerializeObject(new { service = serviceName, token, publickey = userPublicKey });
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = EvaSystem.Singleton<IAuthorityClient>().SendPostRequest("/node/auth", content).Result;
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EAS response is empty");
        CertificateRaw = (string)responseJson["cert"]! ?? throw new Exception("Certificate not found in response");
        PrivateKey = userPrivateKey;
        EasPublicKey = (string)responseJson["pub"] ?? throw new Exception("Public key not found in response");
        
        EasCertificateRaw = (string)responseJson["eas"]! ?? throw new Exception("EAS certificate not found in response");
        EasCertificateUnit = CertificateUtil.ParseCertificateBase64(EasCertificateRaw) ?? throw new Exception("EAS certificate parsing failed");
        EvaSystem.Singleton<IAuthorityClient>().EasCertificate = EasCertificateRaw;
        
        CertificateUnit = CertificateUtil.ParseCertificateBase64(CertificateRaw) ?? throw new Exception("Certificate parsing failed");

        logger.LogInformation("Successfully created certificate.");
    }
    
    public void GenerateTlsCertificate()
    {
        if(Configuration.Content["debug:skip-tls"]=="true") return;
        
        if (TlsNodeCertificate == null)
        {
            logger.LogInformation("Creating TLS certificate...");
        }
        else
        {
            logger.LogInformation("Refreshing TLS certificate...");
        }
        
        var response = EvaSystem.Singleton<IAuthorityClient>().SendGetRequest("/node/auth/tls").Result;
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EAS response is empty");
        
        TlsNodeCertificate = new X509Certificate2(Convert.FromBase64String((string)responseJson["nodeCert"]! ?? throw new Exception("Node TLS certificate not found in response")));
        TlsEasCertificate = new X509Certificate2(Convert.FromBase64String((string)responseJson["easCert"]! ?? throw new Exception("EAS TLS certificate not found in response")));
        
        logger.LogInformation("Successfully created TLS certificate.");
    }
    
}