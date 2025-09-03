#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Bighead.Csv
{
    [Serializable] public class BuildEntry { public string key; public int rows; public int cols; public string md5; public string time; }
    [Serializable] public class BuildManifest { public List<BuildEntry> items = new(); }

    public interface IBuildManifestStore
    {
        BuildManifest Load(string outDirAbs);
        void Save(Dictionary<string, TableMeta> metas, CsvSettings s);
    }

    public sealed class BuildManifestStore : IBuildManifestStore
    {
        private const string FileName = "manifest.json";

        public BuildManifest Load(string outDirAbs)
        {
            try
            {
                var path = Path.Combine(outDirAbs ?? string.Empty, FileName);
                if (!File.Exists(path)) return new BuildManifest();
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<BuildManifest>(json) ?? new BuildManifest();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Csv] 读取 manifest 失败，返回空清单。\n" + e);
                return new BuildManifest();
            }
        }

        public void Save(Dictionary<string, TableMeta> metas, CsvSettings s)
        {
            var outDir = PathUtil.Rel2Abs(s.CsvOutFolder);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // 稳定排序，保证 diff 友好
            var sorted = new List<KeyValuePair<string, TableMeta>>(metas);
            sorted.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

            var manifest = new BuildManifest();
            var now = DateTime.UtcNow.ToString("s");
            foreach (var kv in sorted)
            {
                var m = kv.Value;
                manifest.items.Add(new BuildEntry {
                    key = kv.Key, rows = m.Rows, cols = m.Cols, md5 = m.MD5, time = now
                });
            }

            var json = JsonUtility.ToJson(manifest, true);
            var final = Path.Combine(outDir, FileName);
            var tmp = final + ".tmp";

            File.WriteAllText(tmp, json, Encoding.UTF8);
            AtomicReplace(tmp, final);
        }

        private static void AtomicReplace(string tmp, string dst)
        {
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(dst))
            {
                try { File.Replace(tmp, dst, null, ignoreMetadataErrors: true); return; }
                catch { try { File.Delete(dst); } catch { /* ignore */ } }
            }
            File.Move(tmp, dst);
        }
    }
}
#endif
