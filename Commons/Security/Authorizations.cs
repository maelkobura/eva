namespace Eva.Commons.Security;

public class Authorizations
{

    public static bool Has(Certificate.Certificate cert, string authorization)
    {
        if (cert?.Payload?.Content?.Authorization == null || string.IsNullOrWhiteSpace(authorization))
            return false;

        var auths = cert.Payload.Content.Authorization;

        var targetParts = authorization.Split('.');

        // 1. CHECK DENY FIRST (priority)
        foreach (var raw in auths)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw[0] != '-')
                continue;

            var deny = raw.Substring(1);

            if (Matches(deny, targetParts))
                return false;
        }

        // 2. CHECK ALLOW
        foreach (var raw in auths)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw[0] == '-')
                continue;

            if (raw == "*")
                return true;

            if (Matches(raw, targetParts))
                return true;
        }

        return false;
    }

    private static bool Matches(string rule, string[] targetParts)
    {
        if (rule == "*")
            return true;

        var ruleParts = rule.Split('.');

        for (int i = 0; i < ruleParts.Length; i++)
        {
            if (i >= targetParts.Length)
                return false;

            if (ruleParts[i] == "*")
                return true;

            if (ruleParts[i] != targetParts[i])
                return false;
        }

        // STRICT: same depth only if no wildcard encountered
        return ruleParts.Length == targetParts.Length;
    }
    
}