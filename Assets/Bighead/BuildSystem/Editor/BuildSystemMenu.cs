using UnityEditor;

namespace Bighead.BuildSystem.Editor
{
    public static class BuildSystemMenu
    {
        [MenuItem("Bighead/Build", false, 0)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Bighead/Build System");
        }
    }
}