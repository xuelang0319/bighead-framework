using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bighead.BuildSystem
{
    /// <summary>
    /// BuildSystem 模块的打包配置（唯一真源）
    /// 持久化存储在 Bighead/Configs/BuildSystem/AssetPackSO.asset
    /// </summary>
    [Serializable]
    public class AssetPackSO : ScriptableObject
    {
        [Header("压缩方式")]
        public CompressionType Compression = CompressionType.LZ4;

        [Header("全局标签列表（集中管理，可为空）")]
        public List<string> Labels = new List<string>();

        [Header("打包条目列表")]
        public List<AssetPackEntry> Entries = new List<AssetPackEntry>();
        
        [Header("部署配置")]
        public DeployConfig Deploy = new DeployConfig();
    }

    [Serializable]
    public class AssetPackEntry
    {
        [Tooltip("资源文件夹或文件路径（相对于 Assets）")]
        public string Path;

        [Tooltip("选择的标签列表（直接存字符串，可为空）")]
        public List<string> SelectedLabels = new List<string>();
    }

    public enum CompressionType
    {
        LZ4,
        Uncompressed
    }
    
    [System.Serializable]
    public class DeployConfig
    {
        public string ServerAddress = "";
        public int Port = 8081;
        [HideInInspector] public bool SyncUpload; // 只做编辑器显示用
    }

}