

using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogError($"{nameof(Test)} Awake");
        //StartCoroutine(DownloadFile());
        
        Step();
        /*await Main();
        Debug.LogError($"Execute Finished");*/

    } 
    
    private IEnumerator DownloadFile()
    {
        const string url = "http://120.26.192.253:8091/1.0.1/catalog_1.0.1.hash";
        Debug.Log("[FileDownloadTest] 开始请求: " + url);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[FileDownloadTest] 下载失败: " + www.error);
            }
            else
            {
                // 保存到本地
                Debug.Log("[FileDownloadTest] 下载成功, 文件大小: " + www.downloadHandler.data.Length + " bytes");
                Debug.Log("[FileDownloadTest] 文件内容: " + www.downloadHandler.text);
            }
        }
    }

    private async void Step()
    {
        Addressables.InternalIdTransformFunc = (location) =>
        {
            var original = location.InternalId;
            var url = original;

            // 替换规则
            if (original.Contains("BuildOutput"))
            {
                var port = 0;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                port = 8091;
#elif UNITY_ANDROID
                port = 8092;
#elif UNITY_IOS
                port = 8093;
#endif
                var replaceUrl = $"http://120.26.192.253:{port}/{Application.version}";
                url = original.Replace("BuildOutput", replaceUrl);
            }

            //Debug.LogError($"Original: {original}, Replace: {url}");
            return url;
        };
        
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

        var  material = Addressables.LoadAssetAsync<Material>(@"Assets/Test/New Material 1.mat").WaitForCompletion();
        Debug.Log($"Loaded: {material.name}");
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
