using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Bighead.Core
{
    /// <summary>
    /// Menu.json 顶层结构
    /// </summary>
    [Serializable]
    public class MenuConfig
    {
        /// <summary>
        /// 整包版本号
        /// </summary>
        public string Version; // 使用 VersionNumber.ToString() 存储

        /// <summary>
        /// 菜单生成时间，ISO8601 格式
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// 模块清单：模块名 -> 模块信息
        /// </summary>
        public Dictionary<string, ModuleInfo> Modules = new();
    }

    /// <summary>
    /// 每个模块的最小必要信息
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// 模块自身的版本号
        /// </summary>
        public string Version; // 由模块接口提供
    }
    
    /// <summary>
    /// Menu.json 读写工具，基于 Unity JsonUtility
    /// </summary>
    public static class MenuLoader
    {
        /// <summary>
        /// 从文件读取 Menu.json
        /// </summary>
        public static MenuConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Menu.json 不存在: {path}");

            var json = File.ReadAllText(path, Encoding.UTF8);
            var config = JsonUtility.FromJson<MenuConfig>(json);
            if (config == null)
                throw new InvalidOperationException("反序列化 Menu.json 失败");

            return config;
        }

        /// <summary>
        /// 将 MenuConfig 保存到文件
        /// </summary>
        public static void Save(string path, MenuConfig config)
        {
            var json = JsonUtility.ToJson(config, true); // 第二个参数 true = 格式化输出
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }
}