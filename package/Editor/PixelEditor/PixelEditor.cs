using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Scenario.Editor.PixelEditorWindow;

namespace Scenario.Editor
{
    public class PixelEditor : EditorWindow
    {
        #region Public Fields
        #endregion

        #region Private Fields

        private static readonly PixelEditorUI pixelEditorUI = new PixelEditorUI();

        #endregion

        #region MonoBehaviourCallback

        [MenuItem("Scenario/Editors/Pixel Editor", false, 5)]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(PixelEditor), false, "Pixel Editor") as PixelEditor;
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(720, 540);
        }

        public static void ShowWindow(Texture2D selectedTexture, ImageDataStorage.ImageData imageData)
        {
            PixelEditorUI.currentImage = selectedTexture;
            PixelEditorUI.imageData = imageData;
            ShowWindow();
        }

        private void OnGUI()
        {
            pixelEditorUI.OnGUI(this.position);
            pixelEditorUI.PixelEditor = this;
        }

        private void OnDestroy()
        {
            pixelEditorUI.ClearData();
        }

        #endregion
    }

    #region API_DTO_PixelEditor // Renamed region to be unique

    public class PixelAsset
    {
        public string id { get; set; }
        public string url { get; set; }
        public string mimeType { get; set; }
        public PixelMetadata metadata { get; set; }
        public string ownerId { get; set; }
        public string authorId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string privacy { get; set; }
        public List<object> tags { get; set; }
        public List<object> collectionIds { get; set; }
    }

    public class PixelJob
    {
        public string jobId { get; set; }
        public string jobType { get; set; }
        public string status { get; set; }
        public float progress { get; set; }
        public PixelMetadata metadata { get; set; }
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

    public class PixelMetadata
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

    public class PixelRoot
    {
        public PixelAsset asset { get; set; }
        public PixelJob job { get; set; }
        public string image { get; set; }
    }

    #endregion
}