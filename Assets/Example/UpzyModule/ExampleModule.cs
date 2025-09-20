using System.IO;
using Bighead.Upzy.Core;
using UnityEngine;

public class ExampleModule : UpzyBuildableBase
{
    protected override BuildResult OnBuild(string outputRoot)
    {
        var configJson = JsonUtility.ToJson(ConfigSO);
        string content = $"Message: {(ConfigSO as ExampleModuleSO)?.message}";
        string aggregateHash = $"{content}:{configJson}".GetHashCode().ToString();

        string outFile = Path.Combine(outputRoot, "example.txt");
        Directory.CreateDirectory(outputRoot);
        File.WriteAllText(outFile, content);

        return new BuildResult
        {
            changeLevel = ChangeLevel.Patch,
            aggregateHash = aggregateHash,
            entries = new System.Collections.Generic.List<BuildEntry>
            {
                new BuildEntry
                {
                    fileName = "example.txt",
                    relativePath = "example.txt",
                    hash = content.GetHashCode().ToString(),
                    fileSize = content.Length
                }
            }
        };
        
    }
}