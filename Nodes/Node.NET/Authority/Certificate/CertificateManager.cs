using System.Text;
using System.Text.Json.Nodes;
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
    private string certificateRaw;
    private string easCertificateRaw;
    private Commons.Security.Certificate.Certificate _certificateUnit;
    private Commons.Security.Certificate.Certificate _easCertificateUnit;
    private string PrivateKey;
    
    private CertificateManager(string token)
    {
        this.token = token;
    }
    
    public void GenerateCertificate(string serviceName)
    {
        if (certificateRaw == null)
        {
            logger.LogInformation("Creating node certificate...");
        }
        else
        {
            logger.LogInformation("Refreshing node certificate...");
        }

        string json = JsonConvert.SerializeObject(new { service = serviceName, token });
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = AuthorityClient.Instance!.SendPostRequest("/node/auth", content).Result;
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EAS response is empty");
        certificateRaw = (string)responseJson["cert"]! ?? throw new Exception("Certificate not found in response");
        PrivateKey = (string)responseJson["prv"] ?? throw new Exception("Private key not found in response");
        
        easCertificateRaw = (string)responseJson["eas"]! ?? throw new Exception("EAS certificate not found in response");
        _easCertificateUnit = CertificateUtil.ParseCertificateBase64(easCertificateRaw) ?? throw new Exception("EAS certificate parsing failed");
        AuthorityClient.Instance.EasCertificate = easCertificateRaw;
        
        _certificateUnit = CertificateUtil.ParseCertificateBase64(certificateRaw) ?? throw new Exception("Certificate parsing failed");

        logger.LogInformation("Successfully created certificate.");
    }
    
}