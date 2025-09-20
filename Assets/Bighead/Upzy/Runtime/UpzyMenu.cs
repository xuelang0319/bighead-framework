using Bighead.Upzy.Core;

namespace Bighead.Upzy.Runtime
{
    [System.Serializable]
    public class UpzyMenu
    {
        [System.Serializable]
        public class Meta
        {
            public ConfigVersion version;  // 当前完整版本号
            public string generatedAt;     // 生成时间 (ISO8601)
        }

        [System.Serializable]
        public class ModuleInfo
        {
            public string moduleName;       // 模块名
            public ConfigVersion version;   // 模块版本号
            public string moduleBdRelPath;  // 模块 .bd 的相对路径
            public string aggregateHash;    // 模块总 hash
        }

        public Meta meta;
        public ModuleInfo[] modules;
    }
}