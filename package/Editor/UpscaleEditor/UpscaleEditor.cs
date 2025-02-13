using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Scenario.Editor.UpscaleEditor;

namespace Scenario.Editor.UpscaleEditor
{
    public class UpscaleEditor : EditorWindow
    {
        #region Public Fields
        #endregion

        #region Private Fields

        private static readonly UpscaleEditorUI UpscaleEditorUI = new UpscaleEditorUI();

        #endregion

        #region MonoBehaviourCallback

        [MenuItem("Scenario/Editors/Upscale Editor", false, 5)]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(UpscaleEditor), false, "Upscale Editor") as UpscaleEditor;
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(720, 540);
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
            // Clear all UI state when the window is closed.
            UpscaleEditorUI.ClearData();
        }

        #endregion
    }

    #region API_DTO_UpscaleEditor // Renamed region to be unique

    public class UpscaleAsset
    {
        public string id { get; set; }
        public string url { get; set; }
        public string mimeType { get; set; }
        public UpscaleMetadata metadata { get; set; }
        public string ownerId { get; set; }
        public string authorId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string privacy { get; set; }
        public List<object> tags { get; set; }
        public List<object> collectionIds { get; set; }
    }

    public class UpscaleJob
    {
        public string jobId { get; set; }
        public string jobType { get; set; }
        public string status { get; set; }
        public float progress { get; set; }
        public UpscaleMetadata metadata { get; set; }
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

    public class UpscaleMetadata
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

    public class UpscaleRoot
    {
        public UpscaleAsset asset { get; set; }
        public UpscaleJob job { get; set; }
        public string image { get; set; }
    }

    #endregion
}