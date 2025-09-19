using UnityEngine;

namespace Bighead.Upzy
{
    public abstract class UpzyBuildableBase
    {
        /// <summary>模块名称（必填）</summary>
        public abstract string ModuleName { get; }

        /// <summary>模块配置 ScriptableObject（用户可扩展，框架管理）</summary>
        public abstract ScriptableObject ConfigSO { get; }

        /// <summary>框架调用的统一入口：处理版本递增、写配置文件、调用用户 OnBuild</summary>
        public BuildResult Build(string outputRoot)
        {
            var result = OnBuild(outputRoot);
            // 这里框架负责：
            // 1. 根据 result.changeLevel 递增版本
            // 2. 根据 result.entries 更新、替换、删除文件
            // 3. 生成模块 .bd 配置
            return result;
        }

        /// <summary>用户实现的构建方法，返回模块产物</summary>
        protected abstract BuildResult OnBuild(string outputRoot);
    }
}