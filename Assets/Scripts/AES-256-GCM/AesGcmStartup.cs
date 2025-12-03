using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// nhớ thêm using cho BouncyCastle:
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

public class AesGcmStartup : MonoBehaviour
{
    [Header("AES-256-GCM")]
    [Tooltip("Base64-encoded 32-byte key")]
    public string base64Key = "6jZ4pHq2Q9xT1F6b3vX9W8eKz2nM4rT0yL5vQ7aU0sM=";

    [Header("Payload fixed để test")]
    public long fixedTimestamp = 1764733915;
    public string platform = "lms-3d";

    private void Awake()
    {
        try
        {
            EncryptAndLog();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AES-GCM] LỖI: {ex}");
        }
    }

    private void EncryptAndLog()
    {
        Payload payload = new Payload
        {
            timestamp = fixedTimestamp,
            platform = platform
        };
        string json = JsonUtility.ToJson(payload);

        string cipherB64 = EncryptAesGcm(json, base64Key, out string nonceB64, out string tagB64);
        string packedB64 = PackAll(nonceB64, cipherB64, tagB64);

        Debug.Log($"<color=green>[AES-GCM] Plain JSON:</color> {json}");
        Debug.Log($"[AES-GCM] Nonce  (Base64): {nonceB64}");
        Debug.Log($"[AES-GCM] Cipher (Base64): {cipherB64}");
        Debug.Log($"[AES-GCM] Tag    (Base64): {tagB64}");
        Debug.Log($"<color=yellow>[AES-GCM] PACKED:</color> {packedB64}");
    }

    [Serializable]
    public class Payload
    {
        public long timestamp;
        public string platform;
    }

    /// <summary>
    /// AES-256-GCM với BouncyCastle: trả về cipher Base64 + nonce & tag Base64
    /// </summary>
    public static string EncryptAesGcm(string plainText, string base64Key, out string nonceBase64, out string tagBase64)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256-GCM (256-bit).");

        // Nonce 12 bytes (96-bit) là chuẩn GCM
        byte[] nonce = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        // AES-GCM với BouncyCastle
        var cipher = new GcmBlockCipher(new AesEngine());
        // 128 = tag length (bit)
        var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, null);
        cipher.Init(true, parameters);

        // output = cipher + tag (dính chung)
        byte[] cipherAndTag = new byte[cipher.GetOutputSize(plainBytes.Length)];
        int len = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, cipherAndTag, 0);
        len += cipher.DoFinal(cipherAndTag, len);

        // Tách cipher và tag
        int tagLengthBytes = 16; // 128 bit
        int cipherLength = cipherAndTag.Length - tagLengthBytes;

        byte[] cipherBytes = new byte[cipherLength];
        byte[] tag = new byte[tagLengthBytes];

        Buffer.BlockCopy(cipherAndTag, 0, cipherBytes, 0, cipherLength);
        Buffer.BlockCopy(cipherAndTag, cipherLength, tag, 0, tagLengthBytes);

        nonceBase64 = Convert.ToBase64String(nonce);
        tagBase64 = Convert.ToBase64String(tag);
        string cipherBase64 = Convert.ToBase64String(cipherBytes);
        return cipherBase64;
    }

    /// <summary>
    /// Gộp nonce + cipher + tag thành 1 Base64 duy nhất (tiện gửi server)
    /// </summary>
    public static string PackAll(string nonceB64, string cipherB64, string tagB64)
    {
        byte[] nonce = Convert.FromBase64String(nonceB64);
        byte[] cipher = Convert.FromBase64String(cipherB64);
        byte[] tag = Convert.FromBase64String(tagB64);

        byte[] all = new byte[nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, all, 0, nonce.Length);
        Buffer.BlockCopy(cipher, 0, all, nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, all, nonce.Length + cipher.Length, tag.Length);

        return Convert.ToBase64String(all);
    }
}
