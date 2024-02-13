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
    
        internal static List<ImageDataStorage.ImageData> imageDataList = new();

        /// <summary>
        /// Contains a token that is useful to get the next page of inferences
        /// </summary>
        private static string lastPageToken = string.Empty;

        [MenuItem("Window/Scenario/Images")]
        public static void ShowWindow()
        {
            lastPageToken = string.Empty;
            imageDataList.Clear();
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
    
        public static void GetInferencesData(Action callback_OnDataGet = null) //why get inferences instead of getting the assets ??
        {
            string request = $"inferences";
            if (!string.IsNullOrEmpty(lastPageToken))
                request = $"inferences?paginationToken={lastPageToken}";

            ApiClient.RestGet(request, response =>
            {
                var inferencesResponse = JsonConvert.DeserializeObject<InferencesResponse>(response.Content);

                lastPageToken = inferencesResponse.nextPaginationToken;

                if (inferencesResponse.inferences[0].status == "failed")
                {
                    Debug.LogError("Api Response: Status == failed, Try Again..");
                }

                List<ImageDataStorage.ImageData> imageDataDownloaded = new List<ImageDataStorage.ImageData>();
                
                foreach (var inference in inferencesResponse.inferences)
                {
                    foreach (var image in inference.images)
                    {
                        imageDataDownloaded.Add(new ImageDataStorage.ImageData
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

                imageDataList.AddRange(imageDataDownloaded);
                foreach (ImageDataStorage.ImageData imageData in imageDataList)
                {
                    FetchTextureFor(imageData);
                }
            });
        }

        /// <summary>
        /// Fetch a texture for a specific ImageData
        /// </summary>
        private static void FetchTextureFor(ImageDataStorage.ImageData _image, Action callback_OnTextureGet = null)
        {
            CommonUtils.FetchTextureFromURL(_image.Url, texture =>
            {
                _image.texture = texture;
                callback_OnTextureGet?.Invoke();
            });
        }

        public void DeleteImageAtIndex(int selectedTextureIndex)
        {
            string imageId = imageDataList[selectedTextureIndex].Id;
            string modelId = EditorPrefs.GetString("SelectedModelId", "");
            string inferenceId = imageDataList[selectedTextureIndex].InferenceId;

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
        }

    }
}
