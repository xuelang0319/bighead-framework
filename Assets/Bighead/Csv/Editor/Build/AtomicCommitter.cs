#if UNITY_EDITOR
using System.IO;

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
                SplitKey(key, out var excel, out var sheet);
                var baseName = NameComposer.FileBase(excel, sheet, settings.NameMode);
                var csvFinal = Path.Combine(PathUtil.Rel2Abs(settings.CsvOutFolder), $"{baseName}.csv");
                var bytesFinal = Path.Combine(PathUtil.Rel2Abs(settings.BytesOutFolder), $"{baseName}.bytes");
                var codeFinal = Path.Combine(PathUtil.Rel2Abs(settings.CodeOutFolder), $"{baseName}Row.cs");
                
                ReplaceAtomic(MakeTmp(csvFinal), csvFinal);
                ReplaceAtomic(MakeTmp(bytesFinal), bytesFinal);
                ReplaceAtomic(MakeTmp(codeFinal), codeFinal);
            }
        }

        public static string MakeTmp(string finalPath) => finalPath + ".tmp";

        public static void ReplaceAtomic(string srcTmp, string dstFinal)
        {
            var dir = Path.GetDirectoryName(dstFinal);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(dstFinal))
            {
                try
                {
                    File.Replace(srcTmp, dstFinal, null, true);
                    return;
                }
                catch
                {
                    try
                    {
                        File.Delete(dstFinal);
                    }
                    catch
                    {
                    }
                }
            }

            File.Move(srcTmp, dstFinal);
        }

        public static void CleanupTmpDir(string absDir)
        {
            if (string.IsNullOrEmpty(absDir) || !Directory.Exists(absDir)) return;
            foreach (var f in Directory.GetFiles(absDir, "*.tmp", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(f);
                }
                catch
                {
                }
            }
        }

        private static void SplitKey(string key, out string excel, out string sheet)
        {
            var i = key.IndexOf('$');
            if (i < 0)
            {
                excel = key;
                sheet = "Sheet1";
            }
            else
            {
                excel = key[..i];
                sheet = key[(i + 1)..];
            }
        }
    }
}
#endif