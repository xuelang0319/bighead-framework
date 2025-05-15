#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bighead.Core.Config
{
    public static  class ConfigAutoCreator
    {
        public static T CreateIfMissing<T>(string fullPath, Action<T> createCallback = null) where T : ScriptableObject
        {
            if (File.Exists(fullPath))
                return AssetDatabase.LoadAssetAtPath<T>(fullPath);

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoCreator] Created config: {fullPath}");
            createCallback?.Invoke(asset);
            return asset;
        }
    }
}
#endif