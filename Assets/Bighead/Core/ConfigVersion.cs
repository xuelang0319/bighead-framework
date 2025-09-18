using System;

namespace Bighead.Core
{
    /// <summary>
    /// 配置版本号，支持语义化比较：major.minor.feature.patch
    /// </summary>
    [Serializable]
    public struct ConfigVersion : IComparable<ConfigVersion>
    {
        public int major;
        public int minor;
        public int feature;
        public int patch;

        public ConfigVersion(int major, int minor, int feature, int patch)
        {
            this.major = major;
            this.minor = minor;
            this.feature = feature;
            this.patch = patch;
        }

        public int CompareTo(ConfigVersion other)
        {
            if (major != other.major) return major.CompareTo(other.major);
            if (minor != other.minor) return minor.CompareTo(other.minor);
            if (feature != other.feature) return feature.CompareTo(other.feature);
            return patch.CompareTo(other.patch);
        }

        public override string ToString() => $"{major}.{minor}.{feature}.{patch}";

        public static bool operator >(ConfigVersion a, ConfigVersion b) => a.CompareTo(b) > 0;
        public static bool operator <(ConfigVersion a, ConfigVersion b) => a.CompareTo(b) < 0;
        public static bool operator >=(ConfigVersion a, ConfigVersion b) => a.CompareTo(b) >= 0;
        public static bool operator <=(ConfigVersion a, ConfigVersion b) => a.CompareTo(b) <= 0;
    }
}