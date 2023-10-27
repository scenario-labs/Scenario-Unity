using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class Images : EditorWindow
    {
        private static readonly string PaginationTokenKey = "paginationToken";
        private static readonly float MinimumWidth = 1000f;
        private static readonly float MinimumHeight = 700f;
        private static readonly ImagesUI ImagesUI = new();
    
        private static string _paginationToken = "";
        private static List<ImageDataStorage.ImageData> _imageDataList = ImageDataStorage.imageDataList;

        [MenuItem("Window/Scenario/Images")]
        public static void ShowWindow()
        {
            //Debug.Log(PluginSettings.EncodedAuth);

            Images window = GetWindow<Images>("Images");
        
            GetInferencesData();
        
            var images = EditorWindow.GetWindow(typeof(Images));
            ImagesUI.Init((Images)images);

            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
            window.minSize = new Vector2(window.minSize.x, MinimumHeight);
        }

        private void OnGUI()
        {
            ImagesUI.OnGUI(this.position);
        }
    
        private void OnDestroy()
        {
            ImagesUI.ClearSelectedTexture();
        }
    
        private static void GetInferencesData()
        {
            string selectedModelId = EditorPrefs.GetString("SelectedModelId");
            if (selectedModelId.Length < 2)
            {
                Debug.LogError("Please select a model first.");
                return;
            }

            string paginationTokenString = EditorPrefs.GetString(PaginationTokenKey,"");
            if (paginationTokenString != "")
            {
                paginationTokenString = "&paginationToken=" + paginationTokenString;
            }

            ApiClient.RestGet($"inferences?nextPaginationToken={_paginationToken}", response =>
            {
                var inferencesResponse = JsonConvert.DeserializeObject<InferencesResponse>(response.Content);
                _paginationToken = inferencesResponse.nextPaginationToken;
                
                ImageDataStorage.imageDataList.Clear();
                ImagesUI.textures.Clear();
                
                foreach (var inference in inferencesResponse.inferences)
                {
                    foreach (var image in inference.images)
                    {
                        ImageDataStorage.imageDataList.Add(new ImageDataStorage.ImageData
                        {
                            Id = image.id,
                            Url = image.url,
                            InferenceId = inference.id,
                            Prompt = inference.parameters.prompt,
                            Steps = inference.parameters.numInferenceSteps,
                            Size = new Vector2(inference.parameters.width,inference.parameters.height),
                            Guidance = inference.parameters.guidance,
                            Scheduler = "Default",
                            Seed = image.seed,
                        });
                    }
                }

                ImagesUI.SetFirstPage();
                ImagesUI.UpdatePage();
            });
        }
    
        public void DeleteImageAtIndex(int selectedTextureIndex)
        {
            ImagesUI.textures.RemoveAt(selectedTextureIndex);

            string imageId = _imageDataList[selectedTextureIndex].Id;
            string modelId = EditorPrefs.GetString("SelectedModelId", "");
            string inferenceId = _imageDataList[selectedTextureIndex].InferenceId;

            Debug.Log("Requesting image deletion please wait..");
        
            string url = $"models/{modelId}/inferences/{inferenceId}/images/{imageId}";
        
            ApiClient.RestDelete(url,null);

            Repaint();
        }

        internal void RemoveBackgroundForImageAtIndex(int selectedTextureIndex)
        {
            string dataUrl = CommonUtils.Texture2DToDataURL(ImagesUI.textures[selectedTextureIndex]);
            string fileName =CommonUtils.GetRandomImageFileName();
            string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{fileName}\",\"format\":\"png\",\"returnImage\":\"false\"}}";

            Debug.Log($"Requesting background removal, please wait..");

            ApiClient.RestPut("images/erase-background",param, response =>
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

                        CommonUtils.FetchTextureFromURL(imageUrl, texture =>
                        {
                            CommonUtils.SaveTextureAsPNG(texture);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("An error occurred while processing the response: " + ex.Message);
                    }
                }
            });
        }
    }
}
