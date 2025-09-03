#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Bighead.Csv
{
    public sealed class CsvBuildOrchestrator
    {
        private readonly IExcelScanner _scanner;
        private readonly ITableValidator _validator;
        private readonly IArtifactGenerator _generator;
        private readonly ICommitter _committer;
        private readonly ICodeGenerator _codegen;
        private readonly IBytesEncoder _encoder;
        private readonly IBuildManifestStore _manifest;

        public CsvBuildOrchestrator(
            IExcelScanner scanner,
            ITableValidator validator,
            IArtifactGenerator generator,
            ICommitter committer,
            ICodeGenerator codegen,
            IBytesEncoder encoder,
            IBuildManifestStore manifest
        )
        {
            _scanner = scanner; _validator = validator; _generator = generator;
            _committer = committer; _codegen = codegen; _encoder = encoder; _manifest = manifest;
        }

        public void Run(CsvSettings settings)
        {
            settings.Normalize();
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.ExcelFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.CsvOutFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.BytesOutFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.CodeOutFolder));
            if (settings.DefaultLoadStrategy == LoadStrategy.Eager) settings.DefaultFragmentSize = 0;

            // 清理残留 .tmp
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.CsvOutFolder));
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.BytesOutFolder));
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.CodeOutFolder));

            Debug.Log("[Bighead.Csv] 扫描 Excel...");
            var sheets = _scanner.Scan(PathUtil.Rel2Abs(settings.ExcelFolder), out var metas);
            if (sheets.Count == 0) throw new Exception("未发现任何 Excel 表数据");

            Debug.Log("[Bighead.Csv] 校验...");
            _validator.Validate(sheets, metas, settings); // 失败请抛异常

            var tx = new BuildTransaction();
            try
            {
                AssetDatabase.StartAssetEditing();

                Debug.Log("[Bighead.Csv] 生成 .tmp 产物...");
                _generator.GenerateTmp(sheets, metas, settings, _encoder, _codegen, tx);

                Debug.Log("[Bighead.Csv] 原子提交...");
                _committer.CommitAll(sheets.Keys, settings);

                Debug.Log("[Bighead.Csv] 写入 manifest...");
                _manifest.Save(metas, settings);

                Debug.Log("[Bighead.Csv] 完成。");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Bighead.Csv] 失败，回滚 .tmp。\n" + ex);
                tx.Rollback();
                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif