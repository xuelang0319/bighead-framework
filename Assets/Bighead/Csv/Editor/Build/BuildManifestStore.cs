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
        public BuildManifest Load(string outDirAbs)
        {
            var path = Path.Combine(outDirAbs, "manifest.json");
            if (!File.Exists(path)) return new BuildManifest();
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonUtility.FromJson<BuildManifest>(json) ?? new BuildManifest();
        }

        public void Save(Dictionary<string,TableMeta> metas, CsvSettings s)
        {
            var m = new BuildManifest();
            foreach (var kv in metas)
                m.items.Add(new BuildEntry { key = kv.Key, rows = kv.Value.Rows, cols = kv.Value.Cols, md5 = kv.Value.MD5, time = DateTime.Now.ToString("s") });
            var json = JsonUtility.ToJson(m, true);
            var path = Path.Combine(PathUtil.Rel2Abs(s.CsvOutFolder), "manifest.json");
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }
}
#endif