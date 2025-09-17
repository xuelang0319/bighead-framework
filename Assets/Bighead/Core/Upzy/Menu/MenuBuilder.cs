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
        public static void BuildAndSave(string outputPath, Func<IEnumerable<ModuleInfo>> collectModules,
            VersionBumpLevel bumpLevel = VersionBumpLevel.Patch)
        {
            if (collectModules == null)
                throw new ArgumentNullException(nameof(collectModules));

            // 先收集新模块列表
            var newModules = new List<ModuleInfo>(collectModules());

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

            // 如果有旧版本，先比较模块是否有变化
            if (oldConfig != null)
            {
                var changedModules = GetChangedModules(oldConfig, newModules);
                if (changedModules.Count == 0)
                {
                    Debug.Log("没有模块发生变化，跳过构建。");
                    return;
                }

                // 递增版本号
                var newVersion = BumpVersion(oldConfig.Version, bumpLevel);

                Debug.Log($"共有 {changedModules.Count} 个模块发生变化：");
                foreach (var module in changedModules)
                    Debug.Log($" - {module.Name} -> {module.Version}");

                SaveMenu(outputPath, newModules, newVersion);
            }
            else
            {
                // 没有旧版本，从初始版本开始
                SaveMenu(outputPath, newModules, "1.0.0.0");
            }
        }

        private static void SaveMenu(string path, List<ModuleInfo> modules, string version)
        {
            var newConfig = new MenuConfig
            {
                Version = version,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Modules = modules
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            MenuLoader.Save(path, newConfig);
            Debug.Log($"Menu.json 构建完成 -> {path}");
        }

        /// <summary>
        /// 根据变更等级递增版本号
        /// </summary>
        private static string BumpVersion(string oldVersion, VersionBumpLevel level)
        {
            var v = VersionNumber.Parse(oldVersion);

            switch (level)
            {
                case VersionBumpLevel.Major:
                    v.Major++;
                    v.Module = v.Feature = v.Patch = 0;
                    break;
                case VersionBumpLevel.Module:
                    v.Module++;
                    v.Feature = v.Patch = 0;
                    break;
                case VersionBumpLevel.Feature:
                    v.Feature++;
                    v.Patch = 0;
                    break;
                default:
                    v.Patch++;
                    break;
            }

            return v.ToString();
        }

        private static List<ModuleInfo> GetChangedModules(MenuConfig oldConfig, List<ModuleInfo> newModules)
        {
            var result = new List<ModuleInfo>();
            var oldDict = oldConfig.Modules.ToDictionary(m => m.Name, m => m);

            foreach (var newModule in newModules)
            {
                if (!oldDict.TryGetValue(newModule.Name, out var oldModule) ||
                    oldModule.Version != newModule.Version)
                {
                    result.Add(newModule);
                }
            }

            return result;
        }

        /// <summary>
        /// 版本号递增等级
        /// </summary>
        public enum VersionBumpLevel
        {
            Patch,
            Feature,
            Module,
            Major
        }
    }
}
