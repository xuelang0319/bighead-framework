#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Excel;

namespace Bighead.Csv
{
    public interface IExcelScanner
    {
        Dictionary<string, string> Scan(string excelFolderAbs, out Dictionary<string, TableMeta> metas);
    }

    public sealed class TableMeta
    {
        public int Rows;
        public int Cols;
        public string MD5;
    }

    public sealed class ExcelScanner : IExcelScanner
    {
        private static readonly Regex RxParenEn = new Regex(@"\((\w+)\)", RegexOptions.Compiled);
        private static readonly Regex RxParenCn = new Regex(@"\（(\w+)\）", RegexOptions.Compiled);

        public Dictionary<string, string> Scan(string excelFolderAbs, out Dictionary<string, TableMeta> metas)
        {
            metas = new Dictionary<string, TableMeta>(256, StringComparer.OrdinalIgnoreCase);
            var dict = new Dictionary<string, string>(256, StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(excelFolderAbs) || !Directory.Exists(excelFolderAbs)) return dict;

            var files = Directory.GetFiles(excelFolderAbs, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                .Where(p => !Path.GetFileName(p).StartsWith("~$"))
                .ToArray();

            foreach (var path in files)
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith("#")) continue;
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = path.EndsWith(".xls")
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);
                var dataSet = reader.AsDataSet();
                var excelName = Path.GetFileNameWithoutExtension(path);
                foreach (DataTable table in dataSet.Tables)
                {
                    var sheet = (table?.TableName ?? string.Empty).Trim();
                    if (sheet.Length == 0 || sheet.StartsWith("#")) continue;
                    if (RxParenEn.IsMatch(sheet)) sheet = RxParenEn.Match(sheet).Groups[1].Value;
                    if (RxParenCn.IsMatch(sheet)) sheet = RxParenCn.Match(sheet).Groups[1].Value;

                    var list2D = AnalysisTable(table.Rows, table.Columns, out var effCols, out var effRows);
                    if (effCols == 0 || effRows == 0) continue;
                    var csv = ToCsvRfc4180(list2D);
                    var md5 = Md5Utf8(csv);

                    var key = excelName + "$" + sheet;
                    dict[key] = csv;
                    metas[key] = new TableMeta { Rows = effRows, Cols = effCols, MD5 = md5 };
                }
            }

            return dict;
        }

        private static List<List<string>> AnalysisTable(DataRowCollection rows, DataColumnCollection cols,
            out int effColCount, out int effRowCount)
        {
            var unGen = new HashSet<int>();
            int lastCol = -1;
            for (int ci = 0; ci < cols.Count; ci++)
            {
                var v = (rows.Count > 1 ? rows[1][ci]?.ToString() : string.Empty)?.Trim();
                if (v == "不生成" || (v?.StartsWith("#") ?? false) || string.IsNullOrEmpty(v)) unGen.Add(ci);
                else lastCol = ci;
            }

            if (lastCol < 0)
            {
                effColCount = 0;
                effRowCount = 0;
                return new List<List<string>>();
            }

            var table = new List<List<string>>(rows.Count);
            int countRows = 0;
            for (int ri = 0; ri < rows.Count; ri++)
            {
                bool allEmpty = true;
                var outRow = new List<string>(lastCol + 1);
                for (int ci = 0; ci <= lastCol; ci++)
                {
                    if (unGen.Contains(ci)) continue;
                    var cell = rows[ri][ci]?.ToString() ?? string.Empty;
                    cell = cell.Replace("\r\n", "\n").Replace("\r", "\n");
                    outRow.Add(cell);
                    if (allEmpty && !string.IsNullOrWhiteSpace(cell)) allEmpty = false;
                }

                if (allEmpty) continue;
                table.Add(outRow);
                countRows++;
            }

            effColCount = table.Count > 0 ? table[0].Count : 0;
            effRowCount = countRows;
            return table;
        }

        private static string ToCsvRfc4180(IReadOnlyList<List<string>> table)
        {
            var sb = new StringBuilder(Math.Max(1024, table.Count * 32));
            for (int r = 0; r < table.Count; r++)
            {
                var row = table[r];
                for (int c = 0; c < row.Count; c++)
                {
                    var f = row[c] ?? string.Empty;
                    // ★ 统一使用转义函数，并“强制所有字段加引号”
                    sb.Append(Rfc4180.EscapeField(f, alwaysQuote: true));
                    if (c < row.Count - 1) sb.Append(',');
                }
                if (r < table.Count - 1) sb.Append('\n');
            }
            return sb.ToString();
        }


        private static string Md5Utf8(string s)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s ?? string.Empty));
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
#endif