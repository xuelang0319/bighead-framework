using UnityEngine;

namespace Bighead.Upzy.Core
{
    public abstract class UpzyBuildableBase
    {
        /// <summary>当前模块的配置实例</summary>
        public UpzyModuleSO ConfigSO { get; private set; }

        /// <summary>模块名，默认用 ConfigSO 名称</summary>
        public virtual string ModuleName => ConfigSO != null ? ConfigSO.name : GetType().Name;

        /// <summary>由框架调用，注入 ConfigSO</summary>
        public void SetConfig(UpzyModuleSO config)
        {
            ConfigSO = config;
        }

        /// <summary>模块具体的构建逻辑</summary>
        protected abstract BuildResult OnBuild(string outputRoot);

        /// <summary>外部调用的统一入口</summary>
        public BuildResult Build(string outputRoot)
        {
            if (ConfigSO == null)
            {
                Debug.LogWarning($"[{GetType().Name}] ConfigSO 未注入，使用默认配置执行构建。");
            }
            return OnBuild(outputRoot);
        }
    }
}