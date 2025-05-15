#if UNITY_EDITOR
using framework_bighead.Config;

namespace framework_bighead.Csv
{
    public class CsvEditorAccess
    {
        public static CsvConfig Config => ConfigRegistryEditor.Get<CsvConfig>();
    }
}
#endif