using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Bighead.BuildSystem.Editor
{
    public enum BuildMode
    {
        FullBuild,
        Incremental
    }

    [System.Serializable]
    public class SyncServerConfig
    {
        [Tooltip("是否开启同步服务器模式")]
        public bool Enable = false;

        [Header("服务器路径配置")]
        [Tooltip("客户端加载资源时使用的下载路径，例如 http://cdn.xxx.com/Build/{Platform}")]
        public string DownloadPath = "http://127.0.0.1/Build/{Platform}";

        [Tooltip("上传工具使用的目标服务器路径，例如 scp 目标目录")]
        public string UploadPath = "/var/www/cdn/Build/{Platform}";

        [Header("认证/附加参数")]
        public string AuthToken = "";
        public string ExtraArgs = "";
    }

    public class BuildSystemSetting : ScriptableObject
    {
        [Header("构建模式")]
        public BuildMode Mode = BuildMode.FullBuild;

        [Header("平台选择")]
        public List<BuildTarget> SelectedPlatforms = new List<BuildTarget>();

        [Header("Addressables 路径")]
        [Tooltip("Addressables BuildPath: 本地打包输出路径，支持 {Platform} 和 {Version}")]
        public string BuildPath = "BuildOutput/{Platform}/{Version}";

        [Tooltip("Addressables LoadPath: 本地加载路径，支持 {Platform} 和 {Version}")]
        public string LocalLoadPath = "BuildOutput/{Platform}/{Version}";

        [Header("同步服务器配置")]
        public SyncServerConfig SyncConfig = new SyncServerConfig();

        /// <summary>
        /// 返回用于 Addressables 配置的 BuildPath 和 LoadPath
        /// </summary>
        public (string buildPath, string loadPath) GetAddressablePaths()
        {
            if (SyncConfig.Enable)
            {
                return (BuildPath, SyncConfig.DownloadPath);
            }
            else
            {
                return (BuildPath, LocalLoadPath);
            }
        }

        public static BuildSystemSetting GetOrCreateSettings()
        {
            const string kAssetPath = "Assets/Bighead/BuildSystem/Editor/BuildSystemSetting.asset";

            var setting = AssetDatabase.LoadAssetAtPath<BuildSystemSetting>(kAssetPath);
            if (setting == null)
            {
                setting = CreateInstance<BuildSystemSetting>();
                var dir = System.IO.Path.GetDirectoryName(kAssetPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(setting, kAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return setting;
        }
    }
}
