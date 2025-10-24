#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Excel;
using UnityEditor;
using UnityEngine;

namespace Bighead.ExcelBuild
{
    public static class ExcelPipelineMethods
    {
        
        // ---------------------------------
        // 存储与加载
        // ---------------------------------
        public static void SaveExcelMetaCollectionSO(ExcelMetaCollectionSO collection, string savePath = null)
        {
            if (collection == null) return;
            savePath ??= ExcelSetting.ExcelMetaCollectionPath; // 默认路径

            var dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var existing = AssetDatabase.LoadAssetAtPath<ExcelMetaCollectionSO>(savePath);
            if (existing != null)
                AssetDatabase.DeleteAsset(savePath);

            AssetDatabase.CreateAsset(collection, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static ExcelMetaCollectionSO LoadExcelMetaCollectionSO(string loadPath = null)
        {
            loadPath ??= ExcelSetting.ExcelMetaCollectionPath;
            return AssetDatabase.LoadAssetAtPath<ExcelMetaCollectionSO>(loadPath);
        }
        
        /// <summary>
        /// 从文件路径读取全部字节，异常安全版本。
        /// </summary>
        public static byte[] ReadFileBytes(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Debug.LogWarning($"[ExcelPipeline] File not found: {filePath}");
                    return Array.Empty<byte>();
                }
                return File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExcelPipeline] ReadFileBytes failed: {filePath}\n{e}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// 计算任意字节数组的 MD5 值（返回 32 位小写十六进制字符串）。
        /// </summary>
        public static string ComputeMD5(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// 将 string[][] 数据转为 UTF8 字节序列（用于生成稳定 MD5）。
        /// </summary>
        public static byte[] ToUtf8Bytes(string[][] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            // 每行按制表符拼接，保持结构稳定
            var sb = new StringBuilder();
            foreach (var row in data)
            {
                if (row == null) continue;
                sb.AppendLine(string.Join("\t", row));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// 从 Excel 文件计算其整体 MD5（基于文件字节）。
        /// </summary>
        public static string ComputeExcelMD5FromFile(string filePath)
        {
            var bytes = ReadFileBytes(filePath);
            return ComputeMD5(bytes);
        }

        /// <summary>
        /// 从二维数据计算 MD5（基于内容）。
        /// </summary>
        public static string ComputeSheetMD5(string[][] data)
        {
            var bytes = ToUtf8Bytes(data);
            return ComputeMD5(bytes);
        }

        /// <summary>
        /// 从二维表数据中提取列信息。
        /// 
        /// ⚙️ 约定格式：
        /// 0 - name（字段名，纯英文）
        /// 1 - type（字段类型）
        /// 2 - desc（字段描述，可为任意语言）
        /// </summary>
        /// <param name="data">从 Excel 读取的二维数组</param>
        /// <returns>每一列的列名与类型组成的列表</returns>
        public static List<ColumnKey> ExtractKeys(string[][] data)
        {
            var result = new List<ColumnKey>();

            // 安全检查：至少要有两行（name/type）
            if (data == null || data.Length < 2)
            {
                Debug.LogWarning("[ExcelPipeline] Data rows insufficient for ExtractKeys()");
                return result;
            }

            // 第0行为列名，第1行为类型
            var names = data[0];
            var types = data[1];

            // 取两者中较短的长度，防止越界
            var length = Mathf.Min(names.Length, types.Length);

            for (int i = 0; i < length; i++)
            {
                var key = new ColumnKey
                {
                    Name = names[i]?.Trim() ?? string.Empty,
                    Type = types[i]?.Trim() ?? "string"
                };
                result.Add(key);
            }

            return result;
        }
        
        /// <summary>
        /// 构建单个表单的元数据（SheetMeta）。
        /// 
        /// ⚙️ 数据结构说明：
        /// 0 - name（字段名）
        /// 1 - type（字段类型）
        /// 2 - desc（字段描述）
        /// 3+ - data（表格内容）
        /// 
        /// 🧩 说明：
        /// - 通过 ExtractKeys() 获取列定义；
        /// - 从第3行（索引3）开始收集数据；
        /// - 自动计算表单级 MD5；
        /// </summary>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="data">表格内容矩阵</param>
        /// <returns>构建完成的 SheetMeta 对象</returns>
        public static SheetMeta BuildSheetMeta(string sheetName, string[][] data)
        {
            if (data == null || data.Length == 0)
                return null;

            // 创建 SheetMeta 实例
            var meta = new SheetMeta
            {
                SheetName = sheetName,
                // 计算整张表的 MD5（包含 desc 行）
                MD5 = ComputeSheetMD5(data),
                // 提取列定义信息
                Keys = ExtractKeys(data)
            };

            // 数据从第3行（索引3）开始
            if (data.Length > 3)
            {
                var contentRows = new List<string[]>();

                for (int i = 3; i < data.Length; i++)
                {
                    var row = data[i];

                    // 可选：过滤空行（所有单元格为空）
                    bool allEmpty = true;
                    for (int c = 0; c < row.Length; c++)
                    {
                        if (!string.IsNullOrEmpty(row[c]))
                        {
                            allEmpty = false;
                            break;
                        }
                    }

                    if (!allEmpty)
                        contentRows.Add(row);
                }

                meta.Data = contentRows.ToArray();
            }
            else
            {
                meta.Data = Array.Empty<string[]>();
            }

            return meta;
        }
        
        /// <summary>
        /// 将 DataTable 转换为 string[][]，用于生成 SheetMeta。
        /// 
        /// ⚙️ 特性：
        /// - 自动去除末尾空行和空列；
        /// - 保留中间空单元格；
        /// - 兼容 desc 行（三行表头结构）；
        /// </summary>
        /// <param name="sheet">ExcelDataReader 生成的 DataTable</param>
        /// <returns>二维字符串矩阵</returns>
        public static string[][] ReadSheetToMatrix(DataTable sheet)
        {
            if (sheet == null)
            {
                Debug.LogWarning("[ExcelPipeline] ReadSheetToMatrix: sheet is null");
                return Array.Empty<string[]>();
            }

            var rows = new List<string[]>();

            int maxColumnCount = 0;
            int validRowCount = 0;

            // 1️⃣ 先收集所有行，计算最大列数
            foreach (DataRow row in sheet.Rows)
            {
                int currentCount = row.ItemArray.Length;
                if (currentCount > maxColumnCount)
                    maxColumnCount = currentCount;
            }

            // 2️⃣ 构建完整矩阵并检测有效数据行（防止尾部空行）
            foreach (DataRow row in sheet.Rows)
            {
                var values = new string[maxColumnCount];
                bool isAllEmpty = true;

                for (int i = 0; i < maxColumnCount; i++)
                {
                    var cell = row.ItemArray.Length > i ? row.ItemArray[i] : null;
                    var text = cell?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                        isAllEmpty = false;
                    values[i] = text;
                }

                if (!isAllEmpty)
                {
                    rows.Add(values);
                    validRowCount++;
                }
            }

            // 3️⃣ 去除末尾全空列（Excel中经常会多余几列）
            int lastValidCol = 0;
            for (int c = 0; c < maxColumnCount; c++)
            {
                bool hasValue = false;
                foreach (var row in rows)
                {
                    if (row.Length > c && !string.IsNullOrEmpty(row[c]))
                    {
                        hasValue = true;
                        break;
                    }
                }
                if (hasValue)
                    lastValidCol = c;
            }

            // 4️⃣ 重新构造裁剪后的矩阵
            var finalMatrix = new string[validRowCount][];
            for (int r = 0; r < validRowCount; r++)
            {
                var srcRow = rows[r];
                var destRow = new string[lastValidCol + 1];
                Array.Copy(srcRow, destRow, lastValidCol + 1);
                finalMatrix[r] = destRow;
            }

            return finalMatrix;
        }
        
        /// <summary>
        /// 从指定 Excel 文件读取所有工作表(DataTable)。
        /// 支持 .xls / .xlsx；不依赖 ExcelDataReader.DataSet 扩展包。
        /// </summary>
        public static List<DataTable> LoadAllSheetsFromExcel(string excelPath)
        {
            var result = new List<DataTable>();

            try
            {
                if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
                {
                    Debug.LogWarning($"[ExcelPipeline] Excel file not found: {excelPath}");
                    return result;
                }

                // 以只读+共享方式打开，避免被 Excel 占用
                FileStream stream = null;
                IExcelDataReader reader = null;

                try
                {
                    stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    var ext = Path.GetExtension(excelPath).ToLowerInvariant();
                    if (ext == ".xls")
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    else if (ext == ".xlsx")
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    else
                    {
                        Debug.LogWarning($"[ExcelPipeline] Unsupported Excel format: {ext}");
                        return result;
                    }

                    // 直接调用 AsDataSet()（无需配置对象）
                    var dataSet = reader.AsDataSet();
                    if (dataSet?.Tables == null || dataSet.Tables.Count == 0)
                        return result;

                    foreach (DataTable table in dataSet.Tables)
                    {
                        if (table is { Rows: { Count: > 0 } })
                            result.Add(table);
                    }
                }
                finally
                {
                    // 按老式 using 等价方式显式释放，兼容较低 C# 版本
                    reader?.Close();
                    stream?.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExcelPipeline] LoadAllSheetsFromExcel failed: {excelPath}\n{e}");
            }

            return result;
        }
        
         /// <summary>
        /// 递归扫描目录下的所有 Excel 文件。
        /// 支持 .xls 与 .xlsx。
        /// </summary>
        public static List<string> FindAllExcelFiles(string directoryPath)
        {
            var results = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    Debug.LogWarning("[ExcelPipeline] Directory path is null or empty.");
                    return results;
                }

                if (!Directory.Exists(directoryPath))
                {
                    Debug.LogWarning($"[ExcelPipeline] Directory not found: {directoryPath}");
                    return results;
                }

                // 搜索所有子目录的 .xls / .xlsx 文件
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext == ".xls" || ext == ".xlsx")
                        results.Add(file.Replace("\\", "/"));
                }

                // 稳定排序
                results.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExcelPipeline] FindAllExcelFiles failed: {e.Message}");
            }

            return results;
        }

        /// <summary>
        /// 从 Excel 文件路径中获取文件名（不含扩展名）。
        /// 例如： "C:/Data/Excel/Item.xlsx" → "Item"
        /// </summary>
        public static string GetExcelName(string excelPath)
        {
            if (string.IsNullOrEmpty(excelPath))
                return string.Empty;

            try
            {
                return Path.GetFileNameWithoutExtension(excelPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ExcelPipeline] GetExcelName failed: {excelPath}\n{e}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 构建单个 ExcelMeta 对象。
        /// </summary>
        /// <param name="excelName">Excel 名称（不含扩展名）</param>
        /// <param name="filePath">Excel 文件路径</param>
        /// <param name="sheets">表单集合</param>
        /// <returns>ExcelMeta 实例</returns>
        public static ExcelMeta BuildExcelMeta(string excelName, string filePath, List<SheetMeta> sheets)
        {
            if (string.IsNullOrEmpty(excelName))
                excelName = GetExcelName(filePath);

            return new ExcelMeta
            {
                ExcelName = excelName,
                FilePath = filePath?.Replace("\\", "/") ?? string.Empty,
                Sheets = sheets ?? new List<SheetMeta>(),
                MD5 = string.Empty // 由外层 BuildOrReuseExcel 赋值
            };
        }
        
        /// <summary>
        /// 计算整个 Excel 集合的全局 MD5。
        /// </summary>
        public static string ComputeGlobalMD5(IEnumerable<ExcelMeta> excels)
        {
            if (excels == null)
                return string.Empty;

            // 稳定排序（防止插入顺序影响）
            var ordered = excels
                .OrderBy(e => e.ExcelName, System.StringComparer.Ordinal)
                .ThenBy(e => e.FilePath, System.StringComparer.Ordinal)
                .ToList();

            // 拼接所有 Excel 的 MD5 信息
            var builder = new StringBuilder();
            foreach (var e in ordered)
            {
                builder.Append(e.ExcelName)
                    .Append(e.FilePath)
                    .Append(e.MD5);
            }

            // 计算整体 MD5
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hash = md5.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
#endif
