//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2020年12月18日  |   文件夹帮助器
//

using System;
using System.IO;
using UnityEngine;

namespace Bighead.Core.Utility
{
    public static class DirectoryHelper
    {
        /// <summary>
        /// Directory 类升级方法。
        /// 检查是否存在该路径，不存在则创建。
        /// 若因路径异常报错则仅显示打印信息。
        /// </summary>
        /// <param name="path"> 项目文件夹下的相对路径 </param>
        /// <returns> 创建前是否存在 </returns>
        public static bool ForceCreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    if (!Directory.CreateDirectory(path).Exists)
                        return false;
                }
                catch (Exception e)
                {
                    $"创建文件夹异常，路径 - {path}。".Error();
                    e.Exception();
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// 清空路径下的所有文件及子文件夹
        /// </summary>
        public static void ClearDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                var directoryInfo = new DirectoryInfo(path);
                directoryInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;
                File.SetAttributes(path, FileAttributes.Normal);

                if (!Directory.Exists(path))
                {
                    $"Delete directory failed. Do not exist {path}".Exception();
                    return;
                }

                foreach (string file in Directory.GetFileSystemEntries(path))
                {
                    if (File.Exists(file))
                        File.Delete(file);
                    else
                        ClearDirectory(file);
                }
                
                Directory.Delete(path);
            }
            catch (Exception e)
            {
                $"Delete directory failed. {path}".Exception();
                e.Exception();
            }
        }
        
        /// <summary>
        /// 通过全路径获取Assets下本地相对路径
        /// </summary>
        public static string GetRelativePath(string fullPath)
        {
            // 获取项目根路径
            string projectPath = Application.dataPath;

            // 确保路径格式一致
            projectPath = projectPath.Replace("\\", "/");
            fullPath = fullPath.Replace("\\", "/");

            // 如果完整路径包含项目路径
            if (fullPath.StartsWith(projectPath))
            {
                // 截取相对路径
                return "Assets" + fullPath.Substring(projectPath.Length);
            }

            // 如果路径不在项目内，返回原路径
            return fullPath;
        }
    }
}