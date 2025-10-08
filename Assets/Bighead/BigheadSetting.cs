namespace Bighead
{
    /// <summary>Bighead 全局路径约束：只提供根目录与拼接方法。</summary>
    public static class BigheadSetting
    {
        public const string Root = "Assets/Bighead";
        public static string GeneratedRoot => $"{Root}/Generated";

        public static string ToGenerated(string relative)
            => Normalize(System.IO.Path.Combine(GeneratedRoot, relative ?? string.Empty));

        private static string Normalize(string p) => p.Replace("\\", "/");
    }
}