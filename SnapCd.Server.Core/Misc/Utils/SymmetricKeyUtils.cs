using System.Text.RegularExpressions;

namespace SnapCd.Server.Core.Misc.Utils;

public static class SymmetricKeyUtils
{
    public static string ExtractBase64FromPem(string pemString)
    {
        // Use regex to extract the Base64 string between BEGIN and END delimiters
        var match = Regex.Match(pemString, @"-----BEGIN SYMMETRIC KEY-----(.*?)-----END SYMMETRIC KEY-----",
            RegexOptions.Singleline);

        if (match.Success)
            // Remove whitespace characters and return the Base64 encoded string
            return match.Groups[1].Value.Trim();
        else
            throw new ArgumentException("Invalid PEM format");
    }
}