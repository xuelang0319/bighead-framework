using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Bighead.BuildSystem
{
    public class UploadExecutor
    {
        private readonly string exePath;
        private readonly string secretKey;

        public UploadExecutor(string exePath, string secretKey)
        {
            this.exePath = exePath;
            this.secretKey = secretKey;
        }

        public void Upload(string folderPath, string serverUrl)
        {
            if (!System.IO.File.Exists(exePath))
            {
                Debug.LogError($"[UploadExecutor] 找不到 ClientUploader.exe: {exePath}");
                return;
            }

            string arguments = $"\"{folderPath}\" \"{serverUrl}\" \"{secretKey}\"";
            Debug.Log($"[UploadExecutor] 开始上传: {arguments}");

            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log($"[Client] {e.Data}");
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogError($"[Client-ERR] {e.Data}");
            };

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                int exitCode = process.ExitCode;
                if (exitCode == 0)
                    Debug.Log($"[UploadExecutor] 上传完成，退出码: {exitCode}");
                else
                    Debug.LogError($"[UploadExecutor] 上传失败，退出码: {exitCode}");
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
    }
}