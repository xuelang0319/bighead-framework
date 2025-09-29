using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Shared;

public static class Crypto
{
    // 计算明文的 SHA256（用于完整性校验）
    public static string Sha256Hex(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash); // 大写HEX
    }

    // HMAC-SHA256（对“认证串”签名，防篡改+简易鉴权）
    public static string HmacSha256Base64(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    // AES-GCM 加密（.NET 5+）
    // 返回 (cipherText, nonce, tag)
    public static (byte[] cipher, byte[] nonce, byte[] tag) AesGcmEncrypt(byte[] plain, string key)
    {
        var keyBytes = DeriveKey32(key);               // 32字节密钥
        var nonce = RandomNumberGenerator.GetBytes(12); // 12字节推荐
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(keyBytes);
        aes.Encrypt(nonce, plain, cipher, tag);
        return (cipher, nonce, tag);
    }

    public static byte[] AesGcmDecrypt(byte[] cipher, byte[] nonce, byte[] tag, string key)
    {
        var keyBytes = DeriveKey32(key);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(keyBytes);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    // 将任意长度密钥派生为固定32字节（简化：SHA256）
    private static byte[] DeriveKey32(string key)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    }
}