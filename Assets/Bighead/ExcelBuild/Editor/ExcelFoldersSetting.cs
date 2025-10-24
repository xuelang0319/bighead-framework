// ExcelFoldersSetting.cs
using System.Collections.Generic;
using UnityEngine;

namespace Bighead.ExcelBuild.Editor
{
    /// <summary>持久化：文件夹路径列表（相对路径，基于 Assets，可包含 ../../）</summary>
    public sealed class ExcelFoldersSetting : ScriptableObject
    {
        public List<string> Folders = new List<string>();
    }
}