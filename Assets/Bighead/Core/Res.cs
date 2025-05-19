using System;
using System.Collections.Generic;
using Bighead.Core.Utility;
using Bighead.Csv;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Bighead.Core
{
    public static partial class Res
    {
        /// <summary>
        /// 资源句柄组
        /// </summary>
        private static readonly Dictionary<string, List<AsyncOperationHandle>> Handlers;

        /// <summary>
        /// 构造方法
        /// </summary>
        static Res()
        {
            Handlers = new Dictionary<string, List<AsyncOperationHandle>>();
        }

        /// <summary>
        /// 使用Addressable加载资源,并以Tag进行存储
        /// </summary>
        public static T LoadAsset<T>(string path, string tag) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(path);
            AddHandle(tag, handle);
            var asset = handle.WaitForCompletion();
            return asset;
        }

        /// <summary>
        /// 使用Addressable加载资源,并以Tag进行存储
        /// </summary>
        public static T LoadAsset<T>(int id, string tag) where T : UnityEngine.Object
        {
            var config = CsvAssistant.GetResourcesCsv().GetRowByKey(id);
            if (config == null)
            {
                $"LoadAssetAsync by id {id} failed.".Error();
                return null;
            }

            var handle = Addressables.LoadAssetAsync<T>(config.Path);
            AddHandle(tag, handle);
            var asset = handle.WaitForCompletion();
            return asset;
        }

        /// <summary>
        /// 通过配置表加载资源
        /// </summary>
        public static void LoadAssetAsync<T>(int id, Action<T> callback) where T : UnityEngine.Object
        {
            var config = CsvAssistant.GetResourcesCsv().GetRowByKey(id);
            if (config == null)
            {
                $"LoadAssetAsync by id {id} failed.".Error();
                return;
            }

            LoadAssetAsync<T>(config.Path, config.Label, callback);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static void LoadAssetAsync<T>(string path, string tag, Action<T> callback) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(path);
            AddHandle(tag, handle);

            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(operation.Result);
                }
                else
                {
                    $"Load asset async failed. Asset path : {path}. Status: {operation.Status}. ".Error();
                }
            };
        }

        /// <summary>
        /// 添加管理
        /// </summary>
        private static void AddHandle(string tag, AsyncOperationHandle handle)
        {
            if (!Handlers.TryGetValue(tag, out var list))
            {
                list = new List<AsyncOperationHandle>();
                Handlers[tag] = list;
            }

            list.Add(handle);
        }

        /// <summary>
        /// 以Tag释放资源
        /// </summary>
        public static void ReleaseTag(string tag)
        {
            if (Handlers.TryGetValue(tag, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Addressables.Release(list[i]);
                }

                list.Clear();
                Handlers.Remove(tag);
            }
        }
    }
}