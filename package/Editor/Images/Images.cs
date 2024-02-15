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

        private static bool isVisible = false;

        [MenuItem("Window/Scenario/Images")]
        public static void ShowWindow()
        {
            if (isVisible)
                return;

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
            ImagesUI.OnClose();
            ImagesUI.CloseSelectedTextureSection();
            DataCache.instance.ClearAllImageData();
        }

        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
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
                            modelId = inference.modelId
                        });
                    }
                }

                imageDataList.AddRange(imageDataDownloaded);
                foreach (ImageDataStorage.ImageData imageData in imageDataList)
                {
                    FetchTextureFor(imageData);
                }
                callback_OnDataGet?.Invoke();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_id">The id of the image you want to delete</param>
        public void DeleteImage(string _id)
        {
            var imageData = Images.GetImageDataById(_id); //try to get image from Images
            if (imageData == null)
                imageData = DataCache.instance.GetImageDataById(_id); //try to get from Datacache (if it has just been prompted)
            if (imageData == null)
                return;

            string url = $"models/{imageData.modelId}/inferences/{imageData.InferenceId}/images/{imageData.Id}";
            ApiClient.RestDelete(url,null);
            imageDataList.Remove(imageData);

            if(DataCache.instance.DoesImageIdExist(_id)) //also delete from Datacache if it's there
                DataCache.instance.RemoveImageDataById(_id);

            Repaint();
        }

        public static ImageDataStorage.ImageData GetImageDataById(string _id)
        {
            var imageData = imageDataList.Find(x => x.Id == _id);
            if (imageData == null)
                imageData = DataCache.instance.GetImageDataById(_id); //try to get from Datacache (if it has just been prompted)
            if (imageData == null)
                return null;

            return imageData;
        }

        public static Texture2D GetTextureByImageId(string _id)
        {
            var imageData = Images.GetImageDataById(_id); //try to get image from Images
            if (imageData == null)
                imageData = DataCache.instance.GetImageDataById(_id); //try to get from Datacache (if it has just been prompted)
            if (imageData == null)
                return null;

            return imageData.texture;
        }

    }
}
