#if UNITY_EDITOR
using UnityEngine;

namespace Bighead.Csv
{
    public static class CsvGeneratePipeline
    {
        public static void GenerateAll(CsvSettings settings)
        {
            if (settings == null) { Debug.LogError("[Bighead.Csv] settings 为空"); return; }
            var orchestrator = new CsvBuildOrchestrator(
                new ExcelScanner(),
                new NoopTableValidator(), // 先用空校验，你可替换为真实实现
                new ArtifactGenerator(),
                new AtomicCommitter(),
                new RowCodeGenerator(),
                new BytesEncoder(),
                new BuildManifestStore()
            );
            orchestrator.Run(settings);
        }
    }
}
#endif