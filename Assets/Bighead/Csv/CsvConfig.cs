using System.IO;
using UnityEngine;

namespace framework_bighead.Csv
{
    [CreateAssetMenu(fileName = "CsvConfig", menuName = "Bighead/Configs/CsvConfig")]
    public class CsvConfig : ScriptableObject
    {
        /// <summary> Assets目录外文件夹路径，不希望生成Meta文件的存储位置 </summary>
        private static string NONE_META_FOLDER_PATH = Path.Combine(Directory.GetParent(Application.dataPath).FullName.Replace("/", "\\"), "Bighead");
        
        /// <summary> 表格目录 </summary>
        public static string NONE_META_TABLE_PATH = Path.Combine(NONE_META_FOLDER_PATH, "Table");
        
        /// <summary> 表格目录/Excel目录 </summary>
        public string TABLE_EXCEL_PATH = Path.Combine(NONE_META_TABLE_PATH, "Excel");
        /// <summary> 表格目录/Csv目录 </summary>
        public string TABLE_CSV_PATH = Path.Combine(NONE_META_TABLE_PATH, "Csv");

        /// <summary> 生成目录 </summary>
        public static string ASSETS_GENERATE_FOLDER_PATH = Path.Combine(Application.dataPath.Replace("/", "\\"), "GenerateCsv");
        /// <summary> 生成目录/Csv </summary>
        public string GENERATE_CSV_BYTES_PATH = Path.Combine(ASSETS_GENERATE_FOLDER_PATH, "Csv");
        /// <summary> 生成目录/GenerateCs </summary>
        public string GENERATE_CS_PATH = Path.Combine(ASSETS_GENERATE_FOLDER_PATH, "Scripts");
        

        /// <summary> Assets目录内文件夹路径，包括生成脚本、配置等。 </summary>
        public static readonly string ASSETS_FOLDER_PATH = Path.Combine(Application.dataPath.Replace("/", "\\"), "Bighead");
    }
}