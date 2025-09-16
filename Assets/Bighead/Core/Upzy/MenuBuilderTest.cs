using System.Collections.Generic;
using UnityEditor;

namespace Bighead.Core.Upzy
{
    public static class MenuBuilderTest
    {
        [MenuItem("Tools/Test MenuBuilder")]
        public static void TestBuildMenu()
        {
            // 输出路径（放 StreamingAssets 方便运行时直接读取）
            const string outputPath = "Assets/StreamingAssets/Menu.json";

            // 调用 MenuBuilder
            MenuBuilder.BuildAndSave(outputPath, CollectModules);
        }

        // 模拟收集模块
        private static IEnumerable<ModuleInfo> CollectModules()
        {
            // 模拟两个模块，第二次运行时可以手动改 Version 测试对比功能
            yield return new ModuleInfo { Name = "Core", Version = "1.0.0.0" };
            yield return new ModuleInfo { Name = "Battle", Version = "1.2.0.0" };
        }
    }
}