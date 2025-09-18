/*
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bighead.Upzy;
using UnityEngine;

namespace Bighead.Core.Upzy
{
    public static class MenuBuilder
    {
        public static void BuildMenu(UpzyConfig config, ModuleConfig[] allModules, ConfigVersion buildVersion)
        {
            if (Directory.Exists(config.CurrentDir)) Directory.Delete(config.CurrentDir, true);
            Directory.CreateDirectory(config.CurrentDir);
            Directory.CreateDirectory(config.ModulesDir);

            var menu = new WorkingMenu
            {
                meta = new Meta
                {
                    version = buildVersion,
                    generatedAt = System.DateTime.UtcNow.ToString("O")
                },
                modules = new ModuleEntry[allModules.Length]
            };

            foreach (var (module, i) in allModules.Select((m, i) => (m, i)))
            {
                // 1. 收集模块文件信息
                string moduleDir = Path.Combine("Assets/Modules", module.moduleName); // 需按你的项目调整
                var files = Directory.Exists(moduleDir)
                    ? Directory.GetFiles(moduleDir, "*", SearchOption.AllDirectories)
                    : new string[0];

                module.files = files.Select(f => new ModuleConfig.ModuleFile
                {
                    fileName = Path.GetRelativePath(moduleDir, f).Replace("\\", "/"),
                    hash = ComputeFileHash(f),
                    size = new FileInfo(f).Length
                }).ToArray();

                // 2. 计算模块整体哈希
                module.hash = ComputeModuleHash(module.files);

                // 3. 序列化保存
                string moduleFileName = $"{module.moduleName}.bd";
                string relPath = Path.Combine(config.modulesFolder, moduleFileName);
                string dst = Path.Combine(config.CurrentDir, relPath);
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);

                File.WriteAllText(dst, JsonUtility.ToJson(module, true));

                menu.modules[i] = new ModuleEntry { name = module.moduleName, config = relPath };
            }

            // 写入 Menu
            File.WriteAllText(config.MenuFile, JsonUtility.ToJson(menu, true));

            Debug.Log($"[MenuBuilder] Menu v{buildVersion} 生成完成，模块数={allModules.Length}");
        }

        private static string ComputeFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private static string ComputeModuleHash(ModuleConfig.ModuleFile[] files)
        {
            // 按文件名排序再拼接哈希，保证跨平台一致
            var concat = string.Join("", files.OrderBy(f => f.fileName).Select(f => f.hash));
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concat));
            return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
*/
