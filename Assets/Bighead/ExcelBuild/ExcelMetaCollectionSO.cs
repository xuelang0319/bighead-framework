using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bighead.ExcelBuild
{
    /// <summary>
    /// Excel 解析后的全局快照。
    /// 仅记录所有 Excel 与 Sheet 的结构、类型、内容及校验信息。
    /// 不包含任何业务逻辑或封装配置。
    /// </summary>
    [CreateAssetMenu(menuName = "Bighead/Data/ExcelMetaCollection", fileName = "ExcelMetaCollection")]
    public class ExcelMetaCollectionSO : ScriptableObject
    {
        /// <summary>
        /// 所有解析得到的 Excel 信息集合。
        /// </summary>
        public List<ExcelMeta> Excels = new();

        /// <summary>
        /// 全局MD5，用于标识整体快照状态。
        /// 通常是所有Excel的MD5拼接后再计算得到。
        /// </summary>
        public string GlobalMD5;
    }

    [Serializable]
    public class ExcelMeta
    {
        /// <summary>
        /// Excel 文件名（不含扩展名）
        /// </summary>
        public string ExcelName;

        /// <summary>
        /// Excel 文件路径（完整路径，支持相对或绝对路径）
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 当前 Excel 文件的 MD5（基于文件字节计算）
        /// </summary>
        public string MD5;

        /// <summary>
        /// Excel 内的所有 Sheet 数据集合
        /// </summary>
        public List<SheetMeta> Sheets = new List<SheetMeta>();
    }

    /// <summary>
    /// 单个 Sheet 页的结构定义。
    /// </summary>
    [Serializable]
    public class SheetMeta
    {
        /// <summary>
        /// Sheet 名称。
        /// </summary>
        public string SheetName;

        /// <summary>
        /// Sheet 内容的MD5，用于判断数据变更。
        /// </summary>
        public string MD5;

        /// <summary>
        /// 每一列的键名与类型。
        /// </summary>
        public List<ColumnKey> Keys = new();

        /// <summary>
        /// 表格的完整原始内容（字符串化二维数组）。
        /// 第0行为列名，第1行为类型，第2行起为数据内容。
        /// </summary>
        public string[][] Data;
    }

    /// <summary>
    /// 表格列的键名与类型。
    /// </summary>
    [Serializable]
    public class ColumnKey
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name;

        /// <summary>
        /// 列类型
        /// </summary>
        public string Type;

        /// <summary>
        /// 列描述
        /// </summary>
        public string Desc;
    }
}
