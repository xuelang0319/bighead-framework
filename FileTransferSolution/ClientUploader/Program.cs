using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ClientUploader;
using Shared;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("用法: ClientUploader.exe <文件夹路径> <服务器URL> <密钥>");
            return;
        }

        Console.OutputEncoding = Encoding.UTF8;
        
        string folderPath = args[0];
        string serverUrl = args[1];
        string sharedSecret = args[2];

        Console.WriteLine($"[Client] Zipping folder: {folderPath}");

        var sw = Stopwatch.StartNew();
        string zipPath = Zipper.ZipFolder(folderPath);
        Console.WriteLine($"[Client] 压缩完成，用时 {sw.ElapsedMilliseconds} ms");

        byte[] zipBytes = await File.ReadAllBytesAsync(zipPath);
        Console.WriteLine($"[Client] ZIP 大小: {zipBytes.Length} 字节");

        string sha256Hex = Crypto.Sha256Hex(zipBytes);
        Console.WriteLine($"[Client] SHA256 = {sha256Hex}");

        var (cipher, nonce, tag) = Crypto.AesGcmEncrypt(zipBytes, sharedSecret);

        string fileName = Path.GetFileName(zipPath);
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string reqNonceB64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string nonceB64 = Convert.ToBase64String(nonce);
        string tagB64 = Convert.ToBase64String(tag);

        string message = Protocol.BuildAuthMessage(
            fileName, sha256Hex, timestamp, reqNonceB64, nonceB64, tagB64, cipher.LongLength);

        string signatureB64 = Crypto.HmacSha256Base64(message, sharedSecret);

        // 打印调试信息
        Console.WriteLine("[Client] ====== Signing Info ======");
        Console.WriteLine($"FileName   : {fileName}");
        Console.WriteLine($"SHA256     : {sha256Hex}");
        Console.WriteLine($"Timestamp  : {timestamp}");
        Console.WriteLine($"ReqNonce   : {reqNonceB64}");
        Console.WriteLine($"Nonce      : {nonceB64}");
        Console.WriteLine($"Tag        : {tagB64}");
        Console.WriteLine($"BodyLength : {cipher.LongLength}");
        Console.WriteLine($"Message    : {message}");
        Console.WriteLine($"Signature  : {signatureB64}");
        Console.WriteLine("[Debug] Secret HEX: " + BitConverter.ToString(Encoding.UTF8.GetBytes(sharedSecret)));
        Console.WriteLine("[Client] ==========================");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(100);

        // 使用带进度的内容
        var content = new ProgressableStreamContent(cipher, 8192, (sent, total) =>
        {
            double percent = (double)sent / total * 100;
            Console.Write($"\r[Client] Uploading... {percent:F1}%   ");
        });

        content.Headers.Add(Protocol.H_FileName, fileName);
        content.Headers.Add(Protocol.H_Sha256, sha256Hex);
        content.Headers.Add(Protocol.H_Nonce, nonceB64);
        content.Headers.Add(Protocol.H_Tag, tagB64);
        content.Headers.Add(Protocol.H_Timestamp, timestamp);
        content.Headers.Add(Protocol.H_NonceRand, reqNonceB64);
        content.Headers.Add(Protocol.H_Signature, signatureB64);

        Console.WriteLine();
        Console.WriteLine($"[Client] Uploading to {serverUrl} ...");

        try
        {
            var response = await client.PostAsync(serverUrl, content);
            string serverReply = await response.Content.ReadAsStringAsync();
            Console.WriteLine();
            Console.WriteLine($"[Client] Status: {(int)response.StatusCode}");
            Console.WriteLine($"[Client] Server says: {serverReply}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("\n[Client] Upload failed: 超时，服务器可能没有响应。");
        }
    }
}
