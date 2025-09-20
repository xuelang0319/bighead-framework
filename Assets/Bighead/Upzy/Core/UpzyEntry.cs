using UnityEngine;

namespace Bighead.Upzy.Core
{
    [System.Serializable]
    public class UpzyEntry
    {
        [Tooltip("模块配置 ScriptableObject，由框架自动创建，也可手动替换")]
        public ScriptableObject configSO;
    }
}