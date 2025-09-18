/*// UpzyInitializer.cs  (依赖: Cysharp.Threading.Tasks, UnityWebRequest, JsonUtility 或你自定义解码)

using System;
using System.IO;
using Bighead.Upzy;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Bighead.Core.Upzy
{
    public interface IBdCodec
    {
        // 将 .bd 原始字节解码为对象（内部做解密+JSON反序列化）
        T Decode<T>(byte[] data);
    }

    [Serializable]
    public class WorkingMenu
    {
        public Meta meta;
        public ModuleEntry[] modules;
    }

    [Serializable]
    public class Meta
    {
        public ConfigVersion version;
        public string generatedAt;
    }

    [Serializable]
    public class ModuleEntry
    {
        public string name;
        public string config; // 相对路径，如 "Modules/A/a.bd"
    }

    public static class UpzyInitializer
    {
        public static async UniTask InitializeAsync(IBdCodec codec, UpzyConfig config)
        {
            // 读取内置 FactoryMenu
            var builtinPath = Path.Combine(Application.streamingAssetsPath, config.builtinMenuPath);
            var factoryBytes = await ReadStreamingBytesAsync(builtinPath);
            if (factoryBytes == null || factoryBytes.Length == 0)
                throw new FileNotFoundException($"缺少内置菜单: {builtinPath}");

            var factoryMenu = codec.Decode<WorkingMenu>(factoryBytes);

            // 读取当前菜单
            WorkingMenu currentMenu = null;
            if (File.Exists(config.MenuFile))
                currentMenu = codec.Decode<WorkingMenu>(File.ReadAllBytes(config.MenuFile));

            bool needHydrate = currentMenu == null ||
                               (factoryMenu.meta.version > currentMenu.meta.version);

            if (!needHydrate)
            {
                Debug.Log($"[UpzyInit] 保留当前版本 v{currentMenu.meta.version}");
                return;
            }

            if (Directory.Exists(config.Root)) Directory.Delete(config.Root, true);
            Directory.CreateDirectory(config.CurrentDir);

            // 拷贝菜单
            File.WriteAllBytes(config.MenuFile, factoryBytes);

            // 深拷贝模块配置
            foreach (var m in factoryMenu.modules)
            {
                var dstAbs = config.GetModulePath(m.config);
                var dir = Path.GetDirectoryName(dstAbs);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var srcAbs = Path.Combine(Application.streamingAssetsPath, m.config);
                var moduleBytes = await ReadStreamingBytesAsync(srcAbs);
                if (moduleBytes == null || moduleBytes.Length == 0)
                    throw new FileNotFoundException($"缺少内置模块配置: {m.config}");

                File.WriteAllBytes(dstAbs, moduleBytes);
            }

            Debug.Log($"[UpzyInit] 已灌入 WorkingMenu v{factoryMenu.meta.version}");
        }

        private static async UniTask<byte[]> ReadStreamingBytesAsync(string absPath)
        {
            using var req = UnityWebRequest.Get(absPath);
            await req.SendWebRequest();
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.data : null;
        }
    }
}*/