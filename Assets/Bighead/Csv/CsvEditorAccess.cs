#if UNITY_EDITOR
using Bighead.Core.Config;

namespace Bighead.Csv
{
    public class CsvEditorAccess
    {
        public static CsvConfig Config => ConfigRegistryEditor.Get<CsvConfig>();
    }
}

#endif