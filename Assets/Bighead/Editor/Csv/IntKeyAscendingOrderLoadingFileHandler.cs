using System.Text;
using Bighead.Core.Generate;
using Bighead.Core.Utility;
using Bighead.Csv;

namespace Bighead.Editor.Csv
{
    public class IntKeyAscendingOrderLoadingFileHandler : LazyLoadingFileHandler
    {
        public IntKeyAscendingOrderLoadingFileHandler(string excelName, string sheetName, GeneratorType generatorType,
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
            if (NumberOfRowsPerFragment == 0)
            {
                $"Excel generator setting {SheetName} entry count can not be 0!".Error();
                return;
            }

            var fragmentCount = PureContent.Count / NumberOfRowsPerFragment;
            if (PureContent.Count % NumberOfRowsPerFragment > 0) ++fragmentCount;

            var menuBuilder = new StringBuilder();
            for (int fragmentIndex = 0; fragmentIndex < fragmentCount; fragmentIndex++)
            {
                // Menu data: MaxKey | MaxKey | MaxKey
                if (fragmentIndex < fragmentCount - 1)
                {
                    // 取极限Index
                    var index = (fragmentIndex + 1) * NumberOfRowsPerFragment - 1;
                    var key = PureContent[index][0];
                    menuBuilder.Append(key).Append('|');
                }
                else
                {
                    // 取最后一个 
                    var index = PureContent.Count - 1;
                    var key = PureContent[index][0];
                    menuBuilder.Append(key);
                }
            }

            CreateBytesFile(menuBuilder.ToString(), SheetName, $"Menu_{SheetName}");
        }

        protected override void FillMenuManager(GenFoo foo)
        {
            foo.AddDetail("return new IntKeyAscendingOrderMenuManager(menuInfo);");
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

                var maxIndex = (fragmentIndex + 1) * NumberOfRowsPerFragment - 1;
                if (maxIndex > PureContent.Count - 1) maxIndex = PureContent.Count - 1;
                var maxKey = PureContent[maxIndex][0];
                CreateBytesFile(builder.ToString(), SheetName, $"{SheetName}_{maxKey}");
            }
        }
    }
}