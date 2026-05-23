using System.Security.Cryptography;
using System.Text;

namespace SharingApi.Infrastructure;

public static class ShareTokenCrypto
{
    public static string CreateOpaqueToken(int sizeBytes = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(sizeBytes);
        return ToUrlSafeBase64NoPadding(bytes);
    }

    public static byte[] HashToken(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    private static string ToUrlSafeBase64NoPadding(ReadOnlySpan<byte> bytes)
    {
        var b64 = Convert.ToBase64String(bytes);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
