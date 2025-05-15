using System.Collections.Generic;
using Bighead.Core;
using Bighead.Core.Utility;
using UnityEngine;

namespace Bighead.Csv
{
    /// <summary>
    /// 饥饿加载模式
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntry">配置条目类型</typeparam>
    public abstract class CsvEagerLoading<TKey, TEntry> : CsvBase where TEntry : class, new()
    {
        protected readonly Dictionary<TKey, TEntry> Key2Entry = new ();

        public override void Initialize()
        {
            var asset = Res.LoadAsset<TextAsset>(FolderPath, "Csv").text;
            asset = BigheadCrypto.Base64Decode(asset);
            var entryStrList = CsvReader.ToListWithDeleteFirstLines(asset, 3);
            foreach (var entryStr in entryStrList)
            {
                var entry = AnalysisEntry(entryStr);
                var key = AnalysisKey(entry);
                Key2Entry.Add(key, entry);
            }
        }

        public override void Terminate()
        {
            
        }

        /// <summary>
        /// 通过字符串解析配置
        /// </summary>
        protected abstract TEntry AnalysisEntry(string str);

        /// <summary>
        /// 通过配置解析主键
        /// </summary>
        protected abstract TKey AnalysisKey(TEntry entry);

        /// <summary>
        /// 通过键获取配置条目
        /// </summary>
        /// <param name="key">获取键</param>
        /// <returns>配置条目</returns>
        public TEntry GetRowByKey(TKey key)
        {
            return Key2Entry.TryGetValue(key, out var entry) ? entry : null;
        }
    }
}