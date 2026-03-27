using System.Text;
using System.Text.Json.Nodes;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
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
    
    private CertificateManager(string token)
    {
        this.token = token;
    }
    
    public void GenerateCertificate(string serviceName)
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
        var response = AuthorityClient.Instance!.SendPostRequest("/node/auth", content).Result;
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EAS response is empty");
        CertificateRaw = (string)responseJson["cert"]! ?? throw new Exception("Certificate not found in response");
        PrivateKey = userPrivateKey;
        EasPublicKey = (string)responseJson["pub"] ?? throw new Exception("Public key not found in response");
        
        EasCertificateRaw = (string)responseJson["eas"]! ?? throw new Exception("EAS certificate not found in response");
        EasCertificateUnit = CertificateUtil.ParseCertificateBase64(EasCertificateRaw) ?? throw new Exception("EAS certificate parsing failed");
        AuthorityClient.Instance.EasCertificate = EasCertificateRaw;
        
        CertificateUnit = CertificateUtil.ParseCertificateBase64(CertificateRaw) ?? throw new Exception("Certificate parsing failed");

        logger.LogInformation("Successfully created certificate.");
    }
    
}