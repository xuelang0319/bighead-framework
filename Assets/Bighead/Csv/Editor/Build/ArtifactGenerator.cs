#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Bighead.Csv
{
    public interface IArtifactGenerator
    {
        void GenerateTmp(
            Dictionary<string, string> sheets,
            Dictionary<string, TableMeta> metas,
            CsvSettings settings,
            IBytesEncoder encoder,
            ICodeGenerator codegen,
            BuildTransaction tx);
    }

    public sealed partial class ArtifactGenerator : IArtifactGenerator
    {
        private HashSet<string> _seen;

        public void GenerateTmp(Dictionary<string, string> sheets,
            Dictionary<string, TableMeta> metas,
            CsvSettings settings,
            IBytesEncoder encoder,
            ICodeGenerator codegen,
            BuildTransaction tx)
        {
            foreach (var kv in sheets)
            {
                NameComposer.SplitKey(kv.Key, out var excel, out var sheet);
                var baseName = NameComposer.FileBase(excel, sheet, settings.NameMode);
                var csvFinal = Path.Combine(PathUtil.Rel2Abs(settings.CsvOutFolder), $"{baseName}.csv");
                var bytesFinal = Path.Combine(PathUtil.Rel2Abs(settings.BytesOutFolder), $"{baseName}.bytes");
                var codeFinal = Path.Combine(PathUtil.Rel2Abs(settings.CodeOutFolder), $"{baseName}Row.cs");


                _seen ??= new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                if (!_seen.Add(csvFinal) || !_seen.Add(bytesFinal) || !_seen.Add(codeFinal))
                    throw new System.Exception($"命名冲突：{excel}${sheet} 生成的文件名与其它表重复：{baseName}");


                var csvTmp = AtomicCommitter.MakeTmp(csvFinal);
                var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: settings.CsvUtf8WithBom);
                File.WriteAllText(csvTmp, kv.Value, enc);
                tx.Track(csvTmp);


                var bytesTmp = AtomicCommitter.MakeTmp(bytesFinal);
                var payload = Encoding.UTF8.GetBytes(kv.Value);
                payload = encoder.Encode(payload, settings);
                File.WriteAllBytes(bytesTmp, payload);
                tx.Track(bytesTmp);


                var codeTmp = AtomicCommitter.MakeTmp(codeFinal);
                var codeStr = codegen.Emit(settings, excel, sheet, kv.Value);
                File.WriteAllText(codeTmp, codeStr, Encoding.UTF8);
                tx.Track(codeTmp);
            }
        }
    }
}

#endif