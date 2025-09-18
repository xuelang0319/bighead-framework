/*using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bighead.Upzy;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Bighead.Core.Upzy
{
    public static class UpzyUpdater
    {
        /// <summary>
        /// 热更主流程：拉取 Server Menu → 比对 → 下载差异 → 校验 → 原子切换
        /// </summary>
        public static async UniTask RunHotfixAsync(
            IBdCodec codec,
            UpzyConfig config,
            string serverMenuUrl,
            Action onChanged = null)
        {
            // 1) 拉取 Server Menu
            var serverBytes = await DownloadBytesAsync(serverMenuUrl);
            if (serverBytes == null)
            {
                Debug.LogError("[UpzyUpdater] 下载 Server Menu 失败，终止热更");
                return;
            }

            var serverMenu = codec.Decode<WorkingMenu>(serverBytes);

            // 2) 读取当前 Menu（必须先初始化）
            if (!File.Exists(config.MenuFile))
            {
                Debug.LogError("[UpzyUpdater] 当前 Menu 不存在，请先 UpzyInitializer.InitializeAsync");
                return;
            }

            var currentMenu = codec.Decode<WorkingMenu>(File.ReadAllBytes(config.MenuFile));

            // 3) （占位）差异模块判定：此处仍按 config 路径做最小比对
            //    如需按版本比对，可先只下载各模块的 .bd 到 staging，再用 LoadModuleConfig 读取版本后决定是否拉文件。
            var diffModules = serverMenu.modules
                .Where(r => currentMenu.modules.All(l => l.config != r.config))
                .ToArray();

            if (diffModules.Length == 0)
            {
                Debug.Log("[UpzyUpdater] 无需更新");
                return;
            }

            // 4) 下载到 staging（此处仅占位，按你的实际下载实现替换）
            if (Directory.Exists(config.StagingDir)) Directory.Delete(config.StagingDir, true);
            Directory.CreateDirectory(config.StagingDir);

            foreach (var m in diffModules)
            {
                // 下载逻辑：应将 m.config（模块 .bd）和该模块文件内容
                // 放到 staging 目录下：
                //   - 模块配置：  <staging>/<m.config>  (比如 staging/Modules/A/A.bd)
                //   - 模块文件：  <staging>/<moduleName>/<fileName>
                await DownloadModuleAsync(m, config.StagingDir);
            }

            // 5) 校验（逐模块读取 staging 内的 .bd，然后校验列出的所有文件）
            bool allOk = true;
            foreach (var m in diffModules)
            {
                ModuleConfig modCfg;
                try
                {
                    modCfg = LoadModuleConfig(codec, config.StagingDir, m.config);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UpzyUpdater] 读取模块配置失败: {m.config} - {e.Message}");
                    allOk = false;
                    break;
                }

                if (!VerifyModule(modCfg, config.StagingDir))
                {
                    Debug.LogError($"[UpzyUpdater] 模块校验失败: {modCfg.moduleName}");
                    allOk = false;
                    break;
                }
            }

            if (!allOk)
            {
                Debug.LogError("[UpzyUpdater] 校验未通过，终止切换");
                return;
            }

            // 6) 原子切换
            SwitchToStaging(config, onChanged);
        }

        /// <summary>回滚到上一个版本</summary>
        public static void Rollback(UpzyConfig config, Action onChanged = null)
        {
            if (!Directory.Exists(config.BackupDir))
            {
                Debug.LogWarning("[UpzyUpdater] 没有可回滚的备份");
                return;
            }

            if (Directory.Exists(config.CurrentDir)) Directory.Delete(config.CurrentDir, true);
            Directory.Move(config.BackupDir, config.CurrentDir);

            Debug.Log("[UpzyUpdater] 已回滚到上一个版本");
            onChanged?.Invoke();
        }

        /// <summary>从 baseDir + relativeConfigPath 读取 .bd 并解码成 ModuleConfig</summary>
        private static ModuleConfig LoadModuleConfig(IBdCodec codec, string baseDir, string relativeConfigPath)
        {
            var abs = Path.Combine(baseDir, relativeConfigPath);
            if (!File.Exists(abs))
                throw new FileNotFoundException($"ModuleConfig 丢失: {abs}");

            var bytes = File.ReadAllBytes(abs);
            var cfg = codec.Decode<ModuleConfig>(bytes);
            if (cfg == null)
                throw new InvalidDataException($"ModuleConfig 解析失败: {abs}");
            return cfg;
        }

        /// <summary>校验模块：逐文件比对哈希和大小，并校验聚合哈希</summary>
        private static bool VerifyModule(ModuleConfig module, string baseDir)
        {
            if (module.files == null || module.files.Length == 0)
            {
                Debug.LogError($"[UpzyUpdater] 模块 {module.moduleName} 未包含文件列表");
                return false;
            }

            foreach (var f in module.files)
            {
                // 约定：下载落地路径为 <baseDir>/<moduleName>/<fileName>
                var abs = Path.Combine(baseDir, module.moduleName, f.fileName);
                if (!File.Exists(abs))
                {
                    Debug.LogError($"[UpzyUpdater] 缺失文件: {abs}");
                    return false;
                }

                var size = new FileInfo(abs).Length;
                if (size != f.size)
                {
                    Debug.LogError($"[UpzyUpdater] 大小不匹配: {abs} 期望 {f.size} 实际 {size}");
                    return false;
                }

                var hash = ComputeFileHash(abs);
                if (!hash.Equals(f.hash, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[UpzyUpdater] 哈希不匹配: {abs}");
                    return false;
                }
            }

            // 聚合哈希校验
            var agg = ComputeModuleHash(module.files);
            if (!agg.Equals(module.hash, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[UpzyUpdater] 模块聚合哈希不匹配: {module.moduleName}");
                return false;
            }

            return true;
        }

        /// <summary>下载字节（占位，可替换为你自己的下载器）</summary>
        private static async UniTask<byte[]> DownloadBytesAsync(string url)
        {
            using var req = UnityWebRequest.Get(url);
            await req.SendWebRequest();
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.data : null;
        }

        /// <summary>占位：下载模块（应把模块 .bd 和文件落到 staging 下既定路径）</summary>
        private static async UniTask DownloadModuleAsync(ModuleEntry module, string stagingDir)
        {
            await UniTask.Yield();
            Debug.Log($"[UpzyUpdater] 下载模块（占位）: {module.name}");
        }

        private static void SwitchToStaging(UpzyConfig config, Action onChanged)
        {
            if (Directory.Exists(config.BackupDir)) Directory.Delete(config.BackupDir, true);
            if (Directory.Exists(config.CurrentDir))
                Directory.Move(config.CurrentDir, config.BackupDir);

            Directory.Move(config.StagingDir, config.CurrentDir);
            Debug.Log("[UpzyUpdater] 已切换到新版本");
            onChanged?.Invoke();
        }

        // ---- 运行时哈希工具（避免引用 Editor 代码） ----
        private static string ComputeFileHash(string absPath)
        {
            using var stream = File.OpenRead(absPath);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private static string ComputeModuleHash(ModuleConfig.ModuleFile[] files)
        {
            var concat = string.Join("",
                files.OrderBy(f => f.fileName)
                    .Select(f => f.hash));
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concat));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}*/