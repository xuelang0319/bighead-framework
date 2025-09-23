//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2020年12月18日  |   文件夹帮助器
//

using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Bighead.Core.Utility
{
    public static class DirectoryHelper
    {
        public static string BuildZip(string sourceDir)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
                {
                    Debug.LogWarning($"[Bighead] 压缩失败，目录不存在: {sourceDir}");
                    return null;
                }

                // 拿到源目录父级
                var parentDir = Directory.GetParent(Path.GetFullPath(sourceDir))?.FullName;
                if (string.IsNullOrEmpty(parentDir))
                {
                    Debug.LogWarning($"[Bighead] 压缩失败，无法解析父目录: {sourceDir}");
                    return null;
                }

                // 使用源目录名 + 时间戳 作为 zip 名称
                string folderName = Path.GetFileName(sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string zipFileName = $"{folderName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                string outputZipPath = Path.Combine(parentDir, zipFileName);

                return BuildZip(sourceDir, outputZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bighead] 生成压缩包失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 标准重载：显式指定输出 zip 完整路径（包含文件名）
        /// </summary>
        /// <param name="sourceDir">需要压缩的目录</param>
        /// <param name="outputZipPath">输出 zip 文件完整路径（含文件名与 .zip 扩展名）</param>
        /// <param name="compression">压缩级别，默认 Optimal</param>
        /// <param name="includeBaseDirectory">是否包含源目录本身作为第一层目录</param>
        /// <returns>成功返回 zip 完整路径；失败返回 null</returns>
        public static string BuildZip(string sourceDir, string outputZipPath, CompressionLevel compression = CompressionLevel.Optimal,
            bool includeBaseDirectory = false)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
                {
                    Debug.LogWarning($"[Bighead] 压缩失败，目录不存在: {sourceDir}");
                    return null;
                }

                if (string.IsNullOrEmpty(outputZipPath))
                {
                    Debug.LogWarning("[Bighead] 压缩失败，输出路径为空");
                    return null;
                }

                // 统一扩展名
                if (!outputZipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    outputZipPath += ".zip";

                var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputZipPath));
                if (string.IsNullOrEmpty(outputDir))
                {
                    Debug.LogWarning($"[Bighead] 压缩失败，无法解析输出目录: {outputZipPath}");
                    return null;
                }

                // 防止把 zip 放在源目录内部，避免自包含风险
                var srcFull = Path.GetFullPath(sourceDir)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var outFull = Path.GetFullPath(outputZipPath);
                if (outFull.StartsWith(srcFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("[Bighead] 输出 zip 不能位于源目录内部（避免自包含）。请改为源目录外部位置。");
                    return null;
                }

                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                if (File.Exists(outputZipPath)) File.Delete(outputZipPath);

                ZipFile.CreateFromDirectory(sourceDir, outputZipPath, compression, includeBaseDirectory);

                Debug.Log($"[Bighead] 已生成压缩包: {outputZipPath}");
                return outputZipPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bighead] 生成压缩包失败: {ex.Message}");
                return null;
            }
        }


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