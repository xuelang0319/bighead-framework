#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bighead.Csv
{
    public interface IBytesEncoder
    {
        byte[] Encode(byte[] raw, CsvSettings s);
    }

    public sealed class BytesEncoder : IBytesEncoder
    {
        public byte[] Encode(byte[] raw, CsvSettings s)
        {
            byte[] data = raw ?? Array.Empty<byte>();

            // 1) 文件数据格式（Raw / Base64 / GZipBase64）
            switch (s.BytesFormat)
            {
                case BytesDataFormat.Base64:
                    data = Encoding.ASCII.GetBytes(Convert.ToBase64String(data));
                    break;
                case BytesDataFormat.GZipBase64:
                    using (var ms = new MemoryStream())
                    {
                        using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                            gz.Write(data, 0, data.Length);
                        data = Encoding.ASCII.GetBytes(Convert.ToBase64String(ms.ToArray()));
                    }

                    break;
                case BytesDataFormat.Raw:
                default:
                    break;
            }

            // 2) 若勾选则加密（默认优先 AES-GCM；不支持时回退 XOR）
            if (s.EnableEncryption)
            {
                var key = ResolveKey(s);
                data = AesGcmOrXor(data, key);
            }

            // 3) CRC（可选；覆盖加密后的结果）
            if (s.BytesEnableCRC)
                data = AttachCrc32(data);

            return data;
        }

        private static string ResolveKey(CsvSettings s)
        {
            var k = s.EncryptionKeyProvider ? s.EncryptionKeyProvider.GetKey() : null;
            if (string.IsNullOrEmpty(k)) k = s.FixedEncryptionKey;
            if (string.IsNullOrEmpty(k)) k = "bighead-default-key";
            return k;
        }

        private static byte[] AttachCrc32(byte[] data)
        {
            uint crc = Crc32(data);
            var buf = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, buf, 4, data.Length);
            buf[0] = (byte)(crc & 0xFF);
            buf[1] = (byte)((crc >> 8) & 0xFF);
            buf[2] = (byte)((crc >> 16) & 0xFF);
            buf[3] = (byte)((crc >> 24) & 0xFF);
            return buf;
        }
        
        private static uint Crc32(byte[] data)
        {
            const uint poly = 0xEDB88320u;
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
            }
            return ~crc;
        }


#if UNITY_2020_1_OR_NEWER
        private static byte[] AesGcmOrXor(byte[] plaintext, string keyStr)
        {
            try
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var key = sha.ComputeHash(Encoding.UTF8.GetBytes(keyStr ?? "bighead"));
                var nonce = sha.ComputeHash(Encoding.UTF8.GetBytes((keyStr ?? "bighead") + ":nonce"))[..12];
                var tag = new byte[16];
                var cipher = new byte[plaintext.Length];
                using var aes = new System.Security.Cryptography.AesGcm(key);
                aes.Encrypt(nonce, plaintext, cipher, tag);
                var outBuf = new byte[12 + 16 + cipher.Length];
                Buffer.BlockCopy(nonce, 0, outBuf, 0, 12);
                Buffer.BlockCopy(tag, 0, outBuf, 12, 16);
                Buffer.BlockCopy(cipher, 0, outBuf, 28, cipher.Length);
                return outBuf;
            }
            catch
            {
                return Xor(plaintext, Encoding.UTF8.GetBytes(keyStr ?? ""));
            }
        }
#else
    private static byte[] AesGcmOrXor(byte[] plaintext, string keyStr)
    {
        return Xor(plaintext, Encoding.UTF8.GetBytes(keyStr ?? ""));
    }
#endif

        private static byte[] Xor(byte[] src, byte[] key)
        {
            if (key == null || key.Length == 0) return src;
            var dst = new byte[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = (byte)(src[i] ^ key[i % key.Length]);
            return dst;
        }

        // Crc32(...) 保留你现有实现
    }
}
#endif