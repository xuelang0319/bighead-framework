using System.Text;
using Bighead.Core.Generate;
using Bighead.Csv;

namespace Bighead.Editor.Csv
{
    public class RecordAllLoadingFileHandler : LazyLoadingFileHandler
    {
        public RecordAllLoadingFileHandler(string excelName, string sheetName, GeneratorType generatorType,
            int numberOfRowsPerFragment)
            : base(excelName, sheetName, generatorType, numberOfRowsPerFragment)
        {
        }

        public override void Generate()
        {
            base.Generate();
            GenerateMenu();
            GenerateBytes();
        }

        private void GenerateMenu()
        {
            var fragmentCount = PureContent.Count / NumberOfRowsPerFragment;
            if (PureContent.Count % NumberOfRowsPerFragment > 0) ++fragmentCount;

            var menuBuilder = new StringBuilder();
            for (int fragmentIndex = 0; fragmentIndex < fragmentCount; fragmentIndex++)
            {
                // Menu data: path,Key_Key_Key | path,Key_Key_Key | path,Key_Key_Key
                var builder = new StringBuilder();

                if (fragmentIndex != 0) builder.Append("|");
                builder.Append(fragmentIndex).Append(',');
                for (int index = 0; index < NumberOfRowsPerFragment; index++)
                {
                    // 计算当前索引
                    var currentIndex = fragmentIndex * NumberOfRowsPerFragment + index;
                    // 索引超出最大数据大小则退出
                    if (currentIndex >= PureContent.Count) break;

                    if (index != 0) builder.Append("_");
                    if (MultiKeysIndexList.Count == 0)
                    {
                        // Use index 0
                        builder.Append(PureContent[currentIndex][0]);
                    }
                    else
                    {
                        // Unity keys
                        for (int unityKeyIndex = 0; unityKeyIndex < MultiKeysIndexList.Count; unityKeyIndex++)
                        {
                            // key1@key2@key3
                            if (unityKeyIndex != 0) builder.Append("@");
                            var key = PureContent[currentIndex][MultiKeysIndexList[unityKeyIndex]];
                            builder.Append(key);
                        }
                    }
                }

                menuBuilder.Append(builder);
            }

            CreateBytesFile(menuBuilder.ToString(), SheetName, $"Menu_{SheetName}");
        }

        protected override void FillMenuManager(GenFoo foo)
        {
            foo.AddDetail($"return new AllKeyMappingMenuManager<{TKeyName}>(menuInfo, ParseKey);");
        }
        
        private void GenerateBytes()
        {
            // 确定要生成多少个文件
            var fragmentCount = PureContent.Count / NumberOfRowsPerFragment;
            if (PureContent.Count % NumberOfRowsPerFragment > 0) ++fragmentCount;

            for (var fragmentIndex = 0; fragmentIndex < fragmentCount; fragmentIndex++)
            {
                var builder = new StringBuilder();
                for (var index = 0; index < NumberOfRowsPerFragment; index++)
                {
                    var currentIndex = fragmentIndex * NumberOfRowsPerFragment + index;
                    if (currentIndex >= PureContent.Count) break;

                    if (index != 0) builder.AppendLine();

                    var data = PureContent[currentIndex];
                    for (int i = 0; i < data.Count; i++)
                    {
                        var grid = data[i];
                        builder.Append(grid);

                        if (i != data.Count - 1)
                        {
                            builder.Append(",");
                        }
                    }
                }

                CreateBytesFile(builder.ToString(), SheetName, $"{SheetName}_{fragmentIndex}");
            }
        }
    }
}