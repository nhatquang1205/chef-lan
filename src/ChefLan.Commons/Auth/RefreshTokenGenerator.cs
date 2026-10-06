using System.Security.Cryptography;
using System.Text;

namespace ChefLan.Commons.Auth;

/// <summary>
/// The raw token goes to the client exactly once. Only its SHA-256 hash is stored, so a
/// database leak does not hand out sessions.
/// </summary>
public static class RefreshTokenGenerator
{
    private const int TokenBytes = 32;

    public static (string RawToken, string TokenHash) Create()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes));
        return (raw, Hash(raw));
    }

    public static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    /// <summary>Fixed-time comparison, so token lookup cannot be timed.</summary>
    public static bool HashesMatch(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
}
