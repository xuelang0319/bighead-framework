using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bighead.Core.Generate;
using Bighead.Core.Utility;
using UnityEngine;

namespace Bighead.Csv
{
    /// <summary>
    /// 饥饿模式读取
    /// </summary>
    public partial class EagerLoadingFileHandler : TableFileHandler
    {
        public EagerLoadingFileHandler(string excelName, string sheetName, GeneratorType generatorType, int numberOfRowsPerFragment) 
            : base(excelName, sheetName, generatorType, numberOfRowsPerFragment)
        {
        }
    }

    /// <summary>
    /// 饥饿模式读取
    /// </summary>
    public partial class EagerLoadingFileHandler : TableFileHandler
    {
        public override void Generate()
        {
            base.Generate();
            GenerateCs(SheetName, SheetContent);
            GenerateBytes(SheetName, CompleteContent);
        }
        
        private static void GenerateBytes(string sheetName, string tableString)
        {
            var csvBytesPath = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}.bytes";
            var encryptData = BigheadCrypto.Base64Encode(tableString);
            var encryptBytes = Encoding.UTF8.GetBytes(encryptData);
            using var fileStream = new FileStream(csvBytesPath, FileMode.Create);
            using var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(encryptBytes, 0, encryptBytes.Length);
        }

        private void GenerateCs(string sheetName, List<List<string>> table)
        {
            var className = GetClassName(sheetName);
            // Assistant Partial
            var assistantClass = CreateAssistantClass(className);
            // 创建读取类
            var tableClass = CreateTableClass(sheetName, className, table, MultiKeysIndexList, TKeyName, TEntryName);
            // 创建数据类
            var entryClass = CreateTableEntry(sheetName, table);
            // 生成构建器
            var generateBuilder = assistantClass.StartGenerate()
                .Append(Environment.NewLine)
                .Append(tableClass.StartGenerate())
                .Append(Environment.NewLine)
                .Append(entryClass.StartGenerate());
            // 添加多键模式的生成
            AppendMultiKeys(generateBuilder, table, MultiKeysIndexList, TKeyName);
            // 生成文件
            var generateCsPath = Path.Combine(CsvEditorAccess.Config.GENERATE_CS_PATH, "Csv");
            var csCsvNameWithExtension = sheetName + ".cs";
            DirectoryHelper.ForceCreateDirectory(generateCsPath);
            FileHelper.CreateFile(generateCsPath, generateBuilder.ToString(), csCsvNameWithExtension);
        }

        #region Assistant Class

        /// <summary>
        /// Assistant class
        /// </summary>
        private static GenClass CreateAssistantClass(string className)
        {
            // 创建生成类
            var genClass = new GenClass(0, "CsvAssistant")
            {
                IsPartial = true,
                Modifier = GenBasic.modifier.Public_Static
            };
            genClass.SetNameSpace("Bighead");

            // 添加引用数据
            var usingArray = CustomerGenCsv.GetUsingArray();
            genClass.AddUsing(usingArray);
            genClass.AddUsing("static Bighead.CustomerGenCsv");

            // 添加方法
            var foo = genClass.AddFoo($"Get{className}", className);
            foo.Modifier = GenBasic.modifier.Public_Static;
            foo
                .AddDetail(
                    $"if(Equals(null, _instance{className})) _instance{className} = GetCsv<{className}>(\"{className}\");")
                .AddDetail($"return _instance{className};");
            var prop = genClass.AddProperty($"_instance{className}", className);
            prop.Modifier = GenBasic.modifier.Private_Static;

            return genClass;
        }

        #endregion

        #region Table Class
        
        /// <summary>
        /// 创建表类
        /// </summary>
        private static GenClass CreateTableClass(string sheetName, string className, List<List<string>> table, IReadOnlyList<int> multiKeysIndexList, string tKey, string tEntry)
        {
            // 创建读取类
            var genClass = GetTableClass(className, tKey, tEntry);
            // 填充菜单路径属性
            FillFilePath(genClass, sheetName);
            // 填充字符串解析读取键方法
            FillAnalysisKey(genClass, table, multiKeysIndexList, tKey, tEntry);
            // 填充字符串解析数据类方法
            FillAnalysisEntry(genClass, table, tEntry);
            return genClass;
        }

        /// <summary>
        /// 获取表格类
        /// </summary>
        private static GenClass GetTableClass(string className, string tKey, string tEntry)
        {
            var genClass = new GenClass(0, className)
            {
                Modifier = GenBasic.modifier.Public,
                Parent = $"CsvEagerLoading<{tKey}, {tEntry}>",
            };
            genClass.SetNameSpace("Bighead");
            return genClass;
        }
        
        /// <summary>
        /// 填充菜单路径属性
        /// </summary>
        private static void FillFilePath(GenClass genClass, string sheetName)
        {
            var bytesReadPath = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}.bytes".Replace("\\", "/");
            var dataPathLength = Application.dataPath.Length;
            var filePath = $"\"Assets/{bytesReadPath.Substring(dataPathLength + 1, bytesReadPath.Length - dataPathLength - 1)}\"";

            var pathProp = genClass.AddProperty("FolderPath", "string")
                .SetSet(true).SetGet(true)
                .SetValue(filePath).SetOverrider(true);
            pathProp.Modifier = GenBasic.modifier.Protected;
        }
        
        /// <summary>
        /// 填充解析存储键
        /// </summary>
        private static void FillAnalysisKey(GenClass genClass, IReadOnlyList<List<string>> sheetContent, IReadOnlyList<int> multiKeysIndexList, string tKeyName, string tEntryName)
        {
            var analysisKey = genClass.AddFoo("AnalysisKey", tKeyName).AddParam(tEntryName, "entry").SetOverrider(true);
            analysisKey.Modifier = Core.Generate.GenBasic.modifier.Protected;
            
            var names = sheetContent[0];
            if (multiKeysIndexList.Count == 0)
            {
                analysisKey.AddDetail($"return entry.{names[0]};");
            }
            else
            {
                analysisKey.AddDetail($"var key = new {tKeyName}")
                    .AddDetail("{");
                foreach (var index in multiKeysIndexList)
                {
                    var name = sheetContent[0][index];
                    analysisKey.AddDetail($"    {name} = entry.{name},");
                }
                analysisKey
                    .AddDetail("};")
                    .AddDetail("return key;");
            }
        }
        
        /// <summary>
        /// 填充字符串解析数据类方法
        /// </summary>
        private static void FillAnalysisEntry(GenClass genClass, IReadOnlyList<List<string>> sheetContent, string tEntryName)
        {
            var analysisEntry = genClass.AddFoo("AnalysisEntry", tEntryName).AddParam("string", "str").SetOverrider(true);
            analysisEntry.Modifier = GenBasic.modifier.Protected;
            analysisEntry
                .AddDetail("var item = str.Split(',').Select(ConvertCommaSymbol).ToArray();")
                .AddDetail($"var entry = new {tEntryName}();");
            var names = sheetContent[0];
            var types = sheetContent[1];
            for (int i = 0; i < names.Count; i++)
            {
                var value = $"item[{i}].Trim()";
                var name = names[i];
                var func = CustomerGenCsv.GetTransformFunc(types[i], value);
                analysisEntry.AddDetail($"entry.{name} = {func};");
            }
            analysisEntry.AddDetail("return entry;");
        }

        #endregion

        #region Create Table Entry
        
        /// <summary>
        /// 创建数据类
        /// </summary>
        private static GenClass CreateTableEntry(string sheetName, IReadOnlyList<List<string>> sheetContent)
        {
            var genClass = new GenClass(0, $"{sheetName}Row")
            {
                Modifier = GenBasic.modifier.Public,
                IsPartial = true
            };
            genClass.SetNameSpace("Bighead");

            var names = sheetContent[0];
            var types = sheetContent[1];
            var desc = sheetContent[2];

            for (int i = 0; i < names.Count; i++)
            {
                var type = CustomerGenCsv.GetPropertyType(types[i], out _);
                var genProperty = genClass.AddProperty(names[i], type);

                var annotation = CustomerGenCsv.ConvertCommaSymbol(desc[i]);
                genProperty.Annotation = annotation;
            }

            return genClass;
        }

        #endregion
        
        
        /// <summary>
        /// 构建多键Key的结构体
        /// </summary>
        private static void AppendMultiKeys(StringBuilder builder, IReadOnlyList<List<string>> sheetContent, List<int> multiKeysIndexList, string tKeyName)
        {
            if (multiKeysIndexList.Count == 0) return;
            
            var keyStruct = new GenStruct(0, tKeyName);
            foreach (var index in multiKeysIndexList)
            {
                var name = sheetContent[0][index];
                var type = CustomerGenCsv.GetPropertyType(sheetContent[1][index], out _);
                keyStruct.AddProperty(name, type);
            }
            keyStruct.SetNameSpace("Bighead");
            keyStruct.Modifier = GenBasic.modifier.Public;

            builder.AppendLine(keyStruct.StartGenerate().ToString());
        }

        public override void Delete()
        {
            base.Delete();
            DeleteBytes(SheetName);
            DeleteScript(SheetName);
        }

        private static void DeleteBytes(string sheetName)
        {
            var path = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}.bytes";
            File.Delete(path);
        }

        private static void DeleteScript(string sheetName)
        {
            var path = Path.Combine(CsvEditorAccess.Config.GENERATE_CS_PATH, $"Csv\\{sheetName}.cs");
            File.Delete(path);
        }
    }
}