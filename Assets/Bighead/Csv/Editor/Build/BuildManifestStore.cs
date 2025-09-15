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
    
    [Serializable]
    public class DataConfig
    {
        public string version;
        public string defaultStrategy;
        public string defaultBytesFormat;
        public Dictionary<string, DataConfigTable> tables = new();
    }

    [Serializable]
    public class DataConfigTable
    {
        public string version;
        public string hash;
        public string schemaSignature;
        public string strategy;
        public string format;
        public string bytesKey;
        public List<KeyColumn> keyColumns;
    }

    [Serializable]
    public class KeyColumn
    {
        public int index;
        public string name;
        public string runtimeType;
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

        public void Save(Dictionary<string, TableMeta> metas, CsvSettings s)
        {
            // --- 1. 保存 manifest.json（保持原有逻辑） ---
            var m = new BuildManifest();
            foreach (var kv in metas)
                m.items.Add(new BuildEntry {
                    key = kv.Key,
                    rows = kv.Value.Rows,
                    cols = kv.Value.Cols,
                    md5  = kv.Value.MD5,
                    time = DateTime.Now.ToString("s")
                });
            var manifestJson = JsonUtility.ToJson(m, true);
            var manifestPath = Path.Combine(PathUtil.Rel2Abs(s.CsvOutFolder), "manifest.json");
            File.WriteAllText(manifestPath, manifestJson, Encoding.UTF8);

            // --- 2. 生成 DataConfig.json ---
            var dc = new DataConfig {
                version = s.GlobalVersion, // CsvSettings 中新增字段 or 构建流水线传入
                defaultStrategy = s.DefaultLoadStrategy.ToString(),
                defaultBytesFormat = s.BytesFormat.ToString()
            };

            foreach (var kv in metas)
            {
                var baseName = NameComposer.FileBase(
                    excel: kv.Key.Split('$')[0],
                    sheet: kv.Key.Split('$')[1],
                    mode: s.NameMode);

                var tableCfg = new DataConfigTable {
                    version = s.GlobalVersion,   // 这里暂时和全局同步，后续可细化
                    hash = "md5:" + kv.Value.MD5,
                    schemaSignature = "sha1:" + kv.Value.MD5, // TODO: 实际应使用表头哈希
                    strategy = s.DefaultLoadStrategy.ToString(),
                    format = s.BytesFormat.ToString(),
                    bytesKey = $"CsvBytes/{baseName}",
                    keyColumns = new List<KeyColumn> {
                        new KeyColumn {
                            index = 0,
                            name = "Id",
                            runtimeType = "int"
                        }
                    }
                };
                dc.tables[baseName] = tableCfg;
            }

            var cfgJson = JsonUtility.ToJson(dc, true);
            var cfgPath = Path.Combine(PathUtil.Rel2Abs(s.CsvOutFolder), "DataConfig.json");
            File.WriteAllText(cfgPath, cfgJson, Encoding.UTF8);

            Debug.Log($"[Bighead.Csv] 写入 manifest.json & DataConfig.json 完成");
        }
    }
}
#endif