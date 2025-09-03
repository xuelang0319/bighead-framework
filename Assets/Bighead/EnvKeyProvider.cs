using Bighead.Csv;
using UnityEngine;

namespace Bighead
{
    [CreateAssetMenu(fileName="EnvKeyProvider", menuName="Bighead/Csv/Env Key Provider")]
    public class EnvKeyProvider : BaseEncryptionKeyProvider
    {
        public string EnvVarName = "CSV_BYTES_KEY";
        public string DefaultIfMissing = "";

        public override string GetKey()
        {
            var v = System.Environment.GetEnvironmentVariable(EnvVarName);
            return string.IsNullOrEmpty(v) ? DefaultIfMissing : v;
        }
    }
}