#if UNITY_EDITOR
//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月23日   |   Excel生成Csv文件
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bighead.Core.Utility;
using Excel;
using UnityEditor;

namespace Bighead.Csv
{
    public static partial class Excel2Csv
    {
        [MenuItem("Bighead/Csv/Generate", false, 10)]
        public static void Generate()
        {
            AnalysisExcel();
            AssetDatabase.Refresh();
            GC.Collect();
            EditorUtility.DisplayDialog("Generate Csv Success",
                "" +
                "      ＼　 　／    \r\n" +
                "┏━━━━━＼／━━━━━━┓\r\n" +
                "┃┏━━━━━━━━━━━━┓ ┃\r\n" +
                "┃┃          ┃ ┃\r\n" +
                "┃┃          ┃ ┃\r\n" +
                "┃┗━━━━━━━━━━━━┛ ┃\r\n" +
                "┗━━━∪━━━━━━∪━━━┛\r\n", "OK");
        }

        [MenuItem("Bighead/Csv/Clear")]
        public static void ClearAll()
        {
            DeleteCsv();
            DeleteConfig();
            AssetDatabase.Refresh();
        }

        private static void DeleteCsv()
        {
            DirectoryHelper.ClearDirectory(CsvEditorAccess.Config.TABLE_CSV_PATH);
        }

        /// <summary>
        /// MD5校验文件路径
        /// </summary>
        private static string CSV_CONFIG_PATH = Path.Combine(CsvConfig.NONE_META_TABLE_PATH, "CsvConfig.bh");

        private static void DeleteConfig()
        {
            FileHelper.DeleteUnityFile(CSV_CONFIG_PATH);
        }

        private static void AnalysisExcel()
        {
            DirectoryHelper.ForceCreateDirectory(CsvConfig.NONE_META_TABLE_PATH);
            DirectoryHelper.ForceCreateDirectory(CsvEditorAccess.Config.TABLE_CSV_PATH);
            DirectoryHelper.ForceCreateDirectory(CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH);

            // 上一次生成存储的MD5数据
            var oldData = new Dictionary<string, string>();
            // 此次生成存储的MD5数据
            var newData = new Dictionary<string, string>();
            // 读取生成存储信息
            if (File.Exists(CSV_CONFIG_PATH))
            {
                using var stream = new StreamReader(CSV_CONFIG_PATH);
                while (stream.ReadLine() is { } line)
                {
                    var array = line.Split('@');
                    // 0 - 文件名或全路径， 1 - 校验码
                    oldData.Add(array[0], array[1]);
                }
            }

            // 获取Excel文件夹下所有文件路径
            var files = Directory.GetFiles(CsvEditorAccess.Config.TABLE_EXCEL_PATH, "*", SearchOption.AllDirectories);
            var paths = files
                .Where(name => name.EndsWith(".xlsx") || name.EndsWith(".xls"))
                .Where(name => !name.Contains("~$"))
#if UNITY_EDITOR_WIN
                .Select(name => name.Replace('/', '\\'))
#endif
                .ToArray();

            // 发生变化的列表
            var changedFilter = new List<string>();

            // 对每个Excel做MD5变更校验
            foreach (var path in paths)
            {
                var fileNameWithExtension = Path.GetFileName(path);
                if (fileNameWithExtension.StartsWith("#")) continue;

                var fileContent = FileHelper.ShareReadFile(path);
                var fileMd5 = BigheadCrypto.MD5Encode(fileContent);

                // 添加正确数据，直接修正，但对发生了变化或新增的文件进行记录
                newData.Add(fileNameWithExtension, fileMd5);

                // 老数据是否包含文件名称并且MD5没有变化
                if (oldData.ContainsKey(fileNameWithExtension) && Equals(oldData[fileNameWithExtension], fileMd5))
                {
                    // 说明数据没有变化
                    var excelName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
                    var all = oldData.Keys.Where(recordData =>
                    {
                        // recordData：
                        // 1、ExcelName.xlsx @ MD5
                        // 2、ExcelName $ SheetName @ MD5
                        if (!recordData.StartsWith(excelName)) return false;
                        // 这里校验recordData包含的Excel名称信息是否和excelName相等
                        return !string.IsNullOrEmpty(Path.GetExtension(recordData)) ||
                               Equals(excelName.Split('$')[0], excelName);
                    }).ToList();

                    foreach (var key in all)
                    {
                        newData.AddValue(key, oldData[key]);
                        oldData.Remove(key);
                    }

                    continue;
                }

                // 发生了变化
                changedFilter.Add(path);

                // 由于即将进行处理，所以在这里移除。
                if (oldData.ContainsKey(fileNameWithExtension))
                    oldData.Remove(fileNameWithExtension);
            }

            // 正则比对名称，如果有括号则选用括号内的名称
            var regex1 = new Regex(@"\((\w+)\)");
            var regex2 = new Regex(@"\（(\w+)\）");

            // 遍历发生变更或新建的Excel
            foreach (var path in changedFilter)
            {
                var excelName = Path.GetFileNameWithoutExtension(path);
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var excelReader = path.EndsWith(".xls")
                               ? ExcelReaderFactory.CreateBinaryReader(stream)
                               : ExcelReaderFactory.CreateOpenXmlReader(stream))
                    {
                        var dataSet = excelReader.AsDataSet();

                        float createProgress = 0;
                        var tableCount = dataSet.Tables.Count;
                        foreach (DataTable table in dataSet.Tables)
                        {
                            ++createProgress;
                            // 过滤不生成的表单
                            var sheetName = table.TableName.Trim();
                            if (sheetName.StartsWith("#"))
                                continue;

                            // 正则后的表单名称
                            if (regex1.IsMatch(sheetName))
                                sheetName = regex1.Match(sheetName).Value.TrimStart('(').TrimEnd(')');

                            if (regex2.IsMatch(sheetName))
                                sheetName = regex2.Match(sheetName).Value.TrimStart('（').TrimEnd('）');

                            EditorUtility.DisplayProgressBar($"正在解析：{excelName}", sheetName,
                                createProgress / tableCount);

                            var rows = table.Rows;
                            var cols = table.Columns;

                            var tableData = AnalysisTableData(rows, cols);
                            var tableString = Convert2String(tableData);

                            // 这里进行MD5校验
                            var md5 = BigheadCrypto.MD5Encode(tableString);
                            var dataName = $"{excelName}${sheetName}";
                            newData.Add(dataName, md5);

                            var tableFileHandler = TableFileHandler.GetHandler(excelName, sheetName);

                            // 如果进入判断说明存在，否则为新增
                            if (oldData.ContainsKey(dataName))
                            {
                                var value = oldData[dataName];
                                // 移除旧数据存储，因为这里已经进行了整体处理。
                                oldData.Remove(dataName);

                                // 存在未改变
                                if (Equals(value, md5)) continue;

                                // 已改变，删除原来生成的数据。  -.csv, -.cs -.bytes
                                EditorUtility.DisplayProgressBar($"删除变更前文件： {sheetName}", path,
                                    createProgress / tableCount);
                                tableFileHandler.Delete();
                            }

                            tableFileHandler.SetContent(tableData);
                            tableFileHandler.Generate();
                        }
                    }
                }
            }

            var deleteProgress = 0f;
            var deleteCount = oldData.Count;
            // 没有被移除的均是被删除的文件或Sheet，在这里要进行删除对应的Csv和.cs文件
            foreach (var key in oldData.Keys)
            {
                ++deleteProgress;
                EditorUtility.DisplayProgressBar($"正在删除旧数据:", key, deleteProgress / deleteCount);

                // 这里的 key 有两种可能：
                // 1、 ExcelName.xlsx
                // 2、 ExcelName $ SheetName
                var array = key.Split('$');

                // 这是Excel文件，无需处理。
                if (Equals(array.Length, 1)) continue;

                var tableFileHandler = TableFileHandler.GetHandler(array[0], array[1]);
                tableFileHandler.Delete();
            }

            var md5Entry = newData.Select(kv => $"{kv.Key}@{kv.Value}").ToArray();
            var md5Content = string.Join(Environment.NewLine, md5Entry);
            // 生成最新的配置文件
            var md5Bytes = Encoding.UTF8.GetBytes(md5Content);
            if (File.Exists(CSV_CONFIG_PATH)) File.Delete(CSV_CONFIG_PATH);

            using (var fileStream = new FileStream(CSV_CONFIG_PATH, FileMode.Create))
            {
                fileStream.Write(md5Bytes, 0, md5Bytes.Length);
            }

            EditorUtility.ClearProgressBar();
        }

        private static List<List<string>> AnalysisTableData(DataRowCollection rows, DataColumnCollection cols)
        {
            // 过滤不生成的列
            var unGenerateColumns = new List<int>();
            // lastColIndex是为了解决当数据后几列都不生成时导致生成的数据格式错误问题。
            var lastColIndex = 0;
            for (int index = 0; index < cols.Count; index++)
            {
                var value = rows[1][index].ToString().Trim();
                if (value == "不生成" || value.StartsWith("#") || string.IsNullOrEmpty(value))
                    unGenerateColumns.Add(index);
                else lastColIndex = index;
            }

            var rowBuilders = new List<List<string>>();
            var allEmptyOrWhiteSpaceOrNull = true;
            for (var row = 0; row < rows.Count; row++)
            {
                var rowBuilder = new List<string>();
                for (var col = 0; col <= lastColIndex; col++)
                {
                    var isUnGenerate = unGenerateColumns.Contains(col);
                    if (isUnGenerate) continue;

                    var grid = rows[row][col].ToString()
                        .Replace('\n', '|')
                        .Replace(",", CustomerGenCsv.CommaSymbol);
                    rowBuilder.Add(grid);

                    if (!string.IsNullOrEmpty(grid) || !string.IsNullOrWhiteSpace(grid))
                    {
                        allEmptyOrWhiteSpaceOrNull = false;
                    }
                }

                if (allEmptyOrWhiteSpaceOrNull) continue;
                rowBuilders.Add(rowBuilder);
            }

            return rowBuilders;
        }

        /// <summary>
        /// 转换表格到字符串
        /// </summary>
        /// <param name="table">表格</param>
        /// <returns>字符串数据</returns>
        private static string Convert2String(IReadOnlyList<List<string>> table)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < table.Count; i++)
            {
                foreach (var grid in table[i])
                {
                    builder.Append(grid);
                    builder.Append(",");
                }

                if (i == table.Count - 1) continue;
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
#endif