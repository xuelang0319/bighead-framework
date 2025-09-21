using System.Collections.Generic;
using System.IO;
using Bighead.Upzy.Core;
using UnityEngine;

namespace Bighead.Upzy.Editor
{
    [CreateAssetMenu(fileName = "UpzySetting", menuName = "Bighead/UpzySetting")]
    public class UpzySetting : ScriptableObject
    {
        [Header("Upzy 生成路径")]
        public string rootFolder = "Assets/UpzyGenerated";
        public string latestRel = "latest";
        public string releaseRel = "release";
        public string rollbackRel = "rollback";
        public string modulesRel = "Modules";

        [Header("注册模块")]
        public List<UpzyEntry> registeredModules = new List<UpzyEntry>();

        public string LatestAbs => Path.Combine(rootFolder, latestRel);
        public string ReleaseAbs => Path.Combine(rootFolder, releaseRel);
        public string RollbackAbs => Path.Combine(rootFolder, rollbackRel);
        public string ModulesAbs(string root) => Path.Combine(root, modulesRel);
    }
}