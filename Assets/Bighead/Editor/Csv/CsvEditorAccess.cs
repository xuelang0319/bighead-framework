#if UNITY_EDITOR
using Bighead.Core.Config;
using Bighead.Csv;

namespace Bighead.Editor.Csv
{
    public class CsvEditorAccess
    {
        public static CsvConfig Config => ConfigRegistryEditor.Get<CsvConfig>();
    }
}

#endif