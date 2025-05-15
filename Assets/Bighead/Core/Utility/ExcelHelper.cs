using System;
using System.Data;
using System.IO;
using Excel;
using OfficeOpenXml;

namespace Bighead.Core.Utility
{
    public static class ExcelHelper
    {
        public static bool DoSomething(string path, string sheetName, Action<ExcelWorksheet> callback, bool save = true)
        {
            var info = new FileInfo(path);
            return DoSomething(info, sheetName, callback, save);
        }
        
        public static bool DoSomething(FileInfo info, string sheetName, Action<ExcelWorksheet> callback, bool save = true)
        {
            var find = false;
            using var package = new ExcelPackage(info);
            var sheets = package.Workbook.Worksheets;
            foreach (var sheet in sheets)
            {
                if (string.Equals(sheetName, sheet.Name))
                {
                    find = true;
                    callback?.Invoke(sheet);
                    if(save) package.Save();
                }
            }

            return find;
        }

        /// <summary>
        /// 检查指定路径上的所有Excel文件是否存在指定名字的表单
        /// </summary>
        public static bool ContainExcelSheetName(string folderPath, string sheetName)
        {
            if (!Directory.Exists(folderPath)) return false;
            
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var excelReader = file.EndsWith(".xls")
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);
                var dataSet = excelReader.AsDataSet();

                foreach (DataTable table in dataSet.Tables)
                {
                    // 过滤不生成的表单
                    var tableName = table.TableName.Trim();
                    if (string.Equals(tableName, sheetName)) return true;
                }
            }

            return false;
        }
    }
}