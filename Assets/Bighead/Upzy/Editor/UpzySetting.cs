using System.Collections.Generic;
using System.IO;
using Bighead.Upzy.Core;
using UnityEngine;

namespace Bighead.Upzy.Editor
{
    [CreateAssetMenu(fileName = "UpzySetting", menuName = "Bighead/Upzy Setting", order = 0)]
    public class UpzySetting : ScriptableObject
    {
        [Header("生成产物根目录（建议放在 Assets/UpzyGenerated）")]
        public string rootFolder = "Assets/UpzyGenerated";

        [Header("相对路径配置")]
        public string currentRel = "current";
        public string backupRel  = "backup";
        public string stagingRel = "staging";
        public string modulesRel = "Modules";

        [Header("已注册模块列表")]
        public List<UpzyEntry> registeredModules = new List<UpzyEntry>();

        // 统一计算绝对路径
        public string CurrentAbs => Abs(currentRel);
        public string BackupAbs  => Abs(backupRel);
        public string StagingAbs => Abs(stagingRel);
        public string ModulesAbs(string baseAbs) => Path.Combine(baseAbs, modulesRel);

        private string Abs(string rel)
        {
            string rootAbs = Path.GetFullPath(rootFolder.Replace("\\", "/"));
            return Path.Combine(rootAbs, rel).Replace("\\", "/");
        }
    }
}