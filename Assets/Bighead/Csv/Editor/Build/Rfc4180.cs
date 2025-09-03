#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace Bighead.Csv
{
    // 备用：如需在其它模块使用 CSV 读写，可把这里的方法公开
    public static class Rfc4180
    {
        public static string EscapeField(string f, bool alwaysQuote = false)
        {
            f ??= string.Empty;
            bool edgeSpace = f.Length > 0 && (char.IsWhiteSpace(f[0]) || char.IsWhiteSpace(f[^1]));
            bool need = alwaysQuote
                        || f.IndexOfAny(new[] { ',', '，', ';', '\t', '"', '\n', '\r' }) >= 0
                        || edgeSpace;
            if (!need) return f;
            return "\"" + f.Replace("\"", "\"\"") + "\"";
        }


        public static List<string> SplitLine(string line)
        {
            var list = new List<string>();
            if (line == null)
            {
                list.Add(string.Empty);
                return list;
            }

            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == ',')
                {
                    list.Add(string.Empty);
                    i++;
                    continue;
                }

                bool q = false;
                if (line[i] == '"')
                {
                    q = true;
                    i++;
                }

                var sb = new StringBuilder();
                while (i < line.Length)
                {
                    char c = line[i++];
                    if (q)
                    {
                        if (c == '"')
                        {
                            if (i < line.Length && line[i] == '"')
                            {
                                sb.Append('"');
                                i++;
                            }
                            else
                            {
                                q = false;
                                break;
                            }
                        }
                        else sb.Append(c);
                    }
                    else
                    {
                        if (c == ',') break;
                        sb.Append(c);
                    }
                }

                list.Add(sb.ToString());
                if (!q && i < line.Length && line[i] == ',') i++;
            }

            if (line.EndsWith(",")) list.Add(string.Empty);
            return list;
        }
    }
}
#endif