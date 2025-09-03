#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Bighead.Csv
{
    public interface ITableValidator
    {
        void Validate(Dictionary<string,string> sheets, Dictionary<string, TableMeta> metas, CsvSettings settings);
    }

    // 先用空实现占位；接入你现有校验后，若有错误请抛异常
    public sealed class NoopTableValidator : ITableValidator
    {
        public void Validate(Dictionary<string,string> sheets, Dictionary<string, TableMeta> metas, CsvSettings settings)
        {
            if (sheets == null || sheets.Count == 0) throw new Exception("没有可用的表");
        }
    }
}
#endif