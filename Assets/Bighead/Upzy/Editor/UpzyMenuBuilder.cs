#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Bighead.Upzy.Core;
using Bighead.Upzy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bighead.Upzy.Editor
{
    public static class UpzyMenuBuilder
    {
        /// <summary>构建单个模块：更新产物、递增版本号、写 current/*.bd</summary>
        public static void BuildModule(UpzySetting setting, UpzyModuleSO so)
        {
            var buildable = CreateBuildableFromConfigSO(so);
            if (buildable == null)
            {
                Debug.LogWarning($"模块 {so.name} 无法构建，未找到 Buildable。");
                return;
            }

            var result = buildable.Build(setting.ModulesAbs(setting.CurrentAbs));
            if (result.changeLevel == ChangeLevel.None)
            {
                Debug.Log($"模块 {so.name} 无变化，跳过构建。");
                return;
            }

            if (HasChanged(setting, so, result))
            {
                so.version = IncrementVersion(so.version, result.changeLevel);
                EditorUtility.SetDirty(so);
                WriteModuleBd(setting, so, result);
                Debug.Log($"模块 {so.name} 构建完成，新版本号：{so.version}");
            }
            else
            {
                Debug.Log($"模块 {so.name} 构建产物与上次一致，未更新版本号。");
            }
        }

        /// <summary>构建所有模块</summary>
        public static void BuildAll(UpzySetting setting)
        {
            foreach (var entry in setting.registeredModules)
            {
                if (entry.configSO is UpzyModuleSO so)
                    BuildModule(setting, so);   // ✅ 强转后再传入
            }
        }

        /// <summary>发版：比对模块版本和 builtin 版本，决定是否生成新 Menu.bd</summary>
        public static void Publish(UpzySetting setting, bool isFullBuild)
        {
            var builtinVersion = ReadBuiltinVersionXYZ();
            var lastMenu = LoadLastMenu(setting);
            var needWrite = false;

            // 1. builtin X.Y.Z 变了 → 必须重写 Menu
            if (lastMenu == null || lastMenu.meta.version.major != builtinVersion.major ||
                lastMenu.meta.version.minor != builtinVersion.minor ||
                lastMenu.meta.version.feature != builtinVersion.feature)
            {
                needWrite = true;
            }
            else
            {
                // 2. 有任何模块版本号比 Menu 记录的更新 → 必须重写 Menu
                var currentModules = LoadAllModules(setting);
                foreach (var m in currentModules)
                {
                    var last = lastMenu.modules.FirstOrDefault(x => x.moduleName == m.moduleName);
                    if (last == null || m.version > last.version)
                    {
                        needWrite = true;
                        break;
                    }
                }
            }

            if (!needWrite)
            {
                Debug.Log("无模块变更，无需发版。");
                return;
            }

            // 3. 更新 Menu 的 W
            var newVersion = builtinVersion;
            newVersion.patch = isFullBuild ? 1 : (lastMenu?.meta.version.patch ?? 0) + 1;

            WriteMenuBd(setting, newVersion);
            Debug.Log($"已生成新 Menu.bd，版本号：{newVersion}");
        }

        // --- 以下方法保持不变或简化实现 ---

        public static UpzyBuildableBase CreateBuildableFromConfigSO(UpzyModuleSO so)
        {
            var typeName = so.GetType().Name.Replace("SO", "");
            var type = TypeCache.GetTypesDerivedFrom<UpzyBuildableBase>()
                .FirstOrDefault(t => t.Name == typeName);

            if (type == null) return null;
            var instance = (UpzyBuildableBase)System.Activator.CreateInstance(type);
            instance.SetConfig(so);
            return instance;
        }

        public static bool HasChanged(UpzySetting setting, UpzyModuleSO so, BuildResult result)
        {
            var lastBdPath = Path.Combine(GetModulesOutputRoot(setting), $"{so.name}.bd");
            if (!File.Exists(lastBdPath)) return true;

            var last = JsonUtility.FromJson<ModuleBd>(File.ReadAllText(lastBdPath));
            if (last == null) return true;

            return result.aggregateHash != last.aggregateHash;
        }

        private static string GetModulesOutputRoot(UpzySetting setting)
        {
            var dir = setting.ModulesAbs(setting.CurrentAbs);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void WriteModuleBd(UpzySetting setting, UpzyModuleSO so, BuildResult result)
        {
            var dir = GetModulesOutputRoot(setting);
            var bd = new ModuleBd
            {
                moduleName    = so.name,
                version       = so.version,
                aggregateHash = result.aggregateHash,
                entries       = result.entries
            };
            File.WriteAllText(Path.Combine(dir, $"{so.name}.bd"), JsonUtility.ToJson(bd, true));
        }

        private static UpzyMenu LoadLastMenu(UpzySetting setting)
        {
            var path = Path.Combine(setting.CurrentAbs, "Menu.bd");
            return File.Exists(path)
                ? JsonUtility.FromJson<UpzyMenu>(File.ReadAllText(path))
                : null;
        }

        private static UpzyMenu.ModuleInfo[] LoadAllModules(UpzySetting setting)
        {
            var dir = GetModulesOutputRoot(setting);
            if (!Directory.Exists(dir)) return new UpzyMenu.ModuleInfo[0];

            return Directory.GetFiles(dir, "*.bd")
                .Select(p =>
                {
                    var bd = JsonUtility.FromJson<ModuleBd>(File.ReadAllText(p));
                    return new UpzyMenu.ModuleInfo
                    {
                        moduleName      = bd.moduleName,
                        version         = bd.version,
                        moduleBdRelPath = Path.GetFileName(p),
                        aggregateHash   = bd.aggregateHash
                    };
                }).ToArray();
        }

        private static ConfigVersion ReadBuiltinVersionXYZ()
        {
            // 这里可以从 Application.streamingAssetsPath 读取内置 Menu 或版本号
            return new ConfigVersion(1, 0, 0, 0); // 示例
        }

        public static ConfigVersion IncrementVersion(ConfigVersion current, ChangeLevel level)
        {
            switch (level)
            {
                case ChangeLevel.Major:   return new ConfigVersion(current.major + 1, 0, 0, 0);
                case ChangeLevel.Minor:   return new ConfigVersion(current.major, current.minor + 1, 0, 0);
                case ChangeLevel.Feature: return new ConfigVersion(current.major, current.minor, current.feature + 1, 0);
                case ChangeLevel.Patch:   return new ConfigVersion(current.major, current.minor, current.feature, current.patch + 1);
                default: return current;
            }
        }

        private static void WriteMenuBd(UpzySetting setting, ConfigVersion version)
        {
            var menu = new UpzyMenu
            {
                meta = new UpzyMenu.Meta
                {
                    version     = version,
                    generatedAt = System.DateTime.Now.ToString("O")
                },
                modules = LoadAllModules(setting)
            };
            Directory.CreateDirectory(setting.CurrentAbs);
            File.WriteAllText(Path.Combine(setting.CurrentAbs, "Menu.bd"), JsonUtility.ToJson(menu, true));
        }
    }
}
#endif
