using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Networking;
using RestSharp;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor.TerrainTools;
using System;
using UnityEditor.PackageManager.Requests;
using System.IO;
using Newtonsoft.Json;
using System.Text;

public class PromptImages : EditorWindow
{
    public static List<ImageDataStorage.ImageData> imageDataList = ImageDataStorage.imageDataList;
    public static PromptImagesUI promptImagesUI = new PromptImagesUI();
    
    public static string downloadPath;


    [MenuItem("Window/Scenario/Prompt Images")]
    public static void ShowWindow()
    {
        FetchGeneratedImages();
        
        var promptImages = (PromptImages) EditorWindow.GetWindow(typeof(PromptImages));

        promptImagesUI.Init(promptImages);

        downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
    }

    public void DeleteImageAtIndex(int selectedTextureIndex)
    {
        promptImagesUI.textures.RemoveAt(selectedTextureIndex);

        string imageId = imageDataList[selectedTextureIndex].Id;
        string modelId = EditorPrefs.GetString("SelectedModelId", "");
        string inferenceId = imageDataList[selectedTextureIndex].InferenceId;
        EditorCoroutineUtility.StartCoroutineOwnerless(DeleteImageRequest(inferenceId, modelId, imageId));

        Repaint();
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

    private async static Task<Texture2D> LoadTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            www.SendWebRequest();
            while (!www.isDone)
            {
                await Task.Delay(10);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                return null;
            }

            return DownloadHandlerTexture.GetContent(www);
        }
    }

    private void OnGUI()
    {
        promptImagesUI.OnGUI(this.position);
    }

    private static async void FetchGeneratedImages()
    {
        Debug.Log("Fetching new images, please wait..");

        int oldImageCount = PromptWindow.generatedImagesData.Count;
        int newImageCount = 0;

        foreach (var image in PromptWindow.generatedImagesData)
        {
            if (!imageDataList.Exists(x => x.Id == image.Id))
            {
                ImageDataStorage.ImageData newImageData = new ImageDataStorage.ImageData { Id = image.Id, Url = image.Url, InferenceId = image.InferenceId };
                imageDataList.Insert(0, newImageData);

                Texture2D texture = await LoadTexture(image.Url);
                promptImagesUI.textures.Insert(0, texture);

                newImageCount++;
            }
        }

        Debug.Log("Retrieved " + newImageCount + " new images. Total images: " + imageDataList.Count + ".");

        promptImagesUI?.promptImages?.Repaint();
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

    private void OnDestroy()
    {
        ClearData();
        promptImagesUI.selectedTexture = null;
        promptImagesUI.selectedImageId = null;
    }

    private void OnLostFocus()
    {
        promptImagesUI.selectedTexture = null;
        promptImagesUI.selectedImageId = null;
    }

    private void ClearData()
    {
        imageDataList.Clear();
        promptImagesUI.textures.Clear();
    }

    internal void RemoveBackground(int selectedTextureIndex)
    {
        var imgBytes = promptImagesUI.textures[selectedTextureIndex].EncodeToPNG();
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
        request.AddParameter("application/json",
           param, ParameterType.RequestBody);

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
}