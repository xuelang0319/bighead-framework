using System.IO;
using UnityEngine;

namespace Bighead.Core.Upzy
{
    /// <summary>
    /// Upzy 系统路径配置
    /// </summary>
    [CreateAssetMenu(fileName = "UpzyConfig", menuName = "Upzy/Config", order = 0)]
    public class UpzyConfig : ScriptableObject
    {
        [Header("目录设置")]
        public string rootFolder = "Upzy";
        public string currentFolder = "current";
        public string backupFolder = "backup";
        public string stagingFolder = "staging";
        public string modulesFolder = "Modules";

        [Header("文件设置")]
        public string menuFile = "Menu.bd";
        public string backupMenuFile = "Menu.prev";
        public string builtinMenuPath = "Menu.bd";

        public string Root => Path.Combine(Application.persistentDataPath, rootFolder);
        public string CurrentDir => Path.Combine(Root, currentFolder);
        public string BackupDir => Path.Combine(Root, backupFolder);
        public string StagingDir => Path.Combine(Root, stagingFolder);
        public string ModulesDir => Path.Combine(CurrentDir, modulesFolder);

        public string MenuFile => Path.Combine(CurrentDir, menuFile);
        public string BackupMenu => Path.Combine(BackupDir, backupMenuFile);

        public string GetModulePath(string relativePath)
            => Path.Combine(ModulesDir, relativePath);
        public string GetBackupModulePath(string relativePath)
            => Path.Combine(BackupDir, modulesFolder, relativePath);
    }
}