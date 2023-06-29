using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System.IO;
using RestSharp;

public class Images : EditorWindow
{
    private static readonly string apiEndpoint = "/inferences";
    private static readonly string paginationTokenKey = "paginationToken";

    private static string paginationToken = "";
    private static string downloadPath = "";

    private static float minimumWidth = 1000f;
    private static float minimumHeight = 700f;

    [MenuItem("Window/Scenario/Images")]
    public static void ShowWindow()
    {
        Images window = GetWindow<Images>("Images");
        GetInferencesData();
        var images = EditorWindow.GetWindow(typeof(Images));
        imagesUI.Init((Images)images);

        downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
        
        window.minSize = new Vector2(minimumWidth, window.minSize.y);
        window.minSize = new Vector2(window.minSize.x, minimumHeight);
    }

    private void OnDestroy()
    {
        imagesUI.ClearSelectedTexture();
    }
    
    private static async void GetInferencesData()
    {
        try
        {
            string selectedModelId = EditorPrefs.GetString("SelectedModelId");
            if (selectedModelId.Length < 2)
            {
                Debug.LogError("Please select a model first.");
                return;
            }

            string paginationTokenString = EditorPrefs.GetString(paginationTokenKey,"");
            if (paginationTokenString != "")
            {
                paginationTokenString = "&paginationToken=" + paginationTokenString;
            }

            string response = await ApiClient.GetAsync($"{apiEndpoint}?nextPaginationToken={paginationToken}");
            if (response != null)
            {
                var inferencesResponse = JsonConvert.DeserializeObject<InferencesResponse>(response);
                paginationToken = inferencesResponse.nextPaginationToken;
                ImageDataStorage.imageDataList.Clear();
                imagesUI.textures.Clear();
                foreach (var inference in inferencesResponse.inferences)
                {
                    foreach (var image in inference.images)
                    {
                        ImageDataStorage.imageDataList.Add(new ImageDataStorage.ImageData { Id = image.id, Url = image.url, InferenceId = inference.id });
                    }
                }

                imagesUI.SetFirstPage();
                imagesUI.UpdatePage();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async static Task<Texture2D> LoadTexture(string url)
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

    public void DownloadImage(string fileName, byte[] pngBytes)
    {
        string filePath = downloadPath + "/" + fileName;
        File.WriteAllBytes(filePath, pngBytes);
        EditorCoroutineUtility.StartCoroutineOwnerless(RefreshDatabase());
        Debug.Log("Downloaded image to: " + filePath);
    }

    IEnumerator RefreshDatabase()
    {
        yield return null;
        AssetDatabase.Refresh();
    }


    public void DeleteImageAtIndex(int selectedTextureIndex)
    {
        imagesUI.textures.RemoveAt(selectedTextureIndex);

        string imageId = imageDataList[selectedTextureIndex].Id;
        string modelId = EditorPrefs.GetString("SelectedModelId", "");
        string inferenceId = imageDataList[selectedTextureIndex].InferenceId;
        EditorCoroutineUtility.StartCoroutineOwnerless(DeleteImageRequest(inferenceId, modelId, imageId));

        Repaint();
    }

    IEnumerator DeleteImageRequest(string inferenceId, string modelId, string imageId)
    {
        Debug.Log("Requesting image deletion please wait..");

        string url = $"{PluginSettings.ApiUrl}/models/{modelId}/inferences/{inferenceId}/images/{imageId}";
        Debug.Log(url);

        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest(Method.DELETE);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.Log($"Error: {response.ErrorException.Message}");
            }
            else
            {
                Debug.Log($"Response: {response.Content}");
            }
        });
    }

    internal void RemoveBackground(int selectedTextureIndex)
    {
        var imgBytes = imagesUI.textures[selectedTextureIndex].EncodeToPNG();
        string base64String = Convert.ToBase64String(imgBytes);
        string dataUrl = $"data:image/png;base64,{base64String}";
        EditorCoroutineUtility.StartCoroutineOwnerless(PutRemoveBackground(dataUrl));
    }

    IEnumerator PutRemoveBackground(string dataUrl)
    {
        string name = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";

        Debug.Log("Requesting background removal, please wait..");

        string url = $"{PluginSettings.ApiUrl}/images/erase-background";
        Debug.Log(url);

        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest(Method.PUT);

        string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{name}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";
        Debug.Log(param);

        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");
        request.AddParameter("application/json", param, ParameterType.RequestBody);

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.LogError($"Error: {response.ErrorException.Message}");
            }
            else
            {
                Debug.Log($"Response: {response.Content}");

                try
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                    string imageUrl = jsonResponse.asset.url;

                    EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImageFromUrl(imageUrl));
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred while processing the response: " + ex.Message);
                }
            }
        });
    }

    IEnumerator DownloadImageFromUrl(string imageUrl)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                byte[] pngBytes = texture.EncodeToPNG();
                
                string fileName = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
                DownloadImage(fileName, pngBytes);
            }
        }
    }


    private void OnGUI()
    {
        imagesUI.OnGUI(this.position);
    }

    internal static List<ImageDataStorage.ImageData> imageDataList = ImageDataStorage.imageDataList;
    private static ImagesUI imagesUI = new ImagesUI();
}