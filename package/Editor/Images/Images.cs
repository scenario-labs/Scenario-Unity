using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class Images : EditorWindow
    {
        private static readonly ImagesUI ImagesUI = new();
    
        internal static List<ImageDataStorage.ImageData> _imageDataList = new();

        [MenuItem("Window/Scenario/Images")]
        public static void ShowWindow()
        {
            GetInferencesData();
        
            var images = (Images)GetWindow(typeof(Images));
            ImagesUI.Init(images);
        }

        private void OnGUI()
        {
            ImagesUI.OnGUI(this.position);
        }
    
        private void OnDestroy()
        {
            ImagesUI.CloseSelectedTextureSection();
        }
    
        private static void GetInferencesData() //why get inferences instead of getting the assets ??
        {
            ApiClient.RestGet($"inferences", response =>
            {
                var inferencesResponse = JsonConvert.DeserializeObject<InferencesResponse>(response.Content);

                if (inferencesResponse.inferences[0].status == "failed")
                {
                    Debug.LogError("Api Response: Status == failed, Try Again..");
                }

                _imageDataList.Clear();
                ImagesUI.textures.Clear();
                
                foreach (var inference in inferencesResponse.inferences)
                {
                    foreach (var image in inference.images)
                    {
                        _imageDataList.Add(new ImageDataStorage.ImageData
                        {
                            Id = image.id,
                            Url = image.url,
                            InferenceId = inference.id,
                            Prompt = inference.parameters.prompt,
                            Steps = inference.parameters.numInferenceSteps,
                            Size = new Vector2(inference.parameters.width,inference.parameters.height),
                            Guidance = inference.parameters.guidance,
                            Scheduler = "Default", //TODO : change this to reflect the scheduler used for creating this image
                            Seed = image.seed,
                            CreatedAt = inference.createdAt,
                        });
                    }
                }

                FetchPageTextures();
            });
        }

        private static void FetchPageTextures()
        {
            List<ImageDataStorage.ImageData> images = Images._imageDataList.OrderByDescending(x => x.CreatedAt).ToList();
            var tempTextures = new Texture2D[images.Count];
            int loadedCount = 0;

            for (int i = 0; i < images.Count; i++)
            {
                int index = i;
                CommonUtils.FetchTextureFromURL(images[index].Url, texture =>
                {
                    tempTextures[index] = texture;
                    loadedCount++;

                    if (loadedCount == images.Count)
                    {
                        ImagesUI.textures.Clear();
                        ImagesUI.textures.AddRange(tempTextures);
                    }
                });
            }
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

        /// <summary>
        /// Find the selected texture according to the current user selection and call the API to remove its background
        /// </summary>
        /// <param name="selectedTextureIndex"></param>
        /// <param name="callback_OnBackgroundRemoved">Returns a callback with the byte array corresponding of the image (withouth background) data</param>
        internal void RemoveBackground(int selectedTextureIndex, Action<byte[]> callback_OnBackgroundRemoved)
        {
            BackgroundRemoval.RemoveBackground(ImagesUI.textures[selectedTextureIndex], imageBytes =>
            {
                callback_OnBackgroundRemoved?.Invoke(imageBytes);
            });
        }

        /// <summary>
        /// When Unity reset the GUI (after compiling for example), the link between this script and the GUI can be broken, so I update it
        /// </summary>
        private void OnValidate()
        {
            ShowWindow();
        }

    }
}
