using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public class Models : EditorWindow
{
    public static List<string> loadedModels = new List<string>();
    public static List<ModelData> models = new List<ModelData>();

    private static readonly string apiEndpoint = "/models";
    private static readonly string tokenEndpoint = "/token";
    public static readonly string paginationTokenKey = "paginationToken";

    public static string paginationToken = "";
    private static string privacy = "private";

    private static float minimumWidth = 1000f;

    [MenuItem("Window/Scenario/Models")]
    public static void ShowWindow()
    {
        ShowWindow("private");
        
        Models window = GetWindow<Models>("Models");
        window.minSize = new Vector2(minimumWidth, window.minSize.y);
    }

    public static async void ShowWindow(string privacySetting)
    {
        privacy = privacySetting;
        await GetModelsData(0);
        await GetPaginationToken();
        EditorWindow.GetWindow(typeof(Models));
    }

    private void OnDestroy()
    {
        modelsUI.ResetTabSelection();
        modelsUI.ClearData();
    }

    internal static async Task GetModelsData(int updateType = 0)
    {
        models.Clear();

        bool continueFetching = true;
        while (continueFetching)
        {
            string endpoint = $"{apiEndpoint}?pageSize=15&status=trained&privacy={privacy}";

            if (!string.IsNullOrEmpty(paginationToken) && updateType != 0)
            {
                endpoint += $"&paginationToken={paginationToken}";
            }

            try
            {
                string response = await ApiClient.GetAsync(endpoint);
                if (response != null)
                {
                    var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response);
                    models.AddRange(modelsResponse.models);

                    if (modelsResponse.nextPaginationToken is null ||
                        paginationToken == modelsResponse.nextPaginationToken)
                    {
                        paginationToken = "";
                        continueFetching = false;
                    }
                    else
                    {
                        paginationToken = modelsResponse.nextPaginationToken;
                        Debug.Log("fetching data...");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                continueFetching = false;
            }
        }

        if (updateType == 0)
        {
            modelsUI.SetFirstPage();
        }
        else if (updateType == 1)
        {
            modelsUI.SetNextPage();
        }
        else if (updateType == -1)
        {
            modelsUI.SetPreviousPage();
        }

        await modelsUI.UpdatePage();
        EditorWindow.GetWindow(typeof(Models)).Repaint();
    }


    public async static UniTask<Texture2D> LoadTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            DownloadHandlerTexture downloadHandlerTexture = new DownloadHandlerTexture(true);
            www.downloadHandler = downloadHandlerTexture;
            await www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                return null;
            }
            return downloadHandlerTexture.texture;
        }
    }

    private static async Task GetPaginationToken()
    {
        try
        {
            string response = await ApiClient.GetAsync(tokenEndpoint);
            if (response != null)
            {
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response);
                paginationToken = tokenResponse.nextPaginationToken;
                EditorPrefs.SetString(paginationTokenKey, paginationToken);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private void OnGUI()
    {
        modelsUI.OnGUI(this.position);
    }

    private static List<ImageData> imageDataList = new List<ImageData>();
    private static ModelsUI modelsUI = new ModelsUI();

    private class ImageData
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }

    private class ModelsResponse
    {
        public List<ModelData> models { get; set; }
        public string nextPaginationToken { get; set; }
    }

    public class ModelData
    {
        public string id { get; set; }
        public string name { get; set; }
        public ClassData classData { get; set; }
        public string privacy { get; set; }
        public string status { get; set; }
        public List<TrainingImageData> trainingImages { get; set; }
        public ThumbnailData thumbnail { get; set; } // Assuming you have a ThumbnailData class.
    }

    public class ClassData
    {
        public string modelId { get; set; }
    }

    public class TrainingImageData
    {
        public string downloadUrl { get; set; }
    }

    public class ThumbnailData
    {
        public string url { get; set; }
    }

    private class TokenResponse
    {
        public string nextPaginationToken { get; set; }
    }
}