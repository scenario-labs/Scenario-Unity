using System;
using System.Collections;
using System.Threading.Tasks;
using RestSharp;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Scenario
{
    public class PromptImages : EditorWindow
    {
        public static PromptImagesUI promptImagesUI = new();
        public static string downloadPath;

        [MenuItem("Window/Scenario/Prompt Images")]
        public static void ShowWindow()
        {
            UpdateImages();

            var promptImages = (PromptImages)GetWindow(typeof(PromptImages));
            promptImagesUI.Init(promptImages);

            downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
        }

        public void DeleteImageAtIndex(int selectedTextureIndex)
        {
            var imgData = DataCache.instance.GetImageDataAtIndex(selectedTextureIndex);

            string imageId = imgData.Id;
            string modelId = DataCache.instance.SelectedModelId;
            string inferenceId = imgData.InferenceId;
            EditorCoroutineUtility.StartCoroutineOwnerless(DeleteImageRequest(inferenceId, modelId, imageId));

            Repaint();
        }

        public void CloseSelectedTextureSection()
        {
            //ClearData();
            promptImagesUI.selectedTexture = null;
            promptImagesUI.selectedImageId = null;
        }

        private static async void LoadTexture(string url, Action<Texture2D> result)
        {
            using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

            www.SendWebRequest();

            while (!www.isDone)
            {
                await Task.Delay(10);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error + $"\n{url}");
                result(null);
            }

            result(DownloadHandlerTexture.GetContent(www));
        }

        private void OnGUI()
        {
            promptImagesUI.OnGUI(this.position);
        }

        private static void UpdateImages()
        {
            for (int i = DataCache.instance.GetImageDataCount() - 1; i >= 0; i--)
            {
                var imageData = DataCache.instance.GetImageDataAtIndex(i);

                if (imageData.Url != null && imageData.Url.Length > 10 && imageData.texture == null)
                {
                    LoadTexture(imageData.Url, result =>
                    {
                        imageData.texture = result;

                        if (promptImagesUI != null)
                        {
                            if (promptImagesUI.promptImages != null)
                            {
                                promptImagesUI.promptImages.Repaint();
                            }
                        }
                    });
                }
            }

            if (promptImagesUI != null)
            {
                if (promptImagesUI.promptImages != null)
                {
                    promptImagesUI.promptImages.Repaint();
                }
            }
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

        /// <summary>
        /// When Unity reset the GUI (after compiling for example), the link between this script and the GUI can be broken, so I update it
        /// </summary>
        private void OnValidate()
        {
            ShowWindow();
        }

        private void OnDestroy()
        {
            CloseSelectedTextureSection();
        }

        /*private void ClearData()
        {
            DataCache.instance.ClearAllImageData();
        }*/

        internal void RemoveBackground(int selectedTextureIndex)
        {
            BackgroundRemoval.RemoveBackground(DataCache.instance.GetImageDataAtIndex(selectedTextureIndex).texture, bytes =>
            {
                string fileName = CommonUtils.GetRandomImageFileName();
                CommonUtils.SaveImageBytesToFile(fileName, bytes);
            });
        }
    }
}