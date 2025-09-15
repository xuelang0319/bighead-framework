#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace Bighead.Csv
{
    public interface ICommitter
    {
        void CommitAll(System.Collections.Generic.IEnumerable<string> keys, CsvSettings settings);
    }

    public sealed class AtomicCommitter : ICommitter
    {
        public void CommitAll(System.Collections.Generic.IEnumerable<string> keys, CsvSettings settings)
        {
            foreach (var key in keys)
            {
                NameComposer.SplitKey(key, out var excel, out var sheet);
                var baseName = NameComposer.FileBase(excel, sheet, settings.NameMode);
                var csvFinal = Path.Combine(PathUtil.Rel2Abs(settings.CsvOutFolder), $"{baseName}.csv");
                var bytesFinal = Path.Combine(PathUtil.Rel2Abs(settings.BytesOutFolder), $"{baseName}.bytes");
                var codeFinal = Path.Combine(PathUtil.Rel2Abs(settings.CodeOutFolder), $"{baseName}Row.cs");


                ReplaceAtomic(MakeTmp(csvFinal), csvFinal);
                ReplaceAtomic(MakeTmp(bytesFinal), bytesFinal);
                ReplaceAtomic(MakeTmp(codeFinal), codeFinal);
            }
        }
        
        public static void DeleteFinal(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return;
#if UNITY_EDITOR
            var assets = UnityEngine.Application.dataPath.Replace('\\', '/');
            var target = absPath.Replace('\\', '/');
            if (target.StartsWith(assets + "/"))
            {
                var rel = "Assets/" + System.IO.Path.GetRelativePath(assets, target).Replace('\\', '/');
                if (AssetDatabase.DeleteAsset(rel)) return; // 同时清理 .meta
            }
#endif
            try { if (File.Exists(absPath)) File.Delete(absPath); } catch { }
        }

        public static string MakeTmp(string finalPath) => finalPath + ".tmp";

        public static void ReplaceAtomic(string srcTmp, string dstFinal)
        {
            var dir = Path.GetDirectoryName(dstFinal);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(dstFinal))
            {
                try { File.Replace(srcTmp, dstFinal, null, true); return; }
                catch { try { File.Delete(dstFinal); } catch { } }
            }
            File.Move(srcTmp, dstFinal);
        }

        public static void CleanupTmpDir(string absDir)
        {
            if (string.IsNullOrEmpty(absDir) || !Directory.Exists(absDir)) return;
            foreach (var f in Directory.GetFiles(absDir, "*.tmp", SearchOption.AllDirectories))
            { try { File.Delete(f); } catch { } }
        }
    }
}
#endif