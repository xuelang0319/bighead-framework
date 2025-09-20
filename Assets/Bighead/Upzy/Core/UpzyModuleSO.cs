using Bighead.Core;
using UnityEngine;

namespace Bighead.Upzy.Core
{
    public abstract class UpzyModuleSO : ScriptableObject
    {
        [Header("模块版本号（由框架更新，也可手动调整）")]
        public ConfigVersion version = new ConfigVersion(1, 0, 0, 0);
    }
}