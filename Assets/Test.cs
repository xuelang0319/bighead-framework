using System;
using System.IO;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogError($"{nameof(Test)} Awake");
        Step();
        /*await Main();
        Debug.LogError($"Execute Finished");*/
    }

    private async void Step()
    {
        await Run();
        Debug.Log("Addressables 初始化完成");

        // Step 2: 更新Catalog（可选）
        await UpdateCatalog();
        Debug.Log("Catalog 更新完成");

        // Step 3: 加载关键资源或场景
        var handle = Addressables.InstantiateAsync("Assets/Test/Cube.prefab");
        handle.WaitForCompletion();
        handle = Addressables.InstantiateAsync("Assets/Test/GameObject.prefab");
        handle.WaitForCompletion();
        Debug.Log($"Instantiated: {handle.Result.name}");
    }

    private async UniTask Run()
    {
        // Step 1: 初始化 Addressables
        var initHandle = Addressables.InitializeAsync();
        await initHandle.Task;
    }

    private async UniTask UpdateCatalog()
    {
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        var catalogs = await checkHandle.Task;
        if (catalogs.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(catalogs);
            await updateHandle.Task;
        }
    }

    /*private async UniTask Main()
    {
        string url = "http://120.26.192.253:8080/StandaloneWindows64/catalog_0.1.json";
        string savePath = "C:/Temp/catalog_0.1.json";

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
    }*/
}
