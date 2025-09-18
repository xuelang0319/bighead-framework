using System;
namespace Bighead.Core.Upzy
{
    /// <summary>
    /// 存储单个模块的版本状态：当前、上一版本
    /// </summary>
    [Serializable]
    public class ModuleState
    {
        /// <summary>
        /// 当前版本文件夹名
        /// </summary>
        public string Current;

        /// <summary>
        /// 上一版本文件夹名
        /// </summary>
        public string Previous;
    }
}