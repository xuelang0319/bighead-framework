// Server/DeployHost.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Bighead.Deploy
{
    public class DeployHost
    {
        private readonly HttpListener _listener = new();
        private readonly string _root;         // 线上资源根，如 D:\FileRoot
        private readonly string _staging;      // 临时目录，如 D:\FileRoot\.staging
        private readonly string _token;        // 简易鉴权

        public DeployHost(string prefix, string rootDir, string token)
        {
            _root = Path.GetFullPath(rootDir);
            _staging = Path.Combine(_root, ".staging");
            _token = token;
            Directory.CreateDirectory(_root);
            Directory.CreateDirectory(_staging);

            _listener.Prefixes.Add(prefix);    // 例如 http://+:8081/
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"[DeployHost] Listening on: {string.Join(",", _listener.Prefixes)}");
            _ = AcceptLoop();
        }

        public void Stop() => _listener.Stop();

        private async Task AcceptLoop()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => Handle(ctx));
                }
                catch { if (!_listener.IsListening) break; }
            }
        }

        private void WriteJson(HttpListenerResponse resp, int code, string json)
        {
            resp.StatusCode = code;
            resp.ContentType = "application/json; charset=utf-8";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.Close();
        }

        private async Task Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath.TrimEnd('/') == "/deploy")
            {
                // 简易鉴权
                if (!req.Headers["Authorization"]?.Equals($"Bearer {_token}", StringComparison.Ordinal) ?? true)
                {
                    WriteJson(resp, 401, "{\"error\":\"unauthorized\"}");
                    return;
                }

                // 目标子目录（例如 Windows/1.0.3 或直接 Windows）
                var targetSub = req.QueryString["subdir"] ?? "default";
                if (targetSub.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    WriteJson(resp, 400, "{\"error\":\"invalid subdir\"}");
                    return;
                }

                string zipTemp = Path.Combine(_staging, Guid.NewGuid() + ".zip");
                string extractDir = Path.Combine(_staging, Guid.NewGuid().ToString());
                Directory.CreateDirectory(_staging);
                Directory.CreateDirectory(extractDir);

                try
                {
                    // 保存上传zip
                    using (var fs = File.Create(zipTemp))
                        await req.InputStream.CopyToAsync(fs);

                    // 解压到 staging
                    ZipFile.ExtractToDirectory(zipTemp, extractDir);

                    // 原子替换：先备份，后替换
                    string target = Path.Combine(_root, targetSub);
                    string backup = target + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    if (Directory.Exists(target))
                        Directory.Move(target, backup);

                    Directory.Move(extractDir, target); // 原子移动
                    if (Directory.Exists(backup))
                        Directory.Delete(backup, true);

                    WriteJson(resp, 200, "{\"ok\":true}");
                }
                catch (Exception ex)
                {
                    // 回滚 staging
                    try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch {}
                    try { if (File.Exists(zipTemp)) File.Delete(zipTemp); } catch {}
                    WriteJson(resp, 500, $"{{\"error\":\"{Escape(ex.Message)}\"}}");
                }
                finally
                {
                    try { if (File.Exists(zipTemp)) File.Delete(zipTemp); } catch {}
                }
                return;
            }

            WriteJson(resp, 404, "{\"error\":\"not found\"}");
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            // 示例：DeployHost http://+:8081/  D:\FileRoot  SECRET_TOKEN
            string prefix = args.Length > 0 ? args[0] : "http://+:8081/";
            string root   = args.Length > 1 ? args[1] : @"D:\FileRoot";
            string token  = args.Length > 2 ? args[2] : "SECRET_TOKEN";

            var host = new DeployHost(prefix, root, token);
            host.Start();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
            host.Stop();
        }
    }
}
