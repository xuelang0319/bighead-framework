#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bighead.Csv
{
    public sealed partial class CsvBuildOrchestrator
    {
        private readonly IExcelScanner _scanner;
        private readonly ITableValidator _validator;
        private readonly IArtifactGenerator _generator;
        private readonly ICommitter _committer;
        private readonly ICodeGenerator _codegen;
        private readonly IBytesEncoder _encoder;
        private readonly IBuildManifestStore _manifest;


        public CsvBuildOrchestrator(IExcelScanner scanner, ITableValidator validator, IArtifactGenerator generator,
            ICommitter committer, ICodeGenerator codegen, IBytesEncoder encoder, IBuildManifestStore manifest)
        {
            _scanner = scanner;
            _validator = validator;
            _generator = generator;
            _committer = committer;
            _codegen = codegen;
            _encoder = encoder;
            _manifest = manifest;
        }


        public void Run(CsvSettings settings)
        {
            settings.Normalize();
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.ExcelFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.CsvOutFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.BytesOutFolder));
            PathUtil.EnsureDir(PathUtil.Rel2Abs(settings.CodeOutFolder));
            if (settings.DefaultLoadStrategy == LoadStrategy.Eager)
                settings.DefaultFragmentSize = 0;


// 清理残留 .tmp
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.CsvOutFolder));
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.BytesOutFolder));
            AtomicCommitter.CleanupTmpDir(PathUtil.Rel2Abs(settings.CodeOutFolder));


            Debug.Log("[Bighead.Csv] 扫描 Excel...");
            var sheets = _scanner.Scan(PathUtil.Rel2Abs(settings.ExcelFolder), out var metas);
            if (sheets.Count == 0)
                throw new Exception("未发现任何 Excel 表数据");


            Debug.Log("[Bighead.Csv] 校验...");
            _validator.Validate(sheets, metas, settings);


// === 增量计划 ===
            var oldManifest = _manifest.Load(PathUtil.Rel2Abs(settings.CsvOutFolder));
            var planner = new RebuildPlanner();
            var plan = planner.Plan(oldManifest, metas, settings);
            plan.LogSummary();


// 快速命中：无改动且无需删除
            if (plan.ToBuild.Count == 0 && plan.ToDelete.Count == 0)
            {
                Debug.Log("[Bighead.Csv] 已是最新（无改动）。");
                return;
            }


            var sheetsToBuild = sheets
                .Where(kv => plan.ToBuild.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);


            var tx = new BuildTransaction();
            try
            {
                AssetDatabase.StartAssetEditing();


// 构建新增/修改表
                if (sheetsToBuild.Count > 0)
                {
                    Debug.Log("[Bighead.Csv] 生成 .tmp 产物（增量）...");
                    _generator.GenerateTmp(sheetsToBuild, metas, settings, _encoder, _codegen, tx);


                    Debug.Log("[Bighead.Csv] 原子提交（增量）...");
                    _committer.CommitAll(plan.ToBuild, settings);
                }


// 清理需要删除的旧产物
                if (plan.ToDelete.Count > 0)
                {
                    Debug.Log("[Bighead.Csv] 清理已删除表的旧产物...");
                    foreach (var p in plan.ToDelete)
                        AtomicCommitter.DeleteFinal(p);
                }


// 保存 manifest（包含 ToBuild + ToKeep，不含已删除表）
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