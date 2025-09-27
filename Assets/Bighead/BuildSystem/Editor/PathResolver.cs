#if UNITY_EDITOR
using UnityEditor;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 路径解析工具，支持 {Platform} 和 {Version} 占位符
    /// </summary>
    public static class PathResolver
    {
        public static string Resolve(string template, BuildTarget platform)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string version = PlayerSettings.bundleVersion;
            return template
                .Replace("{Platform}", platform.ToString())
                .Replace("{Version}", version);
        }
    }
}
#endif