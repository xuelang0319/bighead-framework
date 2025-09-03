#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace Bighead.Csv
{
    public interface ICodeGenerator
    {
        string Emit(CsvSettings s, string excelName, string sheetName, string csvText);
    }

    public sealed class RowCodeGenerator : ICodeGenerator
    {
        public string Emit(CsvSettings s, string excelName, string sheetName, string csvText)
        {
            return EmitMinimalRowLoader(s, excelName, sheetName, csvText);
        }

        // —— 下方为你此前要求的可编译版（StringBuilder 拼接） ——
        private static string EmitMinimalRowLoader(CsvSettings s, string excelName, string sheetName, string csvText)
        {
            string ns = string.IsNullOrWhiteSpace(s.CodeNamespace) ? "Bighead.Csv.Generated" : s.CodeNamespace;
            var effMaps = new Dictionary<string, (string runtime, string parse)>(StringComparer.OrdinalIgnoreCase);
            void AddMaps(IEnumerable<global::Bighead.Csv.CsvTypeMappingEntry> maps)
            {
                if (maps == null) return;
                foreach (var m in maps)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.csvType)) continue;
                    string key = m.csvType.Trim();
                    string rt = string.IsNullOrWhiteSpace(m.runtimeType) ? "string" : m.runtimeType.Trim();
                    string pf = string.IsNullOrWhiteSpace(m.parseFunc) ? "ParseString" : m.parseFunc.Trim();
                    effMaps[key] = (rt, pf);
                }
            }
            AddMaps(s.TypeMappings);
            var ov = s.Overrides?.Find(o => string.Equals(o.excelName, excelName, StringComparison.OrdinalIgnoreCase) && string.Equals(o.sheetName, sheetName, StringComparison.OrdinalIgnoreCase));
            if (ov != null) { if (!string.IsNullOrWhiteSpace(ov.customNamespace)) ns = ov.customNamespace; AddMaps(ov.customTypeMappings); }

            static List<string> SplitLine(string line)
            {
                var list = new List<string>(); if (line == null) { list.Add(""); return list; }
                int i = 0; while (i < line.Length)
                {
                    if (line[i] == ',') { list.Add(""); i++; continue; }
                    bool quoted = false; if (line[i] == '"') { quoted = true; i++; }
                    var sb = new StringBuilder();
                    while (i < line.Length)
                    {
                        char c = line[i++];
                        if (quoted)
                        {
                            if (c == '"') { if (i < line.Length && line[i] == '"') { sb.Append('"'); i++; } else { quoted = false; break; } }
                            else sb.Append(c);
                        }
                        else { if (c == ',') break; sb.Append(c); }
                    }
                    list.Add(sb.ToString());
                    if (!quoted && i < line.Length && line[i] == ',') i++;
                }
                if (line.EndsWith(",")) list.Add("");
                return list;
            }

            var lines = csvText?.Split('\n') ?? Array.Empty<string>();
            if (lines.Length < 3) throw new Exception($"表头不足三行：{excelName}${sheetName}");
            var colsName = SplitLine(lines[0].TrimEnd('\r'));
            var colsType = SplitLine(lines[1].TrimEnd('\r'));
            int colCount = Math.Min(colsName.Count, colsType.Count);

            static string Id(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "_col";
                var s2 = new StringBuilder(raw.Length + 4);
                if (!char.IsLetter(raw[0]) && raw[0] != '_') s2.Append('_');
                foreach (var ch in raw) s2.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
                var id = s2.ToString();
                switch (id) { case "string": case "int": case "float": case "bool": case "class": case "namespace": case "public": case "private": case "protected": case "internal": case "params": id = "_" + id; break; }
                return id;
            }
            static string NormType(string t) { var tt = (t ?? "").Trim(); if (string.IsNullOrEmpty(tt)) tt = ":str"; if (!tt.StartsWith(":")) tt = ":" + tt; return tt; }

            var fieldDefs = new StringBuilder();
            var assigns = new StringBuilder();
            for (int i = 0; i < colCount; i++)
            {
                var name = Id(colsName[i]);
                var csvt = NormType(colsType[i]);
                if (!effMaps.TryGetValue(csvt, out var map)) map = ("string", "ParseString");
                fieldDefs.Append("        public ").Append(map.runtime).Append(' ').Append(name).AppendLine(";");
                assigns.Append("                row.").Append(name).Append(" = CsvParsers.").Append(map.parse).Append("(cells[").Append(i).AppendLine("]); ");
            }
            
            string typeName = NameComposer.TypeBase(excelName, sheetName, s.NameMode) + "Row";
            var sbOut = new StringBuilder(8192);
            sbOut.AppendLine("// <auto-generated>");
            sbOut.Append("// generated by RowCodeGenerator - ").Append(excelName).Append('$').Append(sheetName).AppendLine();
            sbOut.AppendLine("using System;");
            sbOut.AppendLine("using System.Collections.Generic;");
            sbOut.AppendLine("using System.Globalization;");
            sbOut.AppendLine("using UnityEngine;");
            sbOut.AppendLine();
            sbOut.Append("namespace ").Append(ns).AppendLine();
            sbOut.AppendLine("{");
            sbOut.AppendLine("    [Serializable]");
            sbOut.Append("    public class ").Append(typeName).AppendLine();
            sbOut.AppendLine("    {");
            sbOut.Append(fieldDefs.ToString());
            sbOut.AppendLine("    }");
            sbOut.AppendLine();
            sbOut.Append("    public static class ").Append(typeName).AppendLine("Loader");
            sbOut.AppendLine("    {");
            sbOut.Append("        public static List<").Append(typeName).AppendLine("> Load(string csv)");
            sbOut.AppendLine("        {");
            sbOut.AppendLine("            var list = new List<" + typeName + ">(128);");
            sbOut.AppendLine("            if (string.IsNullOrEmpty(csv)) return list;");
            sbOut.AppendLine("            var lines = csv.Split('\\n');");
            sbOut.AppendLine("            if (lines.Length <= 3) return list;");
            sbOut.AppendLine("            for (int li = 3; li < lines.Length; li++)");
            sbOut.AppendLine("            {");
            sbOut.AppendLine("                var line = lines[li].TrimEnd('\\r');");
            sbOut.AppendLine("                if (string.IsNullOrWhiteSpace(line)) continue;");
            sbOut.AppendLine("                var cells = CsvParsers.SplitCsvLine(line);");
            sbOut.AppendLine("                if (cells == null || cells.Count == 0) continue;");
            sbOut.AppendLine("                var row = new " + typeName + "();");
            sbOut.Append(assigns.ToString());
            sbOut.AppendLine("                list.Add(row);");
            sbOut.AppendLine("            }");
            sbOut.AppendLine("            return list;");
            sbOut.AppendLine("        }");
            sbOut.AppendLine();
            sbOut.AppendLine("        internal static class CsvParsers");
            sbOut.AppendLine("        {");
            sbOut.AppendLine("            public static System.Collections.Generic.List<string> SplitCsvLine(string line)");
            sbOut.AppendLine("            {");
            sbOut.AppendLine("                var list = new System.Collections.Generic.List<string>();");
            sbOut.AppendLine("                if (line == null) { list.Add(string.Empty); return list; }");
            sbOut.AppendLine("                int i = 0; while (i < line.Length)");
            sbOut.AppendLine("                { if (line[i] == ',') { list.Add(string.Empty); i++; continue; } bool quoted = false; if (line[i] == '\"') { quoted = true; i++; } var sb = new System.Text.StringBuilder(); while (i < line.Length) { char c = line[i++]; if (quoted) { if (c == '\"') { if (i < line.Length && line[i] == '\"') { sb.Append('\"'); i++; } else { quoted = false; break; } } else sb.Append(c); } else { if (c == ',') break; sb.Append(c); } } list.Add(sb.ToString()); if (!quoted && i < line.Length && line[i] == ',') i++; }");
            sbOut.AppendLine("                if (line.EndsWith(\",\")) list.Add(string.Empty);");
            sbOut.AppendLine("                return list;");
            sbOut.AppendLine("            }");
            sbOut.AppendLine("            public static string ParseString(string s) => s ?? string.Empty;");
            sbOut.AppendLine("            public static int ParseInt(string s) => int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;");
            sbOut.AppendLine("            public static float ParseFloat(string s) => float.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;");
            sbOut.AppendLine("            public static bool ParseBool(string s) { if (string.IsNullOrWhiteSpace(s)) return false; s = s.Trim(); return string.Equals(s, \"1\", System.StringComparison.Ordinal) || string.Equals(s, \"true\", System.StringComparison.OrdinalIgnoreCase) || string.Equals(s, \"yes\", System.StringComparison.OrdinalIgnoreCase); }");
            sbOut.AppendLine("            public static int[] ParseIntArray(string s) => ParseArray(s, ParseInt);");
            sbOut.AppendLine("            public static float[] ParseFloatArray(string s) => ParseArray(s, ParseFloat);");
            sbOut.AppendLine("            private static T[] ParseArray<T>(string s, System.Func<string,T> conv) { if (string.IsNullOrEmpty(s)) return System.Array.Empty<T>(); var parts = s.Split(new[] {'|',';'}, System.StringSplitOptions.RemoveEmptyEntries); var arr = new T[parts.Length]; for (int i = 0; i < parts.Length; i++) arr[i] = conv(parts[i]); return arr; }");
            sbOut.AppendLine("            public static UnityEngine.Vector2Int ParseV2Int(string s) { if (string.IsNullOrWhiteSpace(s)) return default; var parts = s.Split(new[] {'|',',',' '}, System.StringSplitOptions.RemoveEmptyEntries); int x = parts.Length > 0 ? ParseInt(parts[0]) : 0; int y = parts.Length > 1 ? ParseInt(parts[1]) : 0; return new UnityEngine.Vector2Int(x,y); }");
            sbOut.AppendLine("        }");
            sbOut.AppendLine("    }");
            sbOut.AppendLine("}");
            return sbOut.ToString();
        }
    }
}
#endif