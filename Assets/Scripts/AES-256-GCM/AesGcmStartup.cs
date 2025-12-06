using System;
using System.Security.Cryptography;
using System.Text;

// BouncyCastle
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

public static class AesGcmStartup
{
    /// <summary>
    /// Mã hóa AES-256-GCM theo đúng format Node:
    /// x-data = Base64( IV(12 bytes) + ciphertext + authTag(16 bytes) )
    /// </summary>
    public static string EncryptForXData(string plainText, string base64Key)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256-GCM");

        // 12-byte IV (nonce)
        byte[] iv = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(key), 128, iv, null);
        cipher.Init(true, parameters);

        // output = ciphertext + tag
        byte[] cipherAndTag = new byte[cipher.GetOutputSize(plainBytes.Length)];
        int len = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, cipherAndTag, 0);
        len += cipher.DoFinal(cipherAndTag, len);

        // combined = IV + (ciphertext+tag)
        byte[] combined = new byte[iv.Length + cipherAndTag.Length];
        Buffer.BlockCopy(iv,           0, combined, 0,             iv.Length);
        Buffer.BlockCopy(cipherAndTag, 0, combined, iv.Length,     cipherAndTag.Length);

        return Convert.ToBase64String(combined);
    }
}
