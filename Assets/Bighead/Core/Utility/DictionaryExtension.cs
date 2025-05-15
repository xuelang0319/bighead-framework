//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2020年12月17日  |   字典扩展方法
//

using System.Collections.Generic;
using framework_bighead.Utility;

namespace Bighead.Core.Utility
{
    public static class DictionaryExtension
    {
        public static bool AddValue<T, T1>(this Dictionary<T,T1> dict,T key, T1 value)
        {
            if (dict.ContainsKey(key)) return false;
            dict.Add(key,value);
            return true;
        }

        public static void AddValueAndLogError<T, T1>(this Dictionary<T, T1> dict, T key, T1 value)
        {
            if(!dict.AddValue(key, value)) "字典存储失败，存在相同的键： {key}, 请检查。".Error();
        }
        
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }

            return defaultValue;
        }

        public static T1 GetValue<T, T1>(this Dictionary<T, T1> dict, T key) where T1 : class, new()
        {
            return !dict.ContainsKey(key) ? null : dict[key];
        }

        public static T1 GetValueLogError<T, T1>(this Dictionary<T, T1> dict, T key) where T1 : class, new()
        {
            var value = dict.GetValue(key);
            if(value == null) $"字典中未找到对应键 {key} 的值， 请检查。".Error();
            return value;
        }
        
        public static bool AddValueToList<T, T1>(this Dictionary<T, List<T1>> dict, T key, T1 value, bool allowSameValue = false)
        {
            if(dict.ContainsKey(key)) dict[key] = new List<T1>();
            var list = dict[key];
            if (!allowSameValue && list.Contains(value)) return false;
            list.Add(value);
            return true;
        }

        public static void AddValueToListLogError<T, T1>(this Dictionary<T, List<T1>> dict, T key, T1 value,bool allowSameValue = false)
        {
            if(!dict.AddValueToList(key, value, allowSameValue)) $"字典存储失败，存在相同的值： {value}, 请检查。".Error();
        }

        public static void AddValueToListAndMerge<T, T1>(this Dictionary<T, List<T1>> dict, T key, List<T1> value)
        {
            if (!dict.ContainsKey(key)) dict.Add(key,value);
            else dict[key].Merge(value);
            
        }
    }
}