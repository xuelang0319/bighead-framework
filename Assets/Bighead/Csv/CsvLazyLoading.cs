using System;
using System.Collections.Generic;
using Bighead.Core;
using Bighead.Core.Utility;
using UnityEngine;

namespace Bighead.Csv
{
    /// <summary>
    /// 懒惰加载模式 （碎片加载）
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntry">配置条目类型</typeparam>
    public abstract class CsvLazyLoading<TKey, TEntry> : CsvBase where TEntry : class, new()
    {
        /// <summary> 目录管理器 </summary>
        private TableMenuManager<TKey> _menuManager;

        /// <summary> 碎片管理器 </summary>
        private TableFragmentManager<TKey, TEntry>[] _fragmentManagers;
        
        /// <summary> 通过Tag获取所有正在使用的碎片管理器 </summary>
        private Dictionary<string, List<TableFragmentManager<TKey, TEntry>>> _tag2UsingTableFragmentManager = new ();

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            var filePath = FolderPath + $"/Menu_{SheetName}.bytes";
            var asset = Res.LoadAsset<TextAsset>(filePath, "Csv").text;
            asset = BigheadCrypto.Base64Decode(asset);
            _menuManager = GetMenuManager(asset);
            var fragmentCount = _menuManager.GetFragmentCount();
            _fragmentManagers = new TableFragmentManager<TKey, TEntry>[fragmentCount];
        }

        public override void Terminate()
        {
            _menuManager = null;
            _fragmentManagers = null;
            _tag2UsingTableFragmentManager = null;
        }

        /// <summary>
        /// 获取目录管理器
        /// </summary>
        /// <param name="menuInfo">目录文件详细信息</param>
        /// <returns>目录管理器</returns>
        protected abstract TableMenuManager<TKey> GetMenuManager(string menuInfo);

        /// <summary>
        /// 通过键获取配置条目
        /// </summary>
        /// <param name="tag">标记</param>
        /// <param name="key">获取键</param>
        /// <returns>配置条目</returns>
        public TEntry GetEntry(TKey key, string tag = "")
        {
            var fragmentIndex = GetFragmentIndex(key);
            var fragmentManager = GetFragmentManagerByIndex(fragmentIndex);

            var entry = fragmentManager?.GetEntry(key, true);
            if (entry == null) return null;

            if (!_tag2UsingTableFragmentManager.TryGetValue(tag, out var usingTableFragmentManagers))
            {
                usingTableFragmentManagers = new List<TableFragmentManager<TKey, TEntry>>();
                _tag2UsingTableFragmentManager.Add(tag, usingTableFragmentManagers);
            }

            if (!usingTableFragmentManagers.Contains(fragmentManager))
            {
                usingTableFragmentManagers.Add(fragmentManager);
                fragmentManager.StartUse();
            }

            return entry;
        }

        /// <summary>
        /// 预加载
        /// </summary>
        public void Preload(List<TKey> keys, string tag = "")
        {
            if (!_tag2UsingTableFragmentManager.TryGetValue(tag, out var usingTableFragmentManagers))
            {
                usingTableFragmentManagers = new List<TableFragmentManager<TKey, TEntry>>();
                _tag2UsingTableFragmentManager.Add(tag, usingTableFragmentManagers);
            }

            foreach (var key in keys)
            {
                var fragmentIndex = GetFragmentIndex(key);
                var fragmentManager = GetFragmentManagerByIndex(fragmentIndex);
                
                // 获取FragmentManager失败或者已经引用过，则不再做处理。
                if (fragmentManager == null || usingTableFragmentManagers.Contains(fragmentManager)) continue;

                // 尝试获取该键，如果能获取到则添加引用。
                // 这里是因为某些存储如升序排列，只能检索在某个文件的范围内，但是是否真实存在存疑的情况。
                // 在该情况下，即使加载进来引用计数也依然为0，所以可能需要手动进行释放。
                if (fragmentManager.TryGetEntry(key, out _))
                {
                    fragmentManager.StartUse();
                    usingTableFragmentManagers.Add(fragmentManager);
                }
            }
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void Unload(string tag, bool tryReleaseAllUsing = false)
        {
            if (!_tag2UsingTableFragmentManager.TryGetValue(tag, out var usingTableFragmentManagers)) return;

            foreach (var tableFragmentManager in usingTableFragmentManagers)
            {
                tableFragmentManager.FinishUse();
                if (tryReleaseAllUsing)
                {
                    var success = tableFragmentManager.TryRelease();
                    if (success)
                    {
                        _fragmentManagers[tableFragmentManager.Index] = null;
                    }
                }
            }
        }

        /// <summary>
        /// 释放所有未引用的内存。
        /// 建议在场景切换等时机调用。
        /// </summary>
        public void ReleaseUseless()
        {
            for (int i = 0; i < _fragmentManagers.Length; i++)
            {
                var success = _fragmentManagers[i]?.TryRelease() ?? false;
                if (success)
                {
                    _fragmentManagers[i] = null;
                }
            }
        }

        /// <summary>
        /// 通过Key获取指定FragmentIndex,这个Index可能会为-1或者模糊索引。
        /// 即使为有效索引也可能取不到数据。
        /// </summary>
        /// <param name="key">获取键</param>
        /// <returns>所在的碎片管理器</returns>
        private int GetFragmentIndex(TKey key)
        {
            return _menuManager.GetFragmentIndex(key);
        }

        /// <summary>
        /// 通过指定索引获取碎片管理器
        /// </summary>
        private TableFragmentManager<TKey, TEntry> GetFragmentManagerByIndex(int index)
        {
            if (!_fragmentManagers.CheckIndexLegal(index)) return null;
            
            // 如何初始化碎片管理器
            if (_fragmentManagers[index] == null)
            {
                var filePath = _menuManager.GetFragmentFilePath(index);
                if (string.IsNullOrEmpty(filePath)) return null;

                filePath = $"{FolderPath}/{SheetName}_{filePath}.bytes";
                var fragmentInfo = ReadFragmentInfo(filePath);
                var fragmentManager = new TableFragmentManager<TKey, TEntry>(AnalysisEntry, AnalysisKey) { Index = index };
                fragmentManager.AnalysisEntry(fragmentInfo);
                _fragmentManagers[index] = fragmentManager;
            }

            return _fragmentManagers[index];
        }

        /// <summary>
        /// 通过路径获取目录文件内的数据
        /// </summary>
        /// <param name="path">读取路径</param>
        /// <returns>目录数据</returns>
        protected string ReadFragmentInfo(string path)
        {
            var asset = Res.LoadAsset<TextAsset>(path, "Csv").text;
            asset = BigheadCrypto.Base64Decode(asset);
            return asset;
        }

        /// <summary>
        /// 通过字符串解析配置
        /// </summary>
        protected abstract TEntry AnalysisEntry(string str);

        /// <summary>
        /// 通过配置解析主键
        /// </summary>
        protected abstract TKey AnalysisKey(TEntry entry);
    }

    /// <summary>
    /// 目录管理器
    /// </summary>
    public abstract class TableMenuManager<TKey>
    {
        /// <summary>
        /// 通过键获取配置所在的碎片数据文件索引编号
        /// </summary>
        /// <param name="key">查找键</param>
        /// <returns>碎片数据文件索引编号</returns>
        public abstract int GetFragmentIndex(TKey key);

        /// <summary>
        /// 通过碎片数据索引编号获取碎片文件路径。
        /// </summary>
        /// <param name="index">碎片数据索引编号</param>
        /// <returns>碎片文件路径</returns>
        public abstract string GetFragmentFilePath(int index);

        /// <summary>
        /// 获取碎片文件数量
        /// </summary>
        /// <returns>文件数量</returns>
        public abstract int GetFragmentCount();
    }

    /// <summary>
    /// 整形升序目录
    /// </summary>
    public class IntKeyAscendingOrderMenuManager : TableMenuManager<int>
    {
        /// <summary> 最大键值 </summary>
        private readonly int[] _maxKeyArray;
        
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="menuInfo">目录详细信息</param>
        public IntKeyAscendingOrderMenuManager(string menuInfo)
        {
            // 分隔符等级 ： | 
            // Menu data: MaxKey | MaxKey | MaxKey
            _maxKeyArray = Array.ConvertAll(menuInfo.Split("|"), int.Parse);
        }

        /// <summary>
        /// 通过配置键获取所在碎片文件编号
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>碎片文件编号</returns>
        public override int GetFragmentIndex(int key)
        {
            for (int i = 0; i < _maxKeyArray.Length; i++)
            {
                if (key <= _maxKeyArray[i]) return i;
            }

            return -1;
        }

        /// <summary>
        /// 通过碎片文件编号获取文件路径
        /// </summary>
        /// <param name="index">碎片文件编号</param>
        /// <returns>文件路径</returns>
        public override string GetFragmentFilePath(int index)
        {
            return _maxKeyArray.CheckIndexLegal(index) ? _maxKeyArray[index].ToString() : string.Empty;
        }

        /// <summary>
        /// 获取碎片文件数量
        /// </summary>
        /// <returns>文件数量</returns>
        public override int GetFragmentCount()
        {
            return _maxKeyArray.Length;
        }
    }

    /// <summary>
    /// 全键映射目录
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    public class AllKeyMappingMenuManager<TKey> : TableMenuManager<TKey>
    {
        /// <summary> Key - 碎片文件索引映射 </summary>
        private readonly Dictionary<TKey, int> _menu;

        /// <summary> 碎片总数 </summary>
        private readonly int _fragmentCount;
        
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="menuInfo">目录详细信息</param>
        /// <param name="getKeyFunc">转换Key的方法</param>
        public AllKeyMappingMenuManager(string menuInfo, Func<string, TKey> getKeyFunc)
        {
            // 分隔符等级 ： | , _
            // Menu data: index,Key_Key_Key | index,Key_Key_Key | index,Key_Key_Key
            // 这种分配方式的原因是Key,可能存在多键和自定义方式。
            var menuEntries = menuInfo.Split("|");
            _menu = new Dictionary<TKey, int>();
            _fragmentCount = menuEntries.Length;
            for (var i = 0; i < menuEntries.Length; i++)
            {
                var pk = menuEntries[i].Split(",");
                
                var index = int.Parse(pk[0]);
                var keyArray = pk[1].Split("_");
                foreach (var key in keyArray)
                {
                    var formatKey = getKeyFunc.Invoke(key);
                    _menu.Add(formatKey, index);
                }
            }
        }

        /// <summary>
        /// 通过配置键获取所在碎片文件编号
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>碎片文件编号</returns>
        public override int GetFragmentIndex(TKey key)
        {
            if (_menu.TryGetValue(key, out var value)) return value;
            return -1;
        }

        /// <summary>
        /// 通过碎片文件编号获取文件路径
        /// </summary>
        /// <param name="index">碎片文件编号</param>
        /// <returns>文件路径</returns>
        public override string GetFragmentFilePath(int index)
        {
            if (index < 0) return string.Empty;
            return index.ToString();
        }

        /// <summary>
        /// 获取碎片文件数量
        /// </summary>
        /// <returns>文件数量</returns>
        public override int GetFragmentCount()
        {
            return _fragmentCount;
        }
    }


    /// <summary>
    /// 碎片数据管理器
    /// </summary>
    /// <typeparam name="TKey">存储Key类型</typeparam>
    /// <typeparam name="TEntry">存储Value类型</typeparam>
    public class TableFragmentManager<TKey, TEntry> where TEntry : class, new()
    {
        /// <summary> 在数组中的索引号 </summary>
        public int Index;
        
        /// <summary> 引用次数 </summary>
        private int _referenceCount;

        /// <summary> 存储数据 </summary>
        private Dictionary<TKey, TEntry> _key2Entry;

        /// <summary> 通过字符串解析成数据类 </summary>
        private readonly Func<string, TEntry> _analysisEntry;

        /// <summary> 通过数据类获取存储键 </summary>
        private readonly Func<TEntry, TKey> _analysisKey;

        /// <summary>
        /// 构造方法
        /// </summary>
        public TableFragmentManager(Func<string, TEntry> analysisEntry, Func<TEntry, TKey> analysisKey)
        {
            _referenceCount = 0;
            _key2Entry = new Dictionary<TKey, TEntry>();
            _analysisEntry = analysisEntry;
            _analysisKey = analysisKey;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="nullLogError">未获取成功打印错误</param>
        /// <returns>配置条目</returns>
        public TEntry GetEntry(TKey key, bool nullLogError = false)
        {
            if (!_key2Entry.TryGetValue(key, out var value) && nullLogError)
            {
                $"Get entry failed. Key: {key}.".Error();
            }

            return value;
        }

        /// <summary>
        /// 尝试获取配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="entry">配置条目</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetEntry(TKey key, out TEntry entry)
        {
            return _key2Entry.TryGetValue(key, out entry);
        }

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="str">数据字符串</param>
        public void AnalysisEntry(string str)
        {
            var dataArray = str.Split("\r\n");
            foreach (var data in dataArray)
            {
                var entry = _analysisEntry.Invoke(data);
                var key = _analysisKey.Invoke(entry);
                _key2Entry.Add(key, entry);
            }
        }

        /// <summary>
        /// 开始使用的时候调用一下
        /// </summary>
        public void StartUse()
        {
            _referenceCount++;
        }

        /// <summary>
        /// 结束使用的时候调用一下
        /// </summary>
        public void FinishUse()
        {
            _referenceCount--;
        }

        /// <summary>
        /// 尝试释放内存
        /// </summary>
        /// <returns>是否释放成功</returns>
        public bool TryRelease()
        {
            if (_referenceCount == 0)
            {
                _key2Entry.Clear();
                _key2Entry = null;
                return true;
            }

            return false;
        }
    }
}