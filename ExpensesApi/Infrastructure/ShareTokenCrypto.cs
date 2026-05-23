using System.Security.Cryptography;
using System.Text;

namespace ExpensesApi.Infrastructure;

public static class ShareTokenCrypto
{
    public static byte[] HashToken(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }
}
