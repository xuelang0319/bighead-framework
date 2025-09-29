using System.IO.Compression;
using System.Net;
using System.Text;
using Shared;

Console.OutputEncoding = Encoding.UTF8;
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

// 参数解析
string prefix       = args.Length > 0 ? $"http://+:{args[0]}/" : "http://+:8080/";
string sharedSecret = args.Length > 1 ? args[1] : "CHANGE_ME_SECRET";
string saveRoot     = args.Length > 2 ? args[2] : Path.Combine(AppContext.BaseDirectory, "Uploads");
string extractRoot  = args.Length > 3 ? args[3] : Path.Combine(AppContext.BaseDirectory, "Extracted");

// ===== Debug 输出参数 =====
Console.WriteLine($"[Debug] Args count = {args.Length}");
for (int i = 0; i < args.Length; i++)
    Console.WriteLine($"[Debug] Arg[{i}] = {args[i]}");

Directory.CreateDirectory(saveRoot);
Directory.CreateDirectory(extractRoot);

Console.WriteLine($"[Server] SharedSecret: {sharedSecret}");
Console.WriteLine($"[Server] Listening on {prefix}");
Console.WriteLine($"[Server] SaveRoot: {saveRoot}");
Console.WriteLine($"[Server] ExtractRoot: {extractRoot}");

// 启动监听
var listener = new HttpListener();
listener.Prefixes.Add(prefix);
listener.Start();

while (true)
{
    var ctx = listener.GetContext();
    if (ctx.Request.HttpMethod != "POST" || ctx.Request.Url?.AbsolutePath != "/upload")
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Not Found"));
        ctx.Response.Close();
        continue;
    }

    try
    {
        // 读取头部
        string? fileName = ctx.Request.Headers[Protocol.H_FileName];
        string? sha256Hex = ctx.Request.Headers[Protocol.H_Sha256];
        string? nonceB64 = ctx.Request.Headers[Protocol.H_Nonce];
        string? tagB64 = ctx.Request.Headers[Protocol.H_Tag];
        string? ts = ctx.Request.Headers[Protocol.H_Timestamp];
        string? reqNonceB64 = ctx.Request.Headers[Protocol.H_NonceRand];
        string? signB64 = ctx.Request.Headers[Protocol.H_Signature];

        if (fileName == null || sha256Hex == null || nonceB64 == null ||
            tagB64 == null || ts == null || reqNonceB64 == null || signB64 == null)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Missing headers"));
            ctx.Response.Close();
            continue;
        }

        // 读取请求体
        using var ms = new MemoryStream();
        ctx.Request.InputStream.CopyTo(ms);
        var cipher = ms.ToArray();

        // 签名校验
        var message = Protocol.BuildAuthMessage(
            fileName, sha256Hex, ts, reqNonceB64, nonceB64, tagB64, cipher.LongLength);

        Console.WriteLine("[Server] ====== Signing Info ======");
        Console.WriteLine($"FileName   : {fileName}");
        Console.WriteLine($"SHA256     : {sha256Hex}");
        Console.WriteLine($"Timestamp  : {ts}");
        Console.WriteLine($"ReqNonce   : {reqNonceB64}");
        Console.WriteLine($"Nonce      : {nonceB64}");
        Console.WriteLine($"Tag        : {tagB64}");
        Console.WriteLine($"BodyLength : {cipher.LongLength}");
        Console.WriteLine($"Message    : {message}");
        Console.WriteLine("[Debug] Secret HEX: " + BitConverter.ToString(Encoding.UTF8.GetBytes(sharedSecret)));
        Console.WriteLine("[Server] ==========================");

        var expectedSign = Crypto.HmacSha256Base64(message, sharedSecret);
        Console.WriteLine($"[Server] ExpectedSig: {expectedSign}");
        Console.WriteLine($"[Server] ClientSig  : {signB64}");

        if (!expectedSign.Equals(signB64, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid signature"));
            ctx.Response.Close();
            continue;
        }

        // 解密
        byte[] plain;
        try
        {
            plain = Crypto.AesGcmDecrypt(cipher,
                Convert.FromBase64String(nonceB64),
                Convert.FromBase64String(tagB64),
                sharedSecret);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Server] Decrypt failed: " + ex.Message);
            ctx.Response.StatusCode = 400;
            await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Decrypt failed"));
            ctx.Response.Close();
            continue;
        }

        // 校验SHA256
        var realSha = Crypto.Sha256Hex(plain);
        if (!realSha.Equals(sha256Hex, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Checksum mismatch"));
            ctx.Response.Close();
            continue;
        }

        // 保存ZIP文件（带时间戳）
        var savePath = Path.Combine(saveRoot, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}");
        await File.WriteAllBytesAsync(savePath, plain);
        Console.WriteLine($"[Server] 文件已保存到: {savePath}");

        // 清空旧解压目录
        if (Directory.Exists(extractRoot))
        {
            Console.WriteLine("[Server] 清理旧解压目录...");
            Directory.Delete(extractRoot, true);
        }
        Directory.CreateDirectory(extractRoot);

        // 解压到固定目录
        ZipFile.ExtractToDirectory(savePath, extractRoot, overwriteFiles: true);

        ctx.Response.StatusCode = 200;
        string okMessage = $"OK: saved={savePath}, extracted={extractRoot}";
        await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(okMessage));
        ctx.Response.Close();

        int fileCount = Directory.GetFiles(extractRoot, "*", SearchOption.AllDirectories).Length;
        Console.WriteLine($"[Server] 已解压 {fileCount} 个文件到: {extractRoot}");
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Server error: " + ex.Message));
        ctx.Response.Close();
        Console.WriteLine("[Server] Error: " + ex);
    }
}
