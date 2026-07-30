using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace GestorDeBacklogs.Api.Security;

[SupportedOSPlatform("windows")]
public static class SecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GestorDeBacklogs.PAT.v1");

    public static string Protect(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedText)
    {
        var protectedBytes = Convert.FromBase64String(protectedText);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
