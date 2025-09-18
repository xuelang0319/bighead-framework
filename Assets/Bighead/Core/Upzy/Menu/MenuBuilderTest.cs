using UnityEditor;

namespace Bighead.Core.Upzy
{
    public class MenuBuildExample
    {
#if UNITY_EDITOR
        [MenuItem("Upzy/Build Menu")]
        public static void Build()
        {
            // 读取 UpzyConfig 资源
            var config = AssetDatabase.LoadAssetAtPath<UpzyConfig>("Assets/Configs/UpzyConfig.asset");

            // 模拟生成几个模块
            var modules = new[]
            {
                new ModuleConfig { moduleName = "A", version = new ConfigVersion(1,0,0,1) },
                new ModuleConfig { moduleName = "B", version = new ConfigVersion(1,0,0,0) }
            };

            MenuBuilder.BuildMenu(config, modules, new ConfigVersion(1, 0, 0, 0));
        }
#endif
    }
}