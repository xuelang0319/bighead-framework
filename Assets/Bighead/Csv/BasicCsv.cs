//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月29日   |   Csv基类
//

using System.Collections.Generic;
using Bighead.Core;
using Bighead.Core.Utility;
using UnityEngine;

namespace Bighead.Csv
{
    public abstract class BasicCsv
    {
        protected abstract void AnalysisCsv(List<string> list);
        protected virtual string Path { get; set; }

        public void InitCsv()
        {
            var asset = Res.LoadAsset<TextAsset>(Path, "Csv").text;
            asset = BigheadCrypto.Base64Decode(asset);
            var list = CsvReader.ToListWithDeleteFirstLines(asset, 3);
            AnalysisCsv(list);
        }
    }
}