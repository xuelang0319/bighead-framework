using System;
using System.IO;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Test : MonoBehaviour
{
    private async void Awake()
    {
        await Main();
        Debug.LogError($"Execute Finished");
    }

    private async UniTask Main()
    {
        string url = "http://120.26.192.253:8080/test.txt";
        string savePath = "C:/Temp/test.txt";

        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
               Debug.LogError("[Client] File not found (204 No Content)");
                return;
            }

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();

            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await File.WriteAllBytesAsync(savePath, data);

            Debug.LogError($"[Client] Downloaded file saved to {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Client] Download failed: {ex.Message}");
        }
    }
}
