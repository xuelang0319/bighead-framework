using System.IO;
using UnityEngine;

namespace Bighead.Core.Upzy
{
    public static class UpzyInitializer
    {
        private static readonly string UpzyRoot = Path.Combine(Application.persistentDataPath, "Upzy");
        private static readonly string CurrentDir = Path.Combine(UpzyRoot, "current");
        private static readonly string PreviousDir = Path.Combine(UpzyRoot, "previous");
        private static readonly string StagingDir = Path.Combine(UpzyRoot, "staging");

        public static void Initialize()
        {
            string builtinMenuPath = Path.Combine(Application.streamingAssetsPath, "FactoryMenu.bd");

            // 1. 首装/升级判断
            string localVersionPath =
                Path.Combine(UpzyRoot, "local_version.txt");
            string builtinVersion = ReadVersionFromMenu(builtinMenuPath);
            string localVersion = File.Exists(localVersionPath) ? File.ReadAllText(localVersionPath) : "";

            if (!Directory.Exists(CurrentDir) || CompareVersion(builtinVersion, localVersion) > 0)
            {
                // 整包升级或首次运行，清空 Upzy
                if (Directory.Exists(UpzyRoot))
                    Directory.Delete(UpzyRoot, true);

                Directory.CreateDirectory(CurrentDir);

                // 2. 灌入 FactoryMenu → Working Menu
                File.Copy(builtinMenuPath, Path.Combine(CurrentDir, "Menu.bd"));

                // 3. 记录当前版本号
                File.WriteAllText(localVersionPath, builtinVersion);

                Debug.Log($"Upzy 初始化完成：版本 {builtinVersion}");
            }
            else
            {
                Debug.Log($"Upzy 保留：版本 {localVersion}");
            }
        }

        private static string ReadVersionFromMenu(string menuPath)
        {
            // 简化处理，真实实现应解密 + 解析 JSON
            string json = File.ReadAllText(menuPath);
            var menu = JsonUtility.FromJson<WorkingMenu>(json);
            return menu.meta.version;
        }

        private static int CompareVersion(string v1, string v2)
        {
            var p1 = v1.Split('.');
            var p2 = v2.Split('.');
            for (int i = 0; i < Mathf.Max(p1.Length, p2.Length); i++)
            {
                int a = i < p1.Length ? int.Parse(p1[i]) : 0;
                int b = i < p2.Length ? int.Parse(p2[i]) : 0;
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }
    }

    [System.Serializable]
    public class WorkingMenu
    {
        public Meta meta;
        public ModuleEntry[] modules;
    }

    [System.Serializable]
    public class Meta
    {
        public string version;
        public string generatedAt;
    }

    [System.Serializable]
    public class ModuleEntry
    {
        public string name;
        public string config;
    }
}