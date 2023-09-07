using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class Models : EditorWindow
{
    public static List<string> loadedModels = new();
    public static List<ModelData> models = new();
    public static readonly string PaginationTokenKey = "paginationToken";

    private static readonly float MinimumWidth = 1000f;
    private static ModelsUI modelsUI = new();

    public static string paginationToken = "";
    private static string privacy = "private";

    [MenuItem("Window/Scenario/Models")]
    public static void ShowWindow()
    {
        SetTab("private");
        
        Models window = GetWindow<Models>("Models");
        window.minSize = new Vector2(MinimumWidth, window.minSize.y);
    }

    public static void SetTab(string privacySetting)
    {
        privacy = privacySetting;
        GetModelsData(0);
    }

    private void OnDestroy()
    {
        modelsUI.ResetTabSelection();
        modelsUI.ClearData();
    }

    internal static void GetModelsData(int updateType = 0,Action onSuccess = null)
    {
        models.Clear();
        
        FetchModelPage(updateType, () =>
        {
            switch (updateType)
            {
                case 0:
                    modelsUI.SetFirstPage();
                    break;
                case 1:
                    modelsUI.SetNextPage();
                    break;
                case -1:
                    modelsUI.SetPreviousPage();
                    break;
            }

            modelsUI.UpdatePage();
            
            EditorWindow.GetWindow(typeof(Models)).Repaint();
            
            onSuccess?.Invoke();
        });
    }

    private static void FetchModelPage(int updateType, Action onSuccess)
    {
        string endpoint = $"models?pageSize=15&status=trained&privacy={privacy}";

        if (!string.IsNullOrEmpty(paginationToken) && updateType != 0)
        {
            endpoint += $"&paginationToken={paginationToken}";
        }

        ApiClient.RestGet(endpoint, response =>
        {
            var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response.Content);
            models.AddRange(modelsResponse.models);

            if (modelsResponse.nextPaginationToken is null ||
                paginationToken == modelsResponse.nextPaginationToken)
            {
                paginationToken = "";
            }
            else
            {
                paginationToken = modelsResponse.nextPaginationToken;
                Debug.Log("fetching next page data...");
                EditorPrefs.SetString(PaginationTokenKey, paginationToken);
                FetchModelPage(updateType, null);
            }
            
            onSuccess?.Invoke();
        }, error =>
        {
            Debug.Log("stop fetching data.");
        });
    }

    private void OnGUI()
    {
        modelsUI.OnGUI(this.position);
    }
    
    #region API_DTO

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

    #endregion
}