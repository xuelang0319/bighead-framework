using System.Collections.Generic;
using System.IO;
using System.Text;
using Bighead.Core.Utility;

namespace Bighead.Csv
{
    public enum GeneratorType
    {
        EagerLoading,
        RecordAll,
        IntKeyAscendingOrder
    }

    /// <summary>
    /// 静态部分
    /// </summary>
    public abstract partial class TableFileHandler
    {
        public static TableFileHandler GetHandler(string excelName, string sheetName)
        {
            return new EagerLoadingFileHandler(excelName, sheetName, GeneratorType.EagerLoading, 100);
            /*var generatorType = GeneratorType.EagerLoading;
            var numberOfRowsPerFragment = 0;
            var excelSetting = ExcelEditorWindow.GetExcelGeneratorSetting();
            foreach (var excelEntry in excelSetting.clipGeneratorExcelEntries)
            {
                if (string.Equals(excelName, excelEntry.ExcelName) &&
                    string.Equals(sheetName, excelEntry.SheetName))
                {
                    generatorType = excelEntry.GeneratorType;
                    numberOfRowsPerFragment = excelEntry.EntryCount;
                    break;
                }
            }

            return generatorType switch
            {
                GeneratorType.EagerLoading => new EagerLoadingFileHandler(excelName, sheetName, generatorType, numberOfRowsPerFragment),
                GeneratorType.RecordAll => new RecordAllLoadingFileHandler(excelName, sheetName, generatorType, numberOfRowsPerFragment),
                GeneratorType.IntKeyAscendingOrder => new IntKeyAscendingOrderLoadingFileHandler(excelName, sheetName, generatorType, numberOfRowsPerFragment),
                _ => throw new ArgumentOutOfRangeException()
            };*/
        }
    }

    /// <summary>
    /// 基础部分
    /// </summary>
    public abstract partial class TableFileHandler
    {
        #region #### Basic Construct ####

        /// <summary> 表格名称 </summary>
        protected readonly string ExcelName;
        
        /// <summary> 表单名称 </summary>
        protected readonly string SheetName;

        /// <summary> 生成类型 </summary>
        protected readonly GeneratorType GeneratorType;

        /// <summary> 每个碎片文件有多少行数据 </summary>
        protected readonly int NumberOfRowsPerFragment;
        
        /// <summary>
        /// 构造方法
        /// </summary>
        protected TableFileHandler(string excelName, string sheetName, GeneratorType generatorType, int numberOfRowsPerFragment)
        {
            ExcelName = excelName;
            SheetName = sheetName;
            GeneratorType = generatorType;
            NumberOfRowsPerFragment = numberOfRowsPerFragment;
        }

        /// <summary>
        /// 获取生成类名称
        /// </summary>
        protected static string GetClassName(string sheetName) => sheetName + "Csv";

        #endregion

        #region Sheet Content

        /// <summary> 表单内容 - 行、列 （包含台头）</summary>
        protected List<List<string>> SheetContent;

        /// <summary> 纯净内容 - 行、列 （不含台头） </summary>
        protected List<List<string>> PureContent;

        /// <summary> 表单拼接后完整的内容 </summary>
        protected string CompleteContent;

        /// <summary> 多主键时的主键在表格列索引列表 </summary>
        protected List<int> MultiKeysIndexList;

        /// <summary> 键名称 </summary>
        protected string TKeyName;

        /// <summary> 条目名称 </summary>
        protected string TEntryName;

        /// <summary>
        /// 设置表单内容
        /// </summary>
        /// <param name="content">表单内容</param>
        public void SetContent(List<List<string>> content)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < content.Count; i++)
            {
                foreach (var grid in content[i])
                {
                    builder.Append(grid);
                    builder.Append(",");
                }

                if (i == content.Count - 1) continue;
                builder.AppendLine();
            }

            CompleteContent = builder.ToString();
            SheetContent = content;
            PureContent = new List<List<string>>(content);
            PureContent.RemoveRange(0, 3);
            MultiKeysIndexList = CustomerGenCsv.GetMultiKeysIndexList(content[1]);
            
            TKeyName = MultiKeysIndexList.Count == 0 ? SheetContent[1][0] : $"{SheetName}Key";
            TEntryName = $"{SheetName}Row";
        }
        
        /// <summary>
        /// 获取MD5校验码
        /// </summary>
        public string GetMD5()
        {
            var content = $"{GeneratorType}_{NumberOfRowsPerFragment}_{CompleteContent}";
            return BigheadCrypto.MD5Encode(content);
        }
        
        #endregion
    }

    /// <summary>
    /// 自动生成部分
    /// </summary>
    public abstract partial class TableFileHandler
    {
        /// <summary>
        /// 生成
        /// </summary>
        public virtual void Generate()
        {
            GenerateCsv(SheetName, CompleteContent);
        }

        /// <summary>
        /// 删除
        /// </summary>
        public virtual void Delete()
        {
            DeleteCsv(SheetName);
        }
        
        #region Generate Csv

        /// <summary>
        /// 生成 Csv 文件
        /// </summary>
        /// <param name="sheetName">表单名称</param>
        /// <param name="completeContent">完整内容</param>
        private static void GenerateCsv(string sheetName, string completeContent)
        {
            var csvFilePath = $"{CsvEditorAccess.Config.TABLE_CSV_PATH}\\{sheetName}.csv";
            var data = Encoding.UTF8.GetBytes(completeContent);
            using var fileStream = new FileStream(csvFilePath, FileMode.Create);
            fileStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// 删除 Csv 文件
        /// </summary>
        /// <param name="sheetName">表单名称</param>
        private static void DeleteCsv(string sheetName)
        {
            var csvFilePath = $"{CsvEditorAccess.Config.TABLE_CSV_PATH}\\{sheetName}.csv";
            File.Delete(csvFilePath);
        }

        #endregion
    }
}