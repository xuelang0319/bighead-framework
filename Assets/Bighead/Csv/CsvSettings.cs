using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bighead.Csv
{
    // —— 加载/索引/封装 枚举 ——
    public enum LoadStrategy { Eager = 0, Lazy = 1 }

    public enum IndexStrategy
    {
        None = 0,              // 不分片/无索引
        KeyMapping = 1,        // 原 RecordAll：整表键映射分片
        IntKeyAscendingOrder = 2
    }

    public enum OutputEncoding { None = 0, Base64 = 1, GZipBase64 = 2 }

    public enum OutputChannel { FileSystem = 0, Resources = 1, Addressables = 2, StreamingAssets = 3 }
    
    public enum OutputNameMode { Excel_Sheet = 0, SheetName   = 1,ExcelName   = 2,}

    public enum BytesCryptoMode { None, Xor /*轻量混淆*/, AesGcm /*可选*/ }

    /// <summary>类型映射项（普通可序列化类，避免与任何引擎原生类型冲突）。</summary>
    [Serializable]
    public class CsvTypeMappingEntry
    {
        [Tooltip("CSV 类型标记（如 :int, :float[], :v2i）")] public string csvType;
        [Tooltip("运行时类型（如 int, float[], UnityEngine.Vector2Int）")] public string runtimeType;
        [Tooltip("解析函数（生成代码里调用的方法名，如 ParseIntArray）")] public string parseFunc;
    }
    
    // CsvSettings.cs（节选）
    public enum BytesDataFormat { Raw, Base64, GZipBase64 }

    public abstract class BaseEncryptionKeyProvider : ScriptableObject
    {
        /// 返回“构建期”要用的密钥。返回 null/空串表示无效。
        public abstract string GetKey();
    }

    /// <summary>单表覆写：可覆盖加载方式/分片/索引策略/多键/命名空间/类型映射/通道。</summary>
    [Serializable]
    public sealed class TableOverride
    {
        [Tooltip("Excel 文件名（不含扩展名）")] public string excelName;
        [Tooltip("Sheet 名称")] public string sheetName;

        [Tooltip("覆盖默认加载方式（可空）")] public LoadStrategy? loadStrategy;
        [Tooltip("覆盖默认分片大小（可空，>0 才启用懒加载）")] public int? fragmentSize;
        [Tooltip("覆盖分片索引策略（可空，懒加载生效）")] public IndexStrategy? indexStrategy;

        [Tooltip("主键列索引（多键启用，基于列顺序，如 [0,2]）")] public int[] keyColumns;
        [Tooltip("自定义命名空间（可空）")] public string customNamespace;

        [Tooltip("本表自定义类型映射（可空）")] public List<CsvTypeMappingEntry> customTypeMappings;
        [Tooltip("单表覆盖产物通道（可空）")] public OutputChannel? channel;
    }

    /// <summary>项目唯一配置资产。</summary>
    [CreateAssetMenu(menuName = "Bighead/Csv/Settings", fileName = "CsvSettings")]
    public sealed class CsvSettings : ScriptableObject
    {
        [Header("路径（相对 Assets/）")]
        public string ExcelFolder    = "Bighead/Table/Excel";
        public string CsvOutFolder   = "Bighead/Table/Csv";
        public string BytesOutFolder = "GenerateCsv/Csv";
        public string CodeOutFolder  = "GenerateCsv/Scripts";

        [Header("代码")]
        public string CodeNamespace = "Bighead.Csv.Generated";

        [Header("加载与分片")]
        public LoadStrategy DefaultLoadStrategy = LoadStrategy.Eager;
        [Tooltip("分片大小（仅 Lazy 生效；<=0 视为不分片）")] public int DefaultFragmentSize = 0;
        [Tooltip("懒加载下的索引/分片策略")] public IndexStrategy DefaultIndexStrategy = IndexStrategy.KeyMapping;
        [Tooltip("多主键串接分隔符")] public string KeyJoiner = "@";

        [Header("类型映射")]
        public List<CsvTypeMappingEntry> TypeMappings = new();

        [Header("产物封装与投递")] 
        public OutputChannel Channel = OutputChannel.FileSystem;
        public OutputNameMode NameMode = OutputNameMode.Excel_Sheet;
        
        // —— 原先的 Encoding/EnableCRC 是“全局”的，改为仅用于 Bytes —— 
        // —— Bytes 文件数据格式（用户明确知道是“文件数据格式”）——
        public BytesDataFormat BytesFormat = BytesDataFormat.GZipBase64;

        // —— 是否加密（勾选才会启用）——
        public bool EnableEncryption = false;

        // 固定密钥（直接写死）；当 Provider 为空时使用
        public string FixedEncryptionKey = "";

        // 自定义密钥提供器（可选；如存在且返回非空，则优先于 FixedEncryptionKey）
        public BaseEncryptionKeyProvider EncryptionKeyProvider;

        // CRC（加/不加密都可选）
        public bool BytesEnableCRC = true;

        // CSV 始终明文；仅可选 BOM
        public bool CsvUtf8WithBom = false;
        
        // 全局版本号
        public string GlobalVersion = "1.0.0.0";

        [Header("按表覆写（可选）")]
        public List<TableOverride> Overrides = new();

        public void Normalize()
        {
            ExcelFolder    = NormalizeRelPath(ExcelFolder);
            CsvOutFolder   = NormalizeRelPath(CsvOutFolder);
            BytesOutFolder = NormalizeRelPath(BytesOutFolder);
            CodeOutFolder  = NormalizeRelPath(CodeOutFolder);

            if (string.IsNullOrWhiteSpace(CodeNamespace)) CodeNamespace = "Bighead.Csv.Generated";
            if (string.IsNullOrEmpty(KeyJoiner)) KeyJoiner = "@";

            TypeMappings ??= new List<CsvTypeMappingEntry>();
            if (TypeMappings.Count == 0) TypeMappings.AddRange(DefaultTypeMappings());

            Overrides ??= new List<TableOverride>();
            
            if (FixedEncryptionKey == null) FixedEncryptionKey = "";
        }

        private static string NormalizeRelPath(string rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) return string.Empty;
            rel = rel.Trim().Replace('\\', '/');
            if (rel.StartsWith("/")) rel = rel.TrimStart('/');
            if (rel.EndsWith("/"))   rel = rel.TrimEnd('/');
            return rel;
        }

        private static IEnumerable<CsvTypeMappingEntry> DefaultTypeMappings()
        {
            yield return new CsvTypeMappingEntry { csvType=":str",    runtimeType="string",                   parseFunc="ParseString"    };
            yield return new CsvTypeMappingEntry { csvType=":int",    runtimeType="int",                      parseFunc="ParseInt"       };
            yield return new CsvTypeMappingEntry { csvType=":float",  runtimeType="float",                    parseFunc="ParseFloat"     };
            yield return new CsvTypeMappingEntry { csvType=":bool",   runtimeType="bool",                     parseFunc="ParseBool"      };
            yield return new CsvTypeMappingEntry { csvType=":int[]",  runtimeType="int[]",                    parseFunc="ParseIntArray"  };
            yield return new CsvTypeMappingEntry { csvType=":float[]",runtimeType="float[]",                  parseFunc="ParseFloatArray"};
            yield return new CsvTypeMappingEntry { csvType=":v2i",    runtimeType="UnityEngine.Vector2Int",   parseFunc="ParseV2Int"     };
        }

#if UNITY_EDITOR
        private void OnValidate() => Normalize();
#endif
    }
}