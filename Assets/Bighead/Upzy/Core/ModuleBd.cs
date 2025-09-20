using System.Collections.Generic;

namespace Bighead.Upzy.Core
{
    [System.Serializable]
    public class ModuleBd
    {
        public string moduleName;        // 模块名
        public ConfigVersion version;    // 模块版本号
        public string aggregateHash;     // 模块总 hash
        public List<BuildEntry> entries; // 模块内所有文件
    }
}