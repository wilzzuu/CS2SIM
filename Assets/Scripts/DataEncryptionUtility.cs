using System.Text;
using UnityEngine;
using System.Security.Cryptography;

public static class DataEncryptionUtility
{
    private static readonly string EncryptionKey = "YourSecureKey123"; // Change this to a secure key

    public static string Encrypt(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

            return System.Convert.ToBase64String(encryptedBytes);
        }
    }

    public static string Decrypt(string encryptedData)
    {
        byte[] encryptedBytes = System.Convert.FromBase64String(encryptedData);
        byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}