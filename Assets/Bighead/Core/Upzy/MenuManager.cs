using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Bighead.Core.Upzy
{
    /// <summary>
    /// 运行时 Menu 管理器：负责加载、比对、决定是否需要热更
    /// </summary>
    public class MenuManager
    {
        private MenuConfig? _localMenu;
        private MenuConfig? _remoteMenu;

        /// <summary>
        /// 异步加载本地 Menu.json（兼容 StreamingAssets）
        /// </summary>
        public async UniTask<bool> LoadLocalMenuAsync(string path)
        {
            using var request = UnityWebRequest.Get(path);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"本地 Menu.json 读取失败：{request.error}");
                _localMenu = null;
                return false;
            }

            try
            {
                _localMenu = JsonUtility.FromJson<MenuConfig>(request.downloadHandler.text);
                Debug.Log($"本地 Menu.json 加载成功：{_localMenu.Version}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"解析本地 Menu.json 失败：{e.Message}");
                _localMenu = null;
                return false;
            }
        }

        /// <summary>
        /// 异步加载远端 Menu.json
        /// </summary>
        public async UniTask<bool> LoadRemoteMenuAsync(string url)
        {
            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"下载远端 Menu.json 失败：{request.error}");
                return false;
            }

            try
            {
                _remoteMenu = JsonUtility.FromJson<MenuConfig>(request.downloadHandler.text);
                Debug.Log($"远端 Menu.json 加载成功：{_remoteMenu.Version}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"解析远端 Menu.json 失败：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否需要整包更新（major.module.feature 不一致）
        /// </summary>
        public bool NeedFullUpdate()
        {
            if (_localMenu == null || _remoteMenu == null)
                return false;

            var localV = VersionNumber.Parse(_localMenu.Version);
            var remoteV = VersionNumber.Parse(_remoteMenu.Version);

            return localV.Major != remoteV.Major ||
                   localV.Module != remoteV.Module ||
                   localV.Feature != remoteV.Feature;
        }

        /// <summary>
        /// 获取需要更新的模块列表
        /// </summary>
        public List<ModuleInfo> GetChangedModules()
        {
            if (_remoteMenu == null)
                throw new InvalidOperationException("必须先获取远端 Menu");

            if (_localMenu == null)
                return new List<ModuleInfo>(_remoteMenu.Modules);

            var result = new List<ModuleInfo>();
            var localDict = _localMenu.Modules.ToDictionary(m => m.Name, m => m);

            foreach (var remoteModule in _remoteMenu.Modules)
            {
                if (!localDict.TryGetValue(remoteModule.Name, out var localModule) ||
                    localModule.Version != remoteModule.Version)
                {
                    result.Add(remoteModule);
                }
            }

            return result;
        }
    }
}
