using System.Collections;
using System.Collections.Generic;
using Bighead.Csv;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CsvAssistant.Step();
        var config = CsvAssistant.GetResourcesCsv().GetRowByKey(1);
        Debug.LogError($"{config.Desc} : {config.Path}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
