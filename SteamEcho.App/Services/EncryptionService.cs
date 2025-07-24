using System.Security.Cryptography;
using System.Text;

namespace SteamEcho.App.Services;

public class EncryptionService
{
    private static readonly byte[] s_entropy = Encoding.Unicode.GetBytes("SteamApiKeySalt");

    public string? Encrypt(string? dataToEncrypt)
    {
        if (string.IsNullOrEmpty(dataToEncrypt))
        {
            return null;
        }

        byte[] dataBytes = Encoding.Unicode.GetBytes(dataToEncrypt);
        byte[] encryptedData = ProtectedData.Protect(dataBytes, s_entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedData);
    }

    public string? Decrypt(string? encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
        {
            return null;
        }
        try
        {
            byte[] dataBytes = Convert.FromBase64String(encryptedData);
            byte[] decryptedData = ProtectedData.Unprotect(dataBytes, s_entropy, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(decryptedData);
        }
        catch (CryptographicException)
        {
            // Failed to decrypt
            return null;
        }
    }
}