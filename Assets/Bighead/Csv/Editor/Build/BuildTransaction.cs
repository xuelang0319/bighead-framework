#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;

namespace Bighead.Csv
{
    public sealed class BuildTransaction
    {
        private readonly List<string> _tmpFiles = new();
        public void Track(string pathTmp) { if (!string.IsNullOrEmpty(pathTmp)) _tmpFiles.Add(pathTmp); }
        public void Rollback()
        {
            foreach (var f in _tmpFiles)
            { try { if (File.Exists(f)) File.Delete(f); } catch { } }
            _tmpFiles.Clear();
        }
    }
}
#endif