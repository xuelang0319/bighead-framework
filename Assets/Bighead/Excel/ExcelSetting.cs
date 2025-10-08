using System.IO;

namespace Bighead
{
    /// <summary>
    /// Excel 模块路径与文件名配置（完全依赖 BigheadSetting）。
    /// </summary>
    public static class ExcelSetting
    {
        // 模块文件夹名
        public const string ModuleFolder = "Excel";

        // 文件名常量
        public const string ExcelMetaCollectionFileName = "ExcelMetaCollection.asset";

        // Excel 模块根路径：Assets/Bighead/Generated/Excel
        public static string ExcelGeneratedDir =>
            BigheadSetting.ToGenerated(ModuleFolder);

        // ExcelMetaCollectionSO 存储路径
        public static string ExcelMetaCollectionPath =>
            Path.Combine(ExcelGeneratedDir, ExcelMetaCollectionFileName).Replace("\\", "/");

        // Excel 原始表格输入目录（比如放 .xlsx 源文件）
        // 注意：此路径不在 Bighead 内，由用户手动维护。
        // 可以根据项目习惯进行外部传入。
        public const string DefaultExcelSourceDir = "Assets/Excels";
    }
}