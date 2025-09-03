#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Bighead.Csv
{
    public static class PathUtil
    {
        public static string Rel2Abs(string rel)
        {
            rel = string.IsNullOrWhiteSpace(rel) ? string.Empty : rel.Trim().Replace('\\', '/');
            if (string.IsNullOrEmpty(rel)) return Application.dataPath;
            return Path.Combine(Application.dataPath, rel).Replace('\\', '/');
        }

        /// <summary>
        /// 将绝对路径转换为“相对 Assets 的相对路径”（可含 ../ 跳出 Assets）。
        /// 若本就位于 Assets 下，返回形如 "BigheadCsv/..."；
        /// 否则返回形如 "../ExternalData/Tables"。
        /// </summary>
        public static string Abs2RelFromAssets(string abs)
        {
            if (string.IsNullOrEmpty(abs)) return string.Empty;
            var target = abs.Replace('\\', '/').TrimEnd('/');
            var assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');

            // 已在 Assets/A 下 —— 直接去掉前缀 "…/Assets/"
            if (target.StartsWith(assets + "/"))
                return target.Substring(assets.Length + 1);

            // 计算相对路径（最短 ../ 形式）
            var ta = target.Split('/');
            var aa = assets.Split('/');
            int i = 0;
            while (i < ta.Length && i < aa.Length &&
                   string.Equals(ta[i], aa[i], System.StringComparison.OrdinalIgnoreCase)) i++;
            int up = aa.Length - i; // 需要退回多少层
            var sb = new System.Text.StringBuilder(up * 3 + (ta.Length - i) * 8);
            for (int k = 0; k < up; k++) sb.Append("../");
            for (int k = i; k < ta.Length; k++)
            {
                sb.Append(ta[k]);
                if (k < ta.Length - 1) sb.Append('/');
            }

            return sb.ToString();
        }

        public static void EnsureDir(string abs)
        {
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }
    }
}
#endif