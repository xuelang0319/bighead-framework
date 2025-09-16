using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Bighead.Core
{
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