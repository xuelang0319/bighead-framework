using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Bighead.Core.Upzy
{
    /// <summary>
    /// 用于在编辑器中构建和比较 Menu.json
    /// </summary>
    public static class MenuBuilder
    {
        /// <summary>
        /// 构建并保存 Menu.json
        /// </summary>
        public static void BuildAndSave(string outputPath, Func<IEnumerable<ModuleInfo>> collectModules)
        {
            if (collectModules == null)
                throw new ArgumentNullException(nameof(collectModules));

            var newConfig = new MenuConfig
            {
                Version = GetCurrentPackageVersion(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Modules = new List<ModuleInfo>(collectModules())
            };

            MenuConfig? oldConfig = null;
            if (File.Exists(outputPath))
            {
                try
                {
                    oldConfig = MenuLoader.Load(outputPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"读取旧 Menu.json 失败，将覆盖生成：{e.Message}");
                }
            }

            if (oldConfig != null)
            {
                var changedModules = GetChangedModules(oldConfig, newConfig);
                if (changedModules.Count == 0)
                {
                    Debug.Log("没有模块发生变化，跳过构建。");
                    return;
                }

                Debug.Log($"共有 {changedModules.Count} 个模块发生变化：");
                foreach (var module in changedModules)
                    Debug.Log($" - {module.Name} -> {module.Version}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            MenuLoader.Save(outputPath, newConfig);
            Debug.Log($"Menu.json 构建完成 -> {outputPath}");
        }

        /// <summary>
        /// 获取当前整包版本号（占位，按需替换）
        /// </summary>
        private static string GetCurrentPackageVersion()
        {
            return "1.0.0.0";
        }

        /// <summary>
        /// 比较新旧 MenuConfig，返回变更模块列表
        /// </summary>
        private static List<ModuleInfo> GetChangedModules(MenuConfig oldConfig, MenuConfig newConfig)
        {
            var result = new List<ModuleInfo>();
            var oldDict = oldConfig.Modules.ToDictionary(m => m.Name, m => m);

            foreach (var newModule in newConfig.Modules)
            {
                if (!oldDict.TryGetValue(newModule.Name, out var oldModule) ||
                    oldModule.Version != newModule.Version)
                {
                    result.Add(newModule);
                }
            }

            return result;
        }
    }
}
