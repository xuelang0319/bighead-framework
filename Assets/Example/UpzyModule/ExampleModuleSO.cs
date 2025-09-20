using System.Collections.Generic;
using Bighead.Upzy.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "ExampleModuleConfig", menuName = "Bighead/Modules/Example Module Config")]
public class ExampleModuleSO : UpzyModuleSO
{
    [Header("示例配置")]
    public string message = "Hello Upzy!";
    public int dataValue = 42;
    
    public List<string> dataList = new List<string>();
}