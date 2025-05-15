using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bighead.Core.Utility;

namespace Bighead.Csv
{
    public static partial class CsvAssistant
    { 
        private static Dictionary<string, CsvBase> _csvBasics = new Dictionary<string, CsvBase>();

        public static T GetCsv<T>(string name) where T: CsvBase, new()
        {
            _csvBasics.TryGetValue(name, out var value);
            if (Equals(null, value))
            {
                value = new T();
                _csvBasics.Add(name, value);
            }

            return value as T;
        }

        public static void RegisterCsv(string key, CsvBase basicCsv)
        {
            if (_csvBasics.ContainsKey(key))
                return;
        
            _csvBasics[key] = basicCsv;
        }
        
        public static void Step()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            
            var array = typeof(CsvBase).CreateAllDerivedClass<CsvBase>().ToList();
            if (array.Count == 0)
            {
                "没有检测到任何Csv配置表，请检查。".Exception();
            }

            var plusArray = array.Where(csv => csv.GetType().Name.EndsWith("Plus")).ToArray();
            var dictionary = new Dictionary<string, CsvBase>();
            for (int i = 0; i < plusArray.Length; i++)
            {
                var item = plusArray[i];
                var name = item.GetType().Name;
                name = name.Substring(0, name.Length - 4);
                dictionary.Add(name, item);
                array.Remove(item);
            }

            for (var i = 0; i < array.Count; i++)
            {
                var item = array[i];
                var name = item.GetType().Name;
                if (dictionary.ContainsKey(name)) continue;
                dictionary.Add(name, item);
            }

            foreach (var kv in dictionary)
            {
                RegisterCsv(kv.Key, kv.Value);
                kv.Value.Initialize();
            }
        }
    }
}