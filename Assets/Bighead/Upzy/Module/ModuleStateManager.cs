using System;
using System.IO;
using UnityEngine;

namespace Bighead.Core.Upzy
{
    /// <summary>
    /// 管理模块的双版本状态和切换（存储在 persistentDataPath）
    /// </summary>
    public class ModuleStateManager
    {
        private readonly string _moduleRoot;
        private readonly string _stateFilePath;
        private ModuleState _state;

        public ModuleStateManager(string moduleName)
        {
            // 模块根目录自动拼接到 persistentDataPath/Modules/moduleName
            _moduleRoot = Path.Combine(Application.persistentDataPath, "Modules", moduleName);
            _stateFilePath = Path.Combine(_moduleRoot, "module_state.json");

            if (!Directory.Exists(_moduleRoot))
                Directory.CreateDirectory(_moduleRoot);

            LoadState();
        }

        /// <summary>
        /// 当前版本目录完整路径
        /// </summary>
        public string CurrentPath =>
            string.IsNullOrEmpty(_state.Current) ? "" : Path.Combine(_moduleRoot, _state.Current);

        /// <summary>
        /// 上一版本目录完整路径
        /// </summary>
        public string PreviousPath =>
            string.IsNullOrEmpty(_state.Previous) ? "" : Path.Combine(_moduleRoot, _state.Previous);

        private void LoadState()
        {
            if (File.Exists(_stateFilePath))
            {
                _state = JsonUtility.FromJson<ModuleState>(File.ReadAllText(_stateFilePath));
            }
            else
            {
                _state = new ModuleState();
                SaveState();
            }
        }

        private void SaveState()
        {
            var json = JsonUtility.ToJson(_state, true);
            File.WriteAllText(_stateFilePath, json);
        }

        /// <summary>
        /// 原子切换：删除上一版本 → 当前版本转为上一版本 → staging 变为当前版本
        /// </summary>
        public void SwitchTo(string stagingFolderName)
        {
            var stagingDir = Path.Combine(_moduleRoot, stagingFolderName);
            if (!Directory.Exists(stagingDir))
                throw new DirectoryNotFoundException($"Staging 目录不存在: {stagingDir}");

            try
            {
                // 1. 删除上一版本
                if (!string.IsNullOrEmpty(_state.Previous))
                {
                    var previousDir = Path.Combine(_moduleRoot, _state.Previous);
                    if (Directory.Exists(previousDir))
                        Directory.Delete(previousDir, true);
                }

                // 2. 当前版本重命名为上一版本
                if (!string.IsNullOrEmpty(_state.Current))
                {
                    var currentDir = Path.Combine(_moduleRoot, _state.Current);
                    if (Directory.Exists(currentDir))
                    {
                        var previousDir = Path.Combine(_moduleRoot, _state.Current + "_prev");
                        Directory.Move(currentDir, previousDir);
                        _state.Previous = Path.GetFileName(previousDir);
                    }
                }

                // 3. staging 目录重命名为当前版本
                var currentDirName = stagingFolderName;
                _state.Current = currentDirName;
                SaveState();

                Debug.Log($"模块切换完成：Current = {_state.Current}, Previous = {_state.Previous}");
            }
            catch (Exception e)
            {
                Debug.LogError($"模块切换失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 回滚到上一版本
        /// </summary>
        public void Rollback()
        {
            if (string.IsNullOrEmpty(_state.Previous))
            {
                Debug.LogWarning("没有可回滚的上一版本");
                return;
            }

            try
            {
                // 删除当前版本
                if (!string.IsNullOrEmpty(_state.Current))
                {
                    var currentDir = Path.Combine(_moduleRoot, _state.Current);
                    if (Directory.Exists(currentDir))
                        Directory.Delete(currentDir, true);
                }

                // 将 previous 改名为 current
                var previousDir = Path.Combine(_moduleRoot, _state.Previous);
                var currentDirPath = Path.Combine(_moduleRoot, _state.Previous.Replace("_prev", ""));
                Directory.Move(previousDir, currentDirPath);

                _state.Current = Path.GetFileName(currentDirPath);
                _state.Previous = string.Empty;
                SaveState();

                Debug.Log($"回滚成功：Current = {_state.Current}");
            }
            catch (Exception e)
            {
                Debug.LogError($"回滚失败: {e.Message}");
                throw;
            }
        }
    }
}
