#if UNITY_EDITOR
using System;
using System.Text;

namespace Bighead.Csv
{
    public static class NameComposer
    {
        // 统一：根据设置生成“基础名”（不含扩展名），用于 csv/bytes/code 文件与类型名
        public static string FileBase(string excel, string sheet, OutputNameMode mode)
        {
            string baseName = mode switch
            {
                OutputNameMode.SheetName   => sheet,
                OutputNameMode.ExcelName   => excel,
                _ /*Excel_Sheet*/          => $"{excel}_{sheet}",
            };
            return SanitizeFileName(baseName);
        }

        public static string TypeBase(string excel, string sheet, OutputNameMode mode)
        {
            // 类型名不允许非字母数字，下划线 OK；与文件名可保持一致但需更严格
            string baseName = mode switch
            {
                OutputNameMode.SheetName   => sheet,
                OutputNameMode.ExcelName   => excel,
                _                          => $"{excel}_{sheet}",
            };
            return SanitizeTypeName(baseName);
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Table";
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            return sb.ToString();
        }

        private static string SanitizeTypeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Table";
            var sb = new StringBuilder(raw.Length + 4);
            // 不能以数字开头
            if (!char.IsLetter(raw[0]) && raw[0] != '_') sb.Append('_');
            foreach (var ch in raw)
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString();
        }
        
        public static void SplitKey(string key, out string excel, out string sheet)
        {
            if (string.IsNullOrEmpty(key)) { excel = ""; sheet = "Sheet1"; return; }
            var i = key.IndexOf('$');
            if (i < 0) { excel = key; sheet = "Sheet1"; }
            else { excel = key.Substring(0, i); sheet = key.Substring(i + 1); }
        }
    }
}
#endif