#if UNITY_EDITOR
using Bighead.Core.Utility;
using Bighead.Csv;
using UnityEditor;

namespace Bighead.Core.Config
{
    [InitializeOnLoad]
    public static class ConfigBootGenerator
    {
        static ConfigBootGenerator()
        {
            ConfigAutoCreator.CreateIfMissing<CsvConfig>("Assets/Bighead/Configs/CsvConfig.asset",config =>
            {
                DirectoryHelper.ForceCreateDirectory(config.TABLE_EXCEL_PATH);
            });
            
            // 后续可加入更多模块
        }
    }
}
#endif