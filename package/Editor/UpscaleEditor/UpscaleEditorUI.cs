using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class UpscaleEditorUI
    {
        public static Texture2D currentImage = null;
        public static ImageDataStorage.ImageData imageData = null;
    
        private static List<ImageDataStorage.ImageData> imageDataList = new();

        private List<Texture2D> upscaledImages = new();
        private Texture2D selectedTexture = null;
        private Vector2 scrollPosition = Vector2.zero;
    
        private string imageDataUrl = "";
        private string assetId = "";

        private bool returnImage = true;
        private bool forceFaceRestoration = false;
        private bool usePhotorealisticModel = false;
    
        private int scalingFactor = 2;
        private int itemsPerRow = 1;

        private readonly float padding = 10f;
        private readonly float leftSectionWidth = 150;

        public void OnGUI(Rect position)
        {
            DrawBackground(position);
            GUILayout.BeginHorizontal();
            {
                position = DrawLeftSection(position);
                GUILayout.FlexibleSpace();
                DrawRightSection(position);
            }
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

            EditorStyle.Label("Upscale Image", bold: true);
            if (currentImage == null)
            {
                DrawImageUploadArea();
                HandleDrag();
            }
            else
            {
                Rect rect = GUILayoutUtility.GetRect(leftSectionWidth, leftSectionWidth, GUILayout.Width(300), GUILayout.Height(300));
                GUI.DrawTexture(rect, currentImage, ScaleMode.ScaleToFit);

                EditorStyle.Button("Clear Image", ()=>currentImage = null);
            }

            EditorStyle.Label("Upscale Image Options", bold: true);

            GUILayout.BeginHorizontal();
            {
                EditorStyle.Label("Scaling Factor:");
            
                if (GUILayout.Toggle(scalingFactor == 2, "2", EditorStyles.miniButtonLeft))
                {
                    scalingFactor = 2;
                }

                if (GUILayout.Toggle(scalingFactor == 4, "4", EditorStyles.miniButtonRight))
                {
                    scalingFactor = 4;
                }
            }
            GUILayout.EndHorizontal();

            forceFaceRestoration = EditorGUILayout.Toggle("Force Face Restoration", forceFaceRestoration);
            usePhotorealisticModel = EditorGUILayout.Toggle("Use Photorealistic Model", usePhotorealisticModel);

            EditorStyle.Button("Upscale Image", () =>
            {
                if (currentImage == null) return;
                imageDataUrl = CommonUtils.Texture2DToDataURL(currentImage);
                assetId = imageData.Id;
                FetchUpscaledImage(imageDataUrl);
            });
        
            if (selectedTexture != null)
            {
                EditorStyle.Button("Download", () =>
                {
                    CommonUtils.SaveTextureAsPNG(selectedTexture);
                });
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

        private static void DrawImageUploadArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 150f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop an image here");

            Rect buttonRect = new Rect(dropArea.center.x - 50f, dropArea.center.y - 15f, 100f, 30f);
            if (GUI.Button(buttonRect, "Choose Image"))
            {
                string imagePath = EditorUtility.OpenFilePanel("Choose Image", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(imagePath))
                {
                    currentImage = new Texture2D(2, 2);
                    byte[] imgbytes = File.ReadAllBytes(imagePath);
                    currentImage.LoadImage(imgbytes);

                    imageData = new ImageDataStorage.ImageData();
                }
            }
        }

        private Rect DrawLeftSection(Rect position)
        {
            // Left section
            GUILayout.BeginVertical(GUILayout.Width(position.width * 0.85f));
            float requiredWidth = itemsPerRow * (256 + padding) + padding;
            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, requiredWidth, position.height - 20), scrollPosition, new Rect(0, 0, requiredWidth, position.height - 20));
            itemsPerRow = 5;

            for (int i = 0; i < upscaledImages.Count; i++)
            {
                DrawTextureButton(i);
            }
            GUI.EndScrollView();
            GUILayout.EndVertical();
            return position;
        }

        private void DrawTextureButton(int i)
        {
            int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
            int colIndex = i % itemsPerRow;

            Rect boxRect = new Rect(colIndex * (256 + padding), rowIndex * (256 + padding), 256, 256);
            Texture2D texture = upscaledImages[i];

            if (texture == null)
            {
                GUI.Box(boxRect, "Loading...");
            }
            else
            {
                if (GUI.Button(boxRect, ""))
                {
                    selectedTexture = texture;
                }

                GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
            }
        }

        private void FetchUpscaledImage(string imgUrl)
        {
            string json = GetJsonPayload(imgUrl);
            Debug.Log(json);
            
            ApiClient.RestPut("images/upscale",json, response =>
            {
                var pixelatedResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                Texture2D texture = CommonUtils.DataURLToTexture2D(pixelatedResponse.image);
                ImageDataStorage.ImageData newImageData = new ImageDataStorage.ImageData
                {
                    Id = pixelatedResponse.asset.id,
                    Url = pixelatedResponse.image, 
                    InferenceId = pixelatedResponse.asset.ownerId,
                };
                upscaledImages.Insert(0, texture);
                imageDataList.Insert(0, newImageData);
            });
        }

        private string GetJsonPayload(string imgUrl)
        {
            string json;
            if (assetId == "")
            {
                var payload = new
                {
                    image = imgUrl,
                    forceFaceRestoration = forceFaceRestoration,
                    photorealist = usePhotorealisticModel,
                    scalingFactor = scalingFactor,
                    returnImage = returnImage,
                    name = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new
                {
                    image = imgUrl,
                    assetId = assetId,
                    forceFaceRestoration = forceFaceRestoration,
                    photorealist = usePhotorealisticModel,
                    scalingFactor = scalingFactor,
                    returnImage = returnImage,
                    name = CommonUtils.GetRandomImageFileName()
                };
                json = JsonConvert.SerializeObject(payload);
            }

            return json;
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

        public class Metadata
        {
            public string type { get; set; }
            public string parentId { get; set; }
            public string rootParentId { get; set; }
            public string kind { get; set; }
            public bool magic { get; set; }
            public bool forceFaceRestoration { get; set; }
            public bool photorealist { get; set; }
        }

        public class Root
        {
            public Asset asset { get; set; }
            public string image { get; set; }
        }

        #endregion
    }
}

