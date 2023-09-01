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
        ShowWindow("private");
        
        Models window = GetWindow<Models>("Models");
        window.minSize = new Vector2(MinimumWidth, window.minSize.y);
    }

    public static void ShowWindow(string privacySetting)
    {
        privacy = privacySetting;
        GetModelsData(0);
        GetPaginationToken(null);
        EditorWindow.GetWindow(typeof(Models));
    }

    private void OnDestroy()
    {
        modelsUI.ResetTabSelection();
        modelsUI.ClearData();
    }

    internal static void GetModelsData(int updateType = 0)
    {
        models.Clear();

        bool continueFetching = true;
        while (continueFetching)
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
                    continueFetching = false;
                }
                else
                {
                    paginationToken = modelsResponse.nextPaginationToken;
                    Debug.Log("fetching data...");
                }
            }, error =>
            {
                Debug.Log("stop fetching data.");
                continueFetching = false;
            });
        }

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
    }

    private static void GetPaginationToken(Action<string> token)
    {
        ApiClient.RestGet("token", response =>
        {
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
            paginationToken = tokenResponse.nextPaginationToken;
            EditorPrefs.SetString(PaginationTokenKey, paginationToken);
            token?.Invoke(paginationToken);
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