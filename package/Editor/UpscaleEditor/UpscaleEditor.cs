using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor.UpscaleEditor
{
    public class UpscaleEditor : EditorWindow
    {
        #region Public Fields
        #endregion

        #region Private Fields
        
        private static readonly float MinimumWidth = 1650f;
        private static readonly UpscaleEditorUI UpscaleEditorUI = new();

        #endregion

        #region MonoBehaviourCallback

        [MenuItem("Window/Scenario/Editors/Upscale Editor", false, 5)]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(UpscaleEditor), false, "Upscale Editor") as UpscaleEditor;
            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
        }

        public static void ShowWindow(Texture2D selectedTexture, ImageDataStorage.ImageData imageData)
        {
            UpscaleEditorUI.currentImage = selectedTexture;
            UpscaleEditorUI.imageData = imageData;
            ShowWindow();
        }

        private void OnGUI()
        {
            UpscaleEditorUI.OnGUI(this.position);
            UpscaleEditorUI.UpscaleEditor = this;
        }

        private void OnDestroy()
        {
            UpscaleEditorUI.currentImage = null;
        }

        #endregion

        #region Public Methods

        public void LaunchProgressUpscale(string _jobId, Action<string> _answer)
        {
            if (!string.IsNullOrEmpty(_jobId))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GetProgressUpscale(_jobId, _answer));
            }
        }

        #endregion

        #region Private Methods

        IEnumerator GetProgressUpscale(string _jobId, Action<string> _response)
        {
            bool inProgress = true;

            while (inProgress)
            {
                if (inProgress)
                {
                    ApiClient.RestGet($"jobs/{_jobId}", response =>
                    {
                        var progressResponse = JsonConvert.DeserializeObject<Root>(response.Content);

                        if (progressResponse != null)
                        {
                            if (!string.IsNullOrEmpty(progressResponse.job.status))
                            {
                                if (!progressResponse.job.status.Equals("success"))
                                {
                                    switch (progressResponse.job.status)
                                    {
                                        case "warming-up":
                                            Debug.Log("Upscale in preparation... wait...");
                                            break;

                                        case "queue":
                                            Debug.Log("Upscale in queue... wait ...");
                                            break;

                                        case "in-progress":
                                            Debug.Log("Upscale in progress... wait...");
                                            break;

                                        default:
                                            Debug.Log("Upscale... wait...");
                                            break;
                                    }
                                    inProgress = true;
                                }
                                else
                                {
                                    Debug.Log("Upscale progress done: " + response.Content);
                                    inProgress = false;
                                    _response?.Invoke(response.Content);
                                    return;
                                }
                            }
                        }
                    });
                    yield return new WaitForSecondsRealtime(4);
                }
                else
                { 
                    yield break;
                }
            }
            
            yield return null;
        }

        #endregion

    }

    #region API_DTO

    public class Asset
    {
        public string id { get; set; }
        public string url { get; set; }
        public string mimeType { get; set; }
        public Metadata metadata { get; set; }
        public string ownerId { get; set; }
        public string authorId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string privacy { get; set; }
        public List<object> tags { get; set; }
        public List<object> collectionIds { get; set; }
    }

    public class Job
    {
        public string jobId { get; set; }
        public string jobType { get; set; }
        public string status { get; set; }
        public float progress { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Metadata
    {
        public string type { get; set; }
        public string preset { get; set; }
        public string parentId { get; set; }
        public string rootParentId { get; set; }
        public string kind { get; set; }
        public string[] assetIds { get; set; }
        public int scalingFactor { get; set; }
        public bool magic { get; set; }
        public bool forceFaceRestoration { get; set; }
        public bool photorealist { get; set; }
    }

    public class Root
    {
        public Asset asset { get; set; }
        public Job job { get; set; }
        public string image { get; set; }
    }

    #endregion
}