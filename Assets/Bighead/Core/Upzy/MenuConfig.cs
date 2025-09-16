using System;
using System.Collections.Generic;

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
        public string Version;

        /// <summary>
        /// 菜单生成时间，ISO8601 格式
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// 模块列表
        /// </summary>
        public List<ModuleInfo> Modules = new();
    }

    /// <summary>
    /// 每个模块的最小必要信息
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// 模块名
        /// </summary>
        public string Name;

        /// <summary>
        /// 模块自身的版本号
        /// </summary>
        public string Version;
    }
}