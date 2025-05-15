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
    /// 懒惰加载处理器 - 基础部分
    /// </summary>
    public abstract partial class LazyLoadingFileHandler : TableFileHandler
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        protected LazyLoadingFileHandler(string excelName, string sheetName, GeneratorType generatorType,
            int numberOfRowsPerFragment) :
            base(excelName, sheetName, generatorType, numberOfRowsPerFragment)
        {
        }
    }
    
    /// <summary>
    /// 懒惰加载处理器 - 生成部分
    /// </summary>
    public abstract partial class LazyLoadingFileHandler : TableFileHandler
    {
        /// <summary>
        /// 生成方法
        /// </summary>
        public override void Generate()
        {
            base.Generate();
            GenerateScript();
        }
        
        /// <summary>
        /// 创建生成脚本
        /// </summary>
        private void GenerateScript()
        {
            // 创建助手类
            var assistantClass = CreateAssistantClass(SheetName);
            // 创建读取类
            var tableClass = CreateTableClass();
            // 创建数据类
            var entryClass = CreateTableEntry(TEntryName, SheetContent);
            // 生成构建器
            var generateBuilder = assistantClass.StartGenerate()
                .Append(Environment.NewLine)
                .Append(tableClass.StartGenerate())
                .Append(Environment.NewLine)
                .Append(entryClass.StartGenerate());
            // 多主键额外添加存储键结构体
            AppendMultiKeys(generateBuilder, SheetContent, MultiKeysIndexList, TKeyName);
            // 生成文件
            var generateCsPath = Path.Combine(CsvEditorAccess.Config.GENERATE_CS_PATH, "Csv");
            var csCsvNameWithExtension = SheetName + ".cs";
            DirectoryHelper.ForceCreateDirectory(generateCsPath);
            FileHelper.CreateFile(generateCsPath, generateBuilder.ToString(), csCsvNameWithExtension);
        }
        
        #region Create Assistant Class
        
        /// <summary>
        /// Assistant class
        /// </summary>
        private static GenClass CreateAssistantClass(string sheetName)
        {
            // 创建生成类
            var genClass = new GenClass(0, "CsvAssistant")
            {
                IsPartial = true,
                Modifier = GenBasic.modifier.Public_Static
            };
            genClass.AddUsing("Bighead.Csv.CustomerGenCsv");
            genClass.SetNameSpace("Bighead.Csv");

            // 添加引用数据
            var usingArray = CustomerGenCsv.GetUsingArray();
            genClass.AddUsing(usingArray);

            // 添加方法
            var className = GetClassName(sheetName);
            var foo = genClass.AddFoo($"Get{className}", className);
            foo.Modifier = GenBasic.modifier.Public_Static;
            foo.AddDetail($"if(Equals(null, _instance{className})) _instance{className} = GetCsv<{className}>(\"{className}\");").AddDetail($"return _instance{className};");
            var prop = genClass.AddProperty($"_instance{className}", className);
            prop.Modifier = GenBasic.modifier.Private_Static;

            return genClass;
        }
        
        #endregion
        
        #region Create Table Class

        /// <summary>
        /// 创建表类
        /// </summary>
        private GenClass CreateTableClass()
        {
            // 获取创建类名
            var className = GetClassName(SheetName);
            // 创建读取类
            var genClass = GetTableClass(className, TKeyName, TEntryName);
            // 填充菜单路径属性
            FillFilePath(genClass, SheetName);
            // 填充表单名称
            FillSheetName(genClass, SheetName);
            // 填充菜单管理器
            FillMenuManager(genClass);
            // 填充键转换方法
            FillParseKey(genClass);
            // 填充字符串解析读取键方法
            FillAnalysisKey(genClass, SheetContent[0], MultiKeysIndexList, TKeyName, TEntryName);
            // 填充字符串解析数据类方法
            FillAnalysisEntry(genClass, SheetName, SheetContent);
            return genClass;
        }

        /// <summary>
        /// 创建读取类
        /// </summary>
        private static GenClass GetTableClass(string className, string tKey, string tEntry)
        {
            var genClass = new GenClass(0, className)
            {
                Modifier = GenBasic.modifier.Public,
                Parent = $"CsvLazyLoading<{tKey}, {tEntry}>",
            };
            genClass.SetNameSpace("Bighead");
            return genClass;
        }

        /// <summary>
        /// 填充菜单路径属性
        /// </summary>
        private static void FillFilePath(GenClass genClass, string sheetName)
        {
            var dataPathLength = Application.dataPath.Length;
            var fullFolderPath = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}".Replace("\\", "/");
            var relativeFolderPath = $"\"Assets/{fullFolderPath.Substring(dataPathLength + 1, fullFolderPath.Length - dataPathLength - 1)}\"";
            
            var pathProp = genClass.AddProperty("FolderPath", "string")
                .SetSet(true).SetGet(true).SetValue(relativeFolderPath).SetOverrider(true);
            pathProp.Modifier = GenBasic.modifier.Protected;
        }

        /// <summary>
        /// 填充表单名称属性
        /// </summary>
        private static void FillSheetName(GenClass genClass, string sheetName)
        {
            var pathProp = genClass.AddProperty("SheetName", "string")
                .SetSet(true).SetGet(true)
                .SetValue($"\"{sheetName}\"").SetOverrider(true);
            pathProp.Modifier = GenBasic.modifier.Protected;
        }

        /// <summary>
        /// 填充菜单管理器
        /// </summary>
        private void FillMenuManager(GenClass genClass)
        {
            var foo = genClass
                .AddFoo("GetMenuManager", $"TableMenuManager<{TKeyName}>")
                .AddParam("string", "menuInfo")
                .SetOverrider(true);
            foo.Modifier = GenBasic.modifier.Protected;
            FillMenuManager(foo);
        }

        /// <summary>
        /// 子类自定义填充菜单管理器创建内容
        /// </summary>
        protected abstract void FillMenuManager(GenFoo foo);

        /// <summary>
        /// 填充键转换方法
        /// </summary>
        private void FillParseKey(GenClass genClass)
        {
            var parseKeyFoo = genClass
                .AddFoo("ParseKey", TKeyName)
                .AddParam("string", "keyString");
            parseKeyFoo.Modifier = GenBasic.modifier.Protected;
            
            if (MultiKeysIndexList.Count == 0)
            {
                AddSingleKeyDetail();
            }
            else
            {
                AddMultiKeysDetail();
                AddMultiKeysGetEntryFunc(); // 多主键需要添加方便客用的适配器
            }
            // 单主键模式填充方法内容
            void AddSingleKeyDetail()
            {
                parseKeyFoo.AddDetail($"return {CustomerGenCsv.GetTransformFunc(TKeyName, "keyString")};");
            }
            // 多主键模式填充方法内容
            void AddMultiKeysDetail()
            {
                parseKeyFoo.AddDetail("var keys = keyString.Split(\"@\");")
                    .AddDetail($"var key = new {TKeyName}")
                    .AddDetail("{");
                for (int i = 0; i < MultiKeysIndexList.Count; i++)
                {
                    var name = SheetContent[0][MultiKeysIndexList[i]];
                    var type = SheetContent[1][MultiKeysIndexList[i]];
                    var func = CustomerGenCsv.GetTransformFunc(type, $"keys[{i}]");
                    parseKeyFoo.AddDetail($"    {name} = {func},");
                }
                parseKeyFoo
                    .AddDetail("};")
                    .AddDetail("return key;");
            }
            // 多主键模式填充读取适配器
            void AddMultiKeysGetEntryFunc()
            {
                var foo = genClass.AddFoo("GetEntry", TEntryName);
                foo.AddDetail($"var key = new {TKeyName}")
                    .AddDetail("{");
                foreach (var index in MultiKeysIndexList)
                {
                    var name = SheetContent[0][index];
                    var type = SheetContent[1][index];
                    var paramType = CustomerGenCsv.GetPropertyType(type, out _);
                    // 添加参数
                    foo.AddParam(paramType, name);
                    // 添加数据
                    foo.AddDetail($"    {name} = {name},");
                }
                foo.AddDetail("};")
                    .AddDetail("return  GetEntry(key);");
            }
        }
        
        /// <summary>
        /// 填充字符串解析数据类方法
        /// </summary>
        private static void FillAnalysisEntry(GenClass genClass, string sheetName, IReadOnlyList<List<string>> table)
        {
            var tEntry = $"{sheetName}Row";
            var analysisEntry = genClass
                .AddFoo("AnalysisEntry", tEntry)
                .AddParam("string", "str").SetOverrider(true);
            analysisEntry.Modifier = GenBasic.modifier.Protected;
            analysisEntry
                .AddDetail("var item = str.Split(',').Select(ConvertCommaSymbol).ToArray();")
                .AddDetail($"var entry = new {tEntry}();");

            var names = table[0];
            var types = table[1];
            for (int i = 0; i < names.Count; i++)
            {
                var value = $"item[{i}].Trim()";
                var name = names[i];
                var func = CustomerGenCsv.GetTransformFunc(types[i], value);
                analysisEntry.AddDetail($"entry.{name} = {func};");
            }

            analysisEntry.AddDetail("return entry;");
        }
        
        /// <summary>
        /// 填充解析存储键
        /// </summary>
        private static void FillAnalysisKey(GenClass genClass, IReadOnlyList<string> names, List<int> multiKeysIndexList, string tKeyName, string tEntryName)
        {
            var analysisKey = genClass.AddFoo("AnalysisKey", tKeyName).AddParam(tEntryName, "entry").SetOverrider(true);
            analysisKey.Modifier = GenBasic.modifier.Protected;
            
            if (multiKeysIndexList.Count == 0)
            {
                analysisKey.AddDetail($"return entry.{names[0]};");
            }
            else
            {
                analysisKey
                    .AddDetail($"var key = new {tKeyName}")
                    .AddDetail("{");
                foreach (var index in multiKeysIndexList)
                {
                    var name = names[index];
                    analysisKey.AddDetail($"    {name} = entry.{name},");
                }
                analysisKey
                    .AddDetail("};")
                    .AddDetail("return key;");
            }
        }
        
        #endregion

        #region Create Table Entry
        
        /// <summary>
        /// 创建数据类
        /// </summary>
        private static GenClass CreateTableEntry(string tEntryName, IReadOnlyList<List<string>> sheetContent)
        {
            var genClass = new GenClass(0, tEntryName)
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

        #region Append MultiKeys
        
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
            keyStruct.SetNameSpace("Bighead.Csv");
            keyStruct.Modifier = GenBasic.modifier.Public;

            builder.AppendLine(keyStruct.StartGenerate().ToString());
        }
        
        #endregion
        
        protected void CreateBytesFile(string str, string sheetName, string fileName)
        {
            str = BigheadCrypto.Base64Encode(str);
            var data = Encoding.UTF8.GetBytes(str);
            var directory = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}";
            DirectoryHelper.ForceCreateDirectory(directory);
            var path = $"{directory}\\{fileName}.bytes";
            using var fileStream = new FileStream(path, FileMode.Create);
            fileStream.Write(data, 0, data.Length);
        }
    }

    /// <summary>
    /// 懒惰加载处理器 - 删除部分
    /// </summary>
    public abstract partial class LazyLoadingFileHandler : TableFileHandler
    {
        public override void Delete()
        {
            base.Delete();
            DeleteScript(SheetName);
            DeleteBytes(SheetName); // 由于整体的Bytes存储在一个文件夹里，所以这里额外删除了文件夹，节省子类都要实现的问题。
        }

        private static void DeleteBytes(string sheetName)
        {
            var directory = $"{CsvEditorAccess.Config.GENERATE_CSV_BYTES_PATH}\\{sheetName}";
            DirectoryHelper.ClearDirectory(directory);
        }

        private static void DeleteScript(string sheetName)
        {
            var path = Path.Combine(CsvEditorAccess.Config.GENERATE_CS_PATH, "Csv");
            path = Path.Combine(path, $"{sheetName}.cs");
            File.Delete(path);
        }
    }
}