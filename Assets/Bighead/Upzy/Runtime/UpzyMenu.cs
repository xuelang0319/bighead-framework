using System;
using Bighead.Upzy.Core;

namespace Bighead.Upzy.Runtime
{
    [Serializable]
    public class UpzyMenu
    {
        public UpzyMenuMeta meta;
        public ModuleInfo[] modules;
    }

    [Serializable]
    public class UpzyMenuMeta
    {
        public ConfigVersion version;
        public string generatedAt;
    }

    [Serializable]
    public class ModuleInfo
    {
        public string moduleName;
        public ConfigVersion version;
    }
}