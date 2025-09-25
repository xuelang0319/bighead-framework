#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Configs
{
    /// <summary>
    /// 建议所有 SO 统一放到 Assets/Bighead/Configs/
    /// </summary>
    public class BuildSystemSetting : ScriptableObject
    {
        public const string AssetPath = "Assets/Bighead/Configs/BuildSystemSetting.asset";

        [Header("分组清单（可编辑）")]
        public List<string> groups = new List<string> { "正式" };

        [Header("发布分组（下拉选择）")]
        public int selectedGroupIndex = 0;

        [System.Flags]
        public enum BuildPlatforms
        {
            None = 0,
            Android = 1 << 0,
            iOS = 1 << 1,
            Windows = 1 << 2,
        }

        [Header("打包平台（多选）")]
        public BuildPlatforms buildPlatforms = BuildPlatforms.Android;

        [Header("输入路径模板（本地构建输出根）")]
        public string inputPathTemplate = "LocalBuild/{Group}/{AppVersion}/{Platform}/";

        [Header("输出路径模板（服务器上传根）")]
        public string outputPathTemplate = "Res/{Group}/{AppVersion}/{Platform}/";

        public static BuildSystemSetting GetOrCreateSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuildSystemSetting>(AssetPath);
            if (asset != null) return asset;

            var dir = Path.GetDirectoryName(AssetPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
                AssetDatabase.Refresh();
            }

            asset = CreateInstance<BuildSystemSetting>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif