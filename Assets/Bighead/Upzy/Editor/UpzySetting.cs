using System;
using Bighead.Core;
using Bighead.Core.Upzy;
using UnityEngine;

namespace Bighead.Upzy
{
    [CreateAssetMenu(fileName = "UpzySetting", menuName = "Bighead/Upzy Setting", order = 0)]
    public class UpzySetting : ScriptableObject
    {
        [Header("基础路径")]
        public string rootFolder = "Upzy";
        public string currentFolder = "current";
        public string backupFolder = "backup";
        public string stagingFolder = "staging";
        public string modulesFolder = "Modules";

        [Header("模块配置")]
        public ModuleConfig[] registeredModules;
    }
    
    [Serializable]
    public class ModuleConfig
    {
        public string moduleName;
        public ConfigVersion version;

        public string hash; // 模块整体哈希（所有文件拼接后计算）
        public ModuleFile[] files; // 模块文件列表，用于精确校验

        [Serializable]
        public class ModuleFile
        {
            public string fileName; // 相对路径
            public string hash; // 单文件哈希
            public long size; // 文件大小（字节）
        }
    }
}