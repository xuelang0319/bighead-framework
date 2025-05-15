using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using static Bighead.Csv.CustomerGenCsv;


namespace Bighead.Csv
{
    public static partial class CsvAssistant
    {
        private static ResourcesCsv _instanceResourcesCsv { get; set;}

        public static ResourcesCsv GetResourcesCsv()
        {
            if(Equals(null, _instanceResourcesCsv)) _instanceResourcesCsv = GetCsv<ResourcesCsv>("ResourcesCsv");
            return _instanceResourcesCsv;
        }
    }
}

namespace Bighead.Csv
{
    public class ResourcesCsv : CsvEagerLoading<int, ResourcesRow>
    {
        protected override string FolderPath { get; set;} = "Assets/GenerateCsv/Csv/Resources.bytes";

        protected override int AnalysisKey(ResourcesRow entry)
        {
            return entry.ID;
        }
        protected override ResourcesRow AnalysisEntry(string str)
        {
            var item = str.Split(',').Select(ConvertCommaSymbol).ToArray();
            var entry = new ResourcesRow();
            entry.ID = ToInt(item[0].Trim());
            entry.Desc = item[1].Trim();
            entry.Label = item[2].Trim();
            entry.Path = item[3].Trim();
            return entry;
        }
    }
}

namespace Bighead
{
    public partial class ResourcesRow
    {
        /// <summary>
        /// 编号
        /// </summary>
        public int ID { get; set;}
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { get; set;}
        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set;}
        /// <summary>
        /// 资源路径
        /// </summary>
        public string Path { get; set;}
    }
}