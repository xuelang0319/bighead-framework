#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Bighead.Excel
{
    public static class ExcelMetaController
    {
        /// <summary>
        /// 扫描 ExcelSetting.DefaultExcelSourceDir 下所有 Excel 文件，
        /// 并生成 ExcelMetaCollectionSO 存储于 ExcelSetting.ExcelMetaCollectionPath。
        /// 这里优先用 FilePath 做增量匹配，回退到 ExcelName。
        /// </summary>
        public static ExcelMetaCollectionSO BuildExcelMetaCollection(IEnumerable<string> directories)
        {
            // 1) 读取旧集合（可能为 null）
            var loadPath = ExcelSetting.ExcelMetaCollectionPath;
            var oldCollection = ExcelPipelineMethods.LoadExcelMetaCollectionSO(loadPath);

            // 2) 扫描所有 Excel 源文件
            var excelFiles = new HashSet<string>();
            foreach (var directory in directories)
            {
                var directoryExcelFiles = ExcelPipelineMethods.FindAllExcelFiles(directory);
                foreach (var excelFile in directoryExcelFiles)
                {
                    excelFiles.Add(excelFile);
                }
            }

            if (excelFiles.Count == 0)
            {
                Debug.LogWarning($"[ExcelMeta] No Excel files found in: {string.Join("、", directories)}");
                return oldCollection;
            }

            // 3) 新集合
            var newCollection = ScriptableObject.CreateInstance<ExcelMetaCollectionSO>();
            newCollection.Excels = new List<ExcelMeta>();

            // 4) 逐个构建，优先用 FilePath 匹配旧项
            foreach (var excelPath in excelFiles)
            {
                var excelName = ExcelPipelineMethods.GetExcelName(excelPath);
                var oldExcel = FindOldExcelMeta(oldCollection, excelPath, excelName);
                var newExcel = BuildOrReuseExcel(excelPath, oldExcel);
                newCollection.Excels.Add(newExcel);
            }

            newCollection.GlobalMD5 = ExcelPipelineMethods.ComputeGlobalMD5(newCollection.Excels);

            // 5) 保存
            var savePath = ExcelSetting.ExcelMetaCollectionPath;
            ExcelPipelineMethods.SaveExcelMetaCollectionSO(newCollection, savePath);
            
            Debug.Log($"[ExcelMeta] ExcelMetaCollectionSO build success: {savePath}");
            return newCollection;
        }

        /// <summary>
        /// 单个 Excel 的增量构建。
        /// 这里真正“应用”了 ExcelMeta.FilePath：
        /// - sourcePath 优先用传入的 excelPath（新扫描结果）
        /// - 若为空/无效，则回退到 oldExcelMeta.FilePath
        /// </summary>
        public static ExcelMeta BuildOrReuseExcel(string excelPath, ExcelMeta oldExcelMeta)
        {
            // 1) 统一决定本次构建使用的来源路径
            var sourcePath = !string.IsNullOrEmpty(excelPath) ? excelPath : oldExcelMeta?.FilePath;

            // 安全兜底：如果两者都拿不到，直接返回旧数据（避免崩溃）
            if (string.IsNullOrEmpty(sourcePath))
                return oldExcelMeta;

            // 2) 用 sourcePath 计算 Excel 级 MD5（判断是否整体跳过）
            var newExcelMD5 = ExcelPipelineMethods.ComputeExcelMD5FromFile(sourcePath);
            if (oldExcelMeta != null && oldExcelMeta.MD5 == newExcelMD5)
                return oldExcelMeta;

            // 3) 读取所有 Sheet，并逐 Sheet 做增量
            var dataTables = ExcelPipelineMethods.LoadAllSheetsFromExcel(sourcePath);
            var newSheets = new List<SheetMeta>();

            foreach (var table in dataTables)
            {
                var sheetName = table.TableName;
                var data = ExcelPipelineMethods.ReadSheetToMatrix(table);
                var newSheetMD5 = ExcelPipelineMethods.ComputeSheetMD5(data: data);

                var oldSheet = oldExcelMeta?.Sheets?.Find(s => s.SheetName == sheetName);
                if (oldSheet != null && oldSheet.MD5 == newSheetMD5)
                {
                    newSheets.Add(oldSheet);
                    continue;
                }

                var newSheet = ExcelPipelineMethods.BuildSheetMeta(sheetName, data);
                newSheets.Add(newSheet);
            }

            // 4) 组装新的 ExcelMeta（这里会把 FilePath 写入）
            var newExcelMeta = ExcelPipelineMethods.BuildExcelMeta(
                excelName: ExcelPipelineMethods.GetExcelName(sourcePath),
                filePath: sourcePath,
                sheets: newSheets
            );
            newExcelMeta.MD5 = newExcelMD5;

            return newExcelMeta;
        }

        /// <summary>
        /// 优先按 FilePath 匹配旧的 ExcelMeta；找不到再按 ExcelName 回退。
        /// 让 Excel 被移动/改名时也能稳定复用旧数据。
        /// </summary>
        private static ExcelMeta FindOldExcelMeta(ExcelMetaCollectionSO oldCollection, string excelPath,
            string excelName)
        {
            if (oldCollection?.Excels == null || oldCollection.Excels.Count == 0)
                return null;

            // 1) 先按路径匹配（应用 FilePath）
            var byPath = oldCollection.Excels.Find(e =>
                !string.IsNullOrEmpty(e.FilePath) &&
                string.Equals(e.FilePath, excelPath, System.StringComparison.OrdinalIgnoreCase));
            if (byPath != null) return byPath;

            // 2) 再按名称匹配（回退策略）
            var byName = oldCollection.Excels.Find(e =>
                string.Equals(e.ExcelName, excelName, System.StringComparison.Ordinal));
            return byName;
        }
    }
}
#endif