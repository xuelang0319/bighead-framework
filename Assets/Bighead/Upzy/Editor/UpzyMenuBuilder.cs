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
        // =============== 公共入口 ===============
        public static void BuildModule(UpzySetting setting, UpzyModuleSO so)
        {
            EnsureLatestDir(setting);

            var buildable = CreateBuildableFromConfigSO(so);
            if (buildable == null)
            {
                Debug.LogWarning($"模块 {so.name} 无法构建，未找到 Buildable。");
                return;
            }

            var result = buildable.Build(setting.ModulesAbs(setting.LatestAbs));

            string lastBdPath = Path.Combine(setting.ModulesAbs(setting.LatestAbs), $"{so.name}.bd");
            bool bdMissing = !File.Exists(lastBdPath);

            if (result.changeLevel == ChangeLevel.None && !bdMissing)
            {
                Debug.Log($"模块 {so.name} 无变化，跳过构建。");
                return;
            }

            // 递增版本
            if (bdMissing || HasChanged(setting, so, result))
            {
                so.version = IncrementVersion(so.version, bdMissing ? ChangeLevel.Patch : result.changeLevel);
                EditorUtility.SetDirty(so);
            }

            WriteModuleBd(setting, so, result, setting.LatestAbs);
            Debug.Log($"[Upzy] 模块 {so.name} 构建完成，新版本号：{so.version}");
            WriteMenuBd(setting, AggregateBuiltinVersion(), setting.LatestAbs);
        }

        public static void BuildAll(UpzySetting setting)
        {
            EnsureLatestDir(setting);
            foreach (var entry in setting.registeredModules)
            {
                if (entry.configSO is UpzyModuleSO so)
                    BuildModule(setting, so);
            }

            WriteMenuBd(setting, AggregateBuiltinVersion(), setting.LatestAbs);
            Debug.Log("[Upzy] 已生成 latest/Menu.bd");
        }
        
        private static ConfigVersion AggregateBuiltinVersion()
        {
            var v = ReadBuiltinVersionXYZ();
            v.patch = 0; // latest 只是预构建，W 在发版时递增
            return v;
        }

        public static void Publish(UpzySetting setting, bool isFullBuild)
        {
            EnsureLatestDir(setting);

            // 检查 latest 下所有模块是否存在 .bd
            foreach (var entry in setting.registeredModules)
            {
                if (entry.configSO is UpzyModuleSO so)
                {
                    string bdPath = Path.Combine(setting.ModulesAbs(setting.LatestAbs), $"{so.name}.bd");
                    if (!File.Exists(bdPath))
                    {
                        Debug.LogError($"模块 {so.name} 缺少 .bd，请先构建再发版！");
                        return;
                    }
                }
            }

            var builtinVersion = ReadBuiltinVersionXYZ();
            var lastMenu = LoadLastMenu(setting.ReleaseAbs);

            bool needWrite = false;
            if (lastMenu == null ||
                lastMenu.meta.version.major != builtinVersion.major ||
                lastMenu.meta.version.minor != builtinVersion.minor ||
                lastMenu.meta.version.feature != builtinVersion.feature)
            {
                needWrite = true;
            }
            else
            {
                var currentModules = LoadAllModules(setting.LatestAbs);
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
                Debug.Log("[Upzy] 无模块变更，无需发版。");
                return;
            }

            // 1️⃣ 备份 release → rollback
            BackupRelease(setting);

            // 2️⃣ 覆盖 release ← latest
            if (Directory.Exists(setting.ReleaseAbs))
                Directory.Delete(setting.ReleaseAbs, true);
            FileUtil.CopyFileOrDirectory(setting.LatestAbs, setting.ReleaseAbs);

            // 3️⃣ 更新 Menu.bd（写入 release）
            var newVersion = builtinVersion;
            newVersion.patch = isFullBuild ? 1 : (lastMenu?.meta.version.patch ?? 0) + 1;
            WriteMenuBd(setting, newVersion, setting.ReleaseAbs);

            Debug.Log($"[Upzy] 发布成功，release 版本号：{newVersion}");
        }
        
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

        public static void RollbackRelease(UpzySetting setting)
        {
            if (!Directory.Exists(setting.RollbackAbs))
            {
                Debug.LogError("[Upzy] 没有可回滚的版本！");
                return;
            }

            if (Directory.Exists(setting.ReleaseAbs))
                Directory.Delete(setting.ReleaseAbs, true);

            FileUtil.CopyFileOrDirectory(setting.RollbackAbs, setting.ReleaseAbs);
            Debug.Log("[Upzy] 已回滚到上一个发版版本。");
        }

        // =============== 内部工具方法 ===============
        private static void EnsureLatestDir(UpzySetting setting)
        {
            if (!Directory.Exists(setting.LatestAbs))
                Directory.CreateDirectory(setting.LatestAbs);
            if (!Directory.Exists(setting.ModulesAbs(setting.LatestAbs)))
                Directory.CreateDirectory(setting.ModulesAbs(setting.LatestAbs));
        }

        private static void BackupRelease(UpzySetting setting)
        {
            if (!Directory.Exists(setting.ReleaseAbs)) return;

            if (Directory.Exists(setting.RollbackAbs))
                Directory.Delete(setting.RollbackAbs, true);

            FileUtil.CopyFileOrDirectory(setting.ReleaseAbs, setting.RollbackAbs);
            Debug.Log("[Upzy] 已备份 release 到 rollback 目录。");
        }

        public static void WriteModuleBd(UpzySetting setting, UpzyModuleSO so, BuildResult result, string rootDir)
        {
            string dir = setting.ModulesAbs(rootDir);
            Directory.CreateDirectory(dir);

            // 清理旧文件
            foreach (var file in Directory.GetFiles(dir, $"{so.name}*.bd"))
                File.Delete(file);

            string bdPath = Path.Combine(dir, $"{so.name}.bd");
            File.WriteAllText(bdPath, JsonUtility.ToJson(new ModuleBd
            {
                moduleName = so.name,
                version = so.version,
                aggregateHash = result.aggregateHash,
                entries = result.entries
            }, true));
        }

        private static ModuleBd[] LoadAllModules(string root)
        {
            string dir = Path.Combine(root, "Modules");
            if (!Directory.Exists(dir)) return new ModuleBd[0];

            return Directory.GetFiles(dir, "*.bd")
                .Select(f => JsonUtility.FromJson<ModuleBd>(File.ReadAllText(f)))
                .Where(m => m != null)
                .ToArray();
        }

        private static ConfigVersion ReadBuiltinVersionXYZ()
        {
            try
            {
                string builtinMenu = Path.Combine(Application.streamingAssetsPath, "Menu.bd");
                if (File.Exists(builtinMenu))
                {
                    var m = JsonUtility.FromJson<UpzyMenu>(File.ReadAllText(builtinMenu));
                    if (m != null) return m.meta.version;
                }
            }
            catch { }
            return new ConfigVersion(1, 0, 0, 0);
        }

        private static ConfigVersion IncrementVersion(ConfigVersion v, ChangeLevel level)
        {
            switch (level)
            {
                case ChangeLevel.Major: v.major++; v.minor = v.feature = v.patch = 0; break;
                case ChangeLevel.Minor: v.minor++; v.feature = v.patch = 0; break;
                case ChangeLevel.Feature: v.feature++; v.patch = 0; break;
                case ChangeLevel.Patch: v.patch++; break;
            }
            return v;
        }

        private static string GetModulesOutputRoot(UpzySetting setting)
        {
            string dir = setting.ModulesAbs(setting.LatestAbs);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static UpzyMenu LoadLastMenu(string root)
        {
            string path = Path.Combine(root, "Menu.bd");
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<UpzyMenu>(File.ReadAllText(path));
        }
        
        private static void WriteMenuBd(UpzySetting setting, ConfigVersion version, string rootDir)
        {
            string path = Path.Combine(rootDir, "Menu.bd");
            Directory.CreateDirectory(rootDir);

            var menu = new UpzyMenu
            {
                meta = new UpzyMenuMeta
                {
                    version = version,
                    generatedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                },
                modules = setting.registeredModules
                    .Where(e => e?.configSO != null)
                    .Select(e => new ModuleInfo
                    {
                        moduleName = e.configSO.name,
                        version = e.configSO.version
                    }).ToArray()
            };

            File.WriteAllText(path, JsonUtility.ToJson(menu, true));
        }
    }
}
#endif
