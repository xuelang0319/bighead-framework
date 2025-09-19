using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bighead.Upzy;
using UnityEngine;

namespace Example.UpzyModule
{
    public class ExampleTextModule : UpzyBuildableBase
    {
        private ExampleTextModuleConfig _cfg;

        public override string ModuleName => _cfg != null ? _cfg.name : "ExampleTextModule";
        public override ScriptableObject ConfigSO => _cfg;
        public void SetConfig(ScriptableObject so) => _cfg = (ExampleTextModuleConfig)so;

        protected override BuildResult OnBuild(string outputRoot)
        {
            var result = new BuildResult { changeLevel = _cfg.changeLevel, entries = new List<BuildEntry>() };

            // 1) 解析源/目标
            string srcRoot = ToAbs(_cfg.sourceFolder);
            string dstRoot = Path.Combine(outputRoot, ModuleName);
            Directory.CreateDirectory(dstRoot);

            // 2) 取清单：指定则用 files；未指定则全量扫描
            var relList = (_cfg.files != null && _cfg.files.Length > 0)
                ? _cfg.files
                : Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories)
                    .Select(p => Path.GetRelativePath(srcRoot, p).Replace("\\", "/"))
                    .ToArray();

            // 3) 复制并生成条目
            foreach (var rel in relList)
            {
                var src = Path.Combine(srcRoot, rel);
                var dst = Path.Combine(dstRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, true);

                result.entries.Add(new BuildEntry {
                    fileName     = Path.GetFileName(rel),
                    relativePath = rel,
                    fileSize     = new FileInfo(src).Length,
                    hash         = HashFile(src)
                });
            }

            // 4) 聚合指纹
            result.aggregateHash = HashAggregate(result.entries);
            return result;
        }

        // ----- helpers -----
        private static string ToAbs(string assetOrAbs)
        {
            if (string.IsNullOrEmpty(assetOrAbs)) return assetOrAbs;
            if (assetOrAbs.StartsWith("Assets/"))
                return Path.Combine(Application.dataPath, assetOrAbs.Substring("Assets/".Length));
            return assetOrAbs; // 已是绝对路径
        }

        private static string HashFile(string abs)
        {
            using var s = File.OpenRead(abs);
            using var sha = SHA256.Create();
            return System.BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
        }

        private static string HashAggregate(IEnumerable<BuildEntry> entries)
        {
            var joined = string.Join("", entries.OrderBy(e => e.relativePath).Select(e => e.hash));
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(joined);
            return System.BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
