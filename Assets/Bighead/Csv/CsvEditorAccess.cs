#if UNITY_EDITOR
using framework_bighead.Config;

namespace Bighead.Csv
{
    public class CsvEditorAccess
    {
        public static CsvConfig Config => ConfigRegistryEditor.Get<CsvConfig>();
    }
}

#endif