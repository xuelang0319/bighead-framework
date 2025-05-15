//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2020年12月17日  |   Csv格式读取方法
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bighead.Core.Utility;
using UnityEngine;

namespace Bighead.Csv
{
    public class CsvReader
    {
        /// <summary>
        /// 根据ResConfig设置获取Csv文本
        ///
        /// [Warning]
        /// *当从AB包中加载时使用的是异步获取，但Resource下使用的是同步获取。
        /// *当切换到Bundle模式下应注意提前加载，否则有可能报空
        /// </summary>
        public static void ReadCsv(string key, Action<string> callback)
        {
            key = key.Split('.')[0];
            Debug.Log(key);
            var textAsset = Resources.Load<TextAsset>(key);
            callback?.Invoke(textAsset.text);
        }

        /// <summary>
        /// 路径读取文件操作
        /// </summary>
        /// <param name="fileFullName">文件全名带格式后缀</param>
        /// <param name="directoryPath">所在文件夹路径</param>
        /// <returns></returns>
        public static string ReadCsvWithPath(string fileFullName, string directoryPath)
        {
            if (!directoryPath.EndsWith("/"))
                directoryPath += "/";

            return ReadCsvWithPath(directoryPath + fileFullName);
        }

        /// <summary>
        /// 路径读取文件操作
        /// </summary>
        /// <param name="fileFullPath">文件全路径带格式后缀</param>
        /// <returns></returns>
        public static string ReadCsvWithPath(string fileFullPath)
        {
            var tempStr = File.ReadAllText(fileFullPath, Encoding.Default);
            return tempStr;
        }

        /// <summary>
        /// 转换为字符串列表，并去除头部指定行数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<string> ToListWithDeleteFirstLines(string str, int deleteLineCount = 0)
        {
            var tempStrs = str.Split('\n');
            if (tempStrs.Length < deleteLineCount)
            {
                $"操作失败，数据行数不足 {deleteLineCount} 行，请检查。 传入字符串： {str}".Error();
                return null;
            }

            List<string> list = new List<string>();

            for (int i = deleteLineCount; i < tempStrs.Length; i++)
            {
                list.Add(tempStrs[i]);
            }

            return list;
        }
    }
}