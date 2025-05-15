#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bighead.Core.Config
{
    public static  class ConfigRegistryEditor
    {
        private static readonly Dictionary<Type, ScriptableObject> cache = new();

        public static T Get<T>() where T : ScriptableObject
        {
            var type = typeof(T);
            if (cache.TryGetValue(type, out var existing))
                return (T)existing;

            string assetPath = $"Assets/Bighead/Configs/{type.Name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[EditorConfigRegistry] {type.Name} not found at {assetPath}, auto-creating...");
                asset = ConfigAutoCreator.CreateIfMissing<T>(assetPath);
            }

            cache[type] = asset;
            return asset;
        }
    }
}
#endif