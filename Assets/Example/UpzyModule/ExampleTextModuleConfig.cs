using Bighead.Core;
using UnityEngine;

namespace Example.UpzyModule
{
    [CreateAssetMenu(fileName = "ExampleTextModuleConfig", menuName = "Bighead/Upzy Example Text Module", order = 1000)]
    public class ExampleTextModuleConfig : ScriptableObject
    {
        public ConfigVersion version = new ConfigVersion(1,0,0,0);

        [Header("源文件根目录（如：Assets/ExampleTextModule）")]
        public string sourceFolder = "Assets/ExampleTextModule";

        [Header("要构建的文件（相对 sourceFolder），留空则打全目录")]
        public string[] files;

        [Header("本次变更等级")]
        public ChangeLevel changeLevel = ChangeLevel.Patch; // 示例：可改为 Feature/Minor/Major
    }
}
