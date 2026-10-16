using System.Buffers.Text;
using System.Security.Cryptography;

namespace ChefLan.Commons.Auth;

/// <summary>
/// Single-use tokens sent by email (customer invitations). URL-safe base64 so they survive being pasted into a link;
/// stored only as a SHA-256 hash, exactly like refresh tokens.
/// </summary>
public static class OneTimeToken
{
    private const int TokenBytes = 32;

    public static (string RawToken, string TokenHash) Create()
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        return (raw, Hash(raw));
    }

    public static string Hash(string rawToken) => RefreshTokenGenerator.Hash(rawToken.Trim());
}
