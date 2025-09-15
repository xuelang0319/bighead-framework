// 负责基于旧 manifest 与本次扫描结果生成 BuildPlan（ToBuild/ToKeep/ToDelete）
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Bighead.Csv
{
    public sealed class RebuildPlanner
    {
        public BuildPlan Plan(BuildManifest oldManifest,
            Dictionary<string, TableMeta> currentMetas,
            CsvSettings settings)
        {
            var plan = new BuildPlan();
            var old = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (oldManifest?.items != null)
                foreach (var it in oldManifest.items)
                    if (!string.IsNullOrEmpty(it?.key)) old[it.key] = it.md5 ?? string.Empty;


// ToBuild/ToKeep
            foreach (var kv in currentMetas)
            {
                var key = kv.Key;
                var md5 = kv.Value?.MD5 ?? string.Empty;
                if (!old.TryGetValue(key, out var prevMd5)) { plan.ToBuild.Add(key); }
                else if (!string.Equals(prevMd5, md5, StringComparison.OrdinalIgnoreCase)) { plan.ToBuild.Add(key); }
                else { plan.ToKeep.Add(key); }
            }


// ToDelete（旧有而现无）
            foreach (var oldKey in old.Keys)
            {
                if (currentMetas.ContainsKey(oldKey)) continue;
                NameComposer.SplitKey(oldKey, out var excel, out var sheet);
                var baseName = NameComposer.FileBase(excel, sheet, settings.NameMode);
                var csvFinal = Path.Combine(PathUtil.Rel2Abs(settings.CsvOutFolder), baseName + ".csv");
                var bytesFinal = Path.Combine(PathUtil.Rel2Abs(settings.BytesOutFolder), baseName + ".bytes");
                var codeFinal = Path.Combine(PathUtil.Rel2Abs(settings.CodeOutFolder), baseName + "Row.cs");
                plan.ToDelete.Add(csvFinal);
                plan.ToDelete.Add(bytesFinal);
                plan.ToDelete.Add(codeFinal);
            }


            return plan;
        }
    }


    public sealed class BuildPlan
    {
        public readonly List<string> ToBuild = new(); // keys
        public readonly List<string> ToKeep = new(); // keys
        public readonly List<string> ToDelete = new(); // abs file paths


        public void LogSummary()
        {
            Debug.Log($"[Csv][Plan] build={ToBuild.Count}, keep={ToKeep.Count}, delete={ToDelete.Count}");
        }
    }
}
#endif