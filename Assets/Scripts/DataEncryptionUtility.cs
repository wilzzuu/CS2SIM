using System;
using System.Security.Cryptography;
using System.Text;

public static class DataEncryptionUtility
{
    private const string EncryptionKey = "DVCL1at2KVWANdLqNJeKrgQaShmtlgOR";

    public static string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();

        aes.Key = GenerateKey(EncryptionKey);
        aes.GenerateIV();

        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        ICryptoTransform encryptor =
            aes.CreateEncryptor(aes.Key, aes.IV);

        byte[] plainBytes =
            Encoding.UTF8.GetBytes(plainText);

        byte[] encryptedBytes =
            encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length);

        byte[] combined =
            new byte[aes.IV.Length + encryptedBytes.Length];

        Buffer.BlockCopy(
            aes.IV,
            0,
            combined,
            0,
            aes.IV.Length);

        Buffer.BlockCopy(
            encryptedBytes,
            0,
            combined,
            aes.IV.Length,
            encryptedBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public static string Decrypt(string encryptedText)
    {
        byte[] combined =
            Convert.FromBase64String(encryptedText);

        using Aes aes = Aes.Create();

        aes.Key = GenerateKey(EncryptionKey);

        byte[] iv = new byte[16];

        Buffer.BlockCopy(
            combined,
            0,
            iv,
            0,
            iv.Length);

        aes.IV = iv;

        byte[] encryptedBytes =
            new byte[combined.Length - iv.Length];

        Buffer.BlockCopy(
            combined,
            iv.Length,
            encryptedBytes,
            0,
            encryptedBytes.Length);

        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        ICryptoTransform decryptor =
            aes.CreateDecryptor(aes.Key, aes.IV);

        byte[] decryptedBytes =
            decryptor.TransformFinalBlock(
                encryptedBytes,
                0,
                encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] GenerateKey(string key)
    {
        using SHA256 sha = SHA256.Create();

        return sha.ComputeHash(
            Encoding.UTF8.GetBytes(key));
    }
}