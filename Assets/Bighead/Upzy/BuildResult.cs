using System.Collections.Generic;
using Bighead.Core;

namespace Bighead.Upzy
{
    public class BuildEntry
    {
        public string fileName;       // 文件名
        public string hash;           // 文件哈希
        public string relativePath;   // 相对路径
        public long fileSize;         // 文件大小
    }
    
    public class BuildResult
    {
        public ChangeLevel changeLevel;        // 变更等级
        public List<BuildEntry> entries = new();
        public string aggregateHash;           // 聚合哈希，用于模块指纹
    }
}