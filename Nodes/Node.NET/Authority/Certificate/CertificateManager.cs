using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

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
    private CertificateEntity certificate;
    
    private CertificateManager(string token)
    {
        this.token = token;
    }
    
    public void GenerateCertificate()
    {
        if (certificateRaw == null)
        {
            logger.LogInformation("Creating node certificate...");
        }
        else
        {
            logger.LogInformation("Refreshing node certificate...");
        }
        
        //TODO Fetch EAS
    }
    
}