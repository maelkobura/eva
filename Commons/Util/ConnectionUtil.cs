using EmbedIO;

namespace Eva.Commons.Util;

public class ConnectionUtil
{
    public static string GetCertificate(IHttpContext context)
    {
        // Récupérer l'en-tête Authorization
        var authHeader = context.Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new Exception("No Cert found");
        }
        
        return authHeader.Substring("Bearer ".Length).Trim();
    }
    
    public static string GetBorrowCertificate(IHttpContext context)
    {
        // Récupérer l'en-tête Authorization
        var authHeader = context.Request.Headers["Borrowed"];

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new Exception("No Borrowed cert found");
        }
        
        return authHeader.Substring("Bearer ".Length).Trim();
    }

    public static bool IsBorrowCertificate(IHttpContext context)
    {
        return context.Request.Headers.AllKeys.Contains("Borrowed");
    }
}