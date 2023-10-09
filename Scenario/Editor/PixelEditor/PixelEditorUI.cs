using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace Scenario
{
    public class PixelEditorUI
    {
        public static Texture2D currentImage = null;
        public static ImageDataStorage.ImageData imageData = null;

        private static List<ImageDataStorage.ImageData> imageDataList = new();

        public bool removeNoise = false;
        public bool removeBackground = false;
        private bool returnImage = true;
        private int itemsPerRow = 1;
        private string imageDataUrl = "";
        private string assetId = "";
        private float pixelGridSize = 32f;
        private float padding = 10f;
        private Vector2 scrollPosition = Vector2.zero;
        private Texture2D selectedTexture = null;
        private List<Texture2D> pixelatedImages = new();
    
        private float leftSectionWidth = 150;
        private int selectedGridSizeIndex = 0;
        private readonly int[] allowedPixelGridSizes = { 32, 64, 128, 256 };

        public void OnGUI(Rect position)
        {
            DrawBackground(position);

            GUILayout.BeginHorizontal();

            position = DrawLeftSection(position);

            GUILayout.FlexibleSpace();

            DrawRightSection(position);

            GUILayout.EndHorizontal();
        }

        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void DrawRightSection(Rect position)
        {
            // Right section
            GUILayout.BeginVertical(GUILayout.Width(position.width * 0.15f));

            EditorStyle.Label("Pixelate Image", bold: true);
            if (currentImage == null)
            {
                Rect dropArea = GUILayoutUtility.GetRect(0f, 150f, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "Drag & Drop an image here");

                Rect buttonRect = new Rect(dropArea.center.x - 50f, dropArea.center.y - 15f, 100f, 30f);
                if (GUI.Button(buttonRect, "Choose Image"))
                {
                    HandleChooseImageClick();
                }

                HandleDrag();
            }
            else
            {
                Rect rect = GUILayoutUtility.GetRect(leftSectionWidth, leftSectionWidth, GUILayout.Width(300), GUILayout.Height(300));
                GUI.DrawTexture(rect, currentImage, ScaleMode.ScaleToFit);

                EditorStyle.Button("Clear Image", ()=> currentImage = null);
            }


            EditorStyle.Label("Pixel Grid Size:");
            int pixelGridSizeIndex = Array.IndexOf(allowedPixelGridSizes, (int)pixelGridSize);
            if (pixelGridSizeIndex == -1) { pixelGridSizeIndex = 0; }
        
            selectedGridSizeIndex = GUILayout.SelectionGrid(selectedGridSizeIndex, Array.ConvertAll(allowedPixelGridSizes, x => x.ToString()), allowedPixelGridSizes.Length);
            pixelGridSize = allowedPixelGridSizes[selectedGridSizeIndex];
            removeNoise = EditorGUILayout.Toggle("Remove Noise", removeNoise);
            removeBackground = EditorGUILayout.Toggle("Remove Background", removeBackground);

            EditorStyle.Button("Pixelate Image", () =>
            {
                if (currentImage == null) return;
            
                imageDataUrl = CommonUtils.Texture2DToDataURL(currentImage);
                assetId = imageData.Id;
                FetchPixelatedImage(imageDataUrl);
            });
        
            if (selectedTexture != null)
            {
                EditorStyle.Button("Download", () => CommonUtils.SaveTextureAsPNG(selectedTexture));
            }

            GUILayout.EndVertical();
        }

        private static void HandleDrag()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (currentEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        string path = DragAndDrop.paths[0];
                        if (System.IO.File.Exists(path) &&
                            (System.IO.Path.GetExtension(path).ToLower() == ".png" ||
                             System.IO.Path.GetExtension(path).ToLower() == ".jpg" ||
                             System.IO.Path.GetExtension(path).ToLower() == ".jpeg"))
                        {
                            currentImage = new Texture2D(2, 2);
                            byte[] imgBytes = File.ReadAllBytes(path);
                            currentImage.LoadImage(imgBytes);
                        }
                    }
                    currentEvent.Use();
                }
            }
        }

        private static void HandleChooseImageClick()
        {
            string imagePath = EditorUtility.OpenFilePanel("Choose Image", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(imagePath))
            {
                currentImage = new Texture2D(2, 2);
                byte[] imgBytes = File.ReadAllBytes(imagePath);
                currentImage.LoadImage(imgBytes);

                PixelEditorUI.imageData = new ImageDataStorage.ImageData();
            }
        }

        private Rect DrawLeftSection(Rect position)
        {
            // Left section
            GUILayout.BeginVertical(GUILayout.Width(position.width * 0.85f));
            float requiredWidth = itemsPerRow * (256 + padding) + padding;
            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, requiredWidth, position.height - 20), scrollPosition, new Rect(0, 0, requiredWidth, position.height - 20));
            itemsPerRow = 5;

            for (int i = 0; i < pixelatedImages.Count; i++)
            {
                int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
                int colIndex = i % itemsPerRow;

                Rect boxRect = new Rect(colIndex * (256 + padding), rowIndex * (256 + padding), 256, 256);
                Texture2D texture = pixelatedImages[i];

                if (texture != null)
                {
                    if (GUI.Button(boxRect, ""))
                    {
                        selectedTexture = texture;
                    }
                    GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(boxRect, "Loading...");
                }
            }
            GUI.EndScrollView();
            GUILayout.EndVertical();
            return position;
        }
    
        private void FetchPixelatedImage(string imgUrl)
        {
            string json = "";
        
            if (assetId == "")
            {
                var payload = new
                {
                    image = imgUrl,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = "",
                    colorPalette = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new
                {
                    image = imgUrl,
                    assetId = assetId,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = CommonUtils.GetRandomImageFileName(),
                    colorPalette = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }
        
            ApiClient.RestPut("images/pixelate", json, response =>
            {
                var pixelatedResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                var texture = CommonUtils.DataURLToTexture2D(pixelatedResponse.image);
                var newImageData = new ImageDataStorage.ImageData
                {
                    Id = pixelatedResponse.asset.id,
                    Url = pixelatedResponse.image, 
                    InferenceId = pixelatedResponse.asset.ownerId,
                };
                pixelatedImages.Insert(0, texture);
                imageDataList.Insert(0, newImageData);
            });
        }
    }

    #region API_DTOS

    [Serializable]
    public class Asset
    {
        public string id { get; set; }
        public string mimeType { get; set; }
        public Type type { get; set; }
        public string ownerId { get; set; }
        public string authorId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string privacy { get; set; }
        public List<object> tags { get; set; }
        public List<object> collectionIds { get; set; }
    }

    [Serializable]
    public class Root
    {
        public Asset asset { get; set; }
        public string image { get; set; }
    }

    [Serializable]
    public class Type
    {
        public string source { get; set; }
        public string parentId { get; set; }
        public string rootParentId { get; set; }
        public string kind { get; set; }
        public int pixelGridSize { get; set; }
        public bool removeNoise { get; set; }
        public bool removeBackground { get; set; }
    }

    #endregion
}