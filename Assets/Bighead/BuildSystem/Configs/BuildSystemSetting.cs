#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bighead.BuildSystem.Editor
{
    [CreateAssetMenu(fileName = "BuildSystemSetting", menuName = "Bighead/BuildSystem/BuildSystemSetting", order = 0)]
    public class BuildSystemSetting : ScriptableObject
    {
        [Header("根路径（不含平台和版本号）")]
        public string BuildPath = "BuildOutput";

        [Header("加载路径（本地）")]
        public string LocalLoadPath = "BuildOutput";

        [Header("构建模式")]
        public BuildMode Mode = BuildMode.FullBuild;

        [Header("目标平台")] 
        public List<BuildPlatformSetting> BuildPlatformSettings = new List<BuildPlatformSetting>();

        public static BuildSystemSetting GetOrCreateSettings()
        {
            const string assetPath = "Assets/Bighead/BuildSystem/Configs/BuildSystemSetting.asset";
            var setting = AssetDatabase.LoadAssetAtPath<BuildSystemSetting>(assetPath);

            if (setting == null)
            {
                setting = CreateInstance<BuildSystemSetting>();
                AssetDatabase.CreateAsset(setting, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[BuildSystemSetting] 新建配置文件：" + assetPath);
            }
            return setting;
        }
    }

    [System.Serializable]
    public class BuildPlatformSetting
    {
        [Header("打包平台")] public BuildTarget Platform = BuildTarget.NoTarget;
        [Header("上传服务器")] public bool Upload2Server = false;
        [Header("服务器地址")]public string ServerUrl;
        [Header("服务器秘钥")]public string Secret;
    }

    public enum BuildMode
    {
        FullBuild,
        Incremental
    }
}
#endif