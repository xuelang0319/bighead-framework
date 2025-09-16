using System;
using System.Globalization;

namespace Bighead.Core
{
    /// <summary>
    /// 表示整包版本号：major.module.feature.patch
    /// </summary>
    public struct VersionNumber : IComparable<VersionNumber>
    {
        public int Major;
        public int Module;
        public int Feature;
        public int Patch;

        public override string ToString()
        {
            return $"{Major}.{Module}.{Feature}.{Patch}";
        }

        public static VersionNumber Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            var parts = value.Split('.');
            if (parts.Length != 4)
                throw new FormatException($"版本号格式错误: {value}");

            return new VersionNumber
            {
                Major = int.Parse(parts[0], CultureInfo.InvariantCulture),
                Module = int.Parse(parts[1], CultureInfo.InvariantCulture),
                Feature = int.Parse(parts[2], CultureInfo.InvariantCulture),
                Patch = int.Parse(parts[3], CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// 逐级比较版本号：Major > Module > Feature > Patch
        /// </summary>
        public int CompareTo(VersionNumber other)
        {
            if (Major != other.Major)
                return Major.CompareTo(other.Major);
            if (Module != other.Module)
                return Module.CompareTo(other.Module);
            if (Feature != other.Feature)
                return Feature.CompareTo(other.Feature);
            return Patch.CompareTo(other.Patch);
        }
    }
}