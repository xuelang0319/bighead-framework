#if UNITY_EDITOR
using framework_bighead.Csv;
using UnityEditor;

namespace framework_bighead.Config
{
    [InitializeOnLoad]
    public static class ConfigBootGenerator
    {
        static ConfigBootGenerator()
        {
            ConfigAutoCreator.CreateIfMissing<CsvConfig>("Assets/Bighead/Configs/CsvConfig.asset");
            // 后续可加入更多模块
        }
    }
}
#endif