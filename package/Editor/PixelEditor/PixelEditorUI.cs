using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor.PixelEditorWindow
{
    public class PixelEditorUI
    {
        #region Public Fields

        public static Texture2D currentImage = null;
        public static ImageDataStorage.ImageData imageData = null;

        public PixelEditor PixelEditor { get { return pixelEditor; } set { pixelEditor = value; } }

        #endregion

        #region Private Fields

        private static List<ImageDataStorage.ImageData> imageDataList = new();

        /// <summary>
        /// Reference object to the Pixel editor parent class.
        /// </summary>
        private PixelEditor pixelEditor = null;

        private List<Texture2D> pixelatedImages = new();
        private Texture2D selectedTexture = null;
        private Vector2 scrollPosition = Vector2.zero;
        private string imageDataUrl = "";
        private string assetId = string.Empty;
        private bool returnImage = true;

        /// <summary>
        /// Default pixel grid size
        /// </summary>
        private int pixelGridSize = 32;

        /// <summary>
        /// Flag to remove noise during pixelation
        /// </summary>
        public bool removeNoise = false;

        /// <summary>
        /// Flag to remove background during pixelation
        /// </summary>
        public bool removeBackground = false;

        /// <summary>
        /// Allowed pixel grid sizes
        /// </summary>
        private readonly int[] allowedPixelGridSizes = { 16, 32, 64, 128, 256 };

        /// <summary>
        /// Index of the selected pixel grid size
        /// </summary>
        private int selectedGridSizeIndex = 0;


        private int itemsPerRow = 1;
        private readonly float padding = 10f;
        private readonly float leftSectionWidth = 150;

        #endregion

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

            EditorStyle.Label("Pixelate Image", bold: true);
            if (currentImage == null)
            {
                DrawImageUploadArea();
                HandleDrag();
            }
            else
            {
                Rect rect = GUILayoutUtility.GetRect(leftSectionWidth, leftSectionWidth, GUILayout.Width(300), GUILayout.Height(300));
                GUI.DrawTexture(rect, currentImage, ScaleMode.ScaleToFit);

                EditorStyle.Button("Clear Image", () =>
                {
                    currentImage = null;
                    assetId = string.Empty;
                    imageDataUrl = string.Empty;
                    pixelatedImages.Clear();
                    selectedTexture = null;
                });
            }

            EditorStyle.Label("Pixelate Options", bold: true);

            EditorStyle.Label("Pixel Grid Size:");
            selectedGridSizeIndex = GUILayout.SelectionGrid(selectedGridSizeIndex, Array.ConvertAll(allowedPixelGridSizes, x => x.ToString()), allowedPixelGridSizes.Length);
            pixelGridSize = allowedPixelGridSizes[selectedGridSizeIndex];

            removeNoise = EditorGUILayout.Toggle("Remove Noise", removeNoise);
            removeBackground = EditorGUILayout.Toggle("Remove Background", removeBackground);


            EditorStyle.Button("Pixelate Image", () =>
            {
                if (currentImage == null) return;
                pixelatedImages.Add(null);
                imageDataUrl = CommonUtils.Texture2DToDataURL(currentImage);
                FetchPixelatedImage(imageDataUrl);
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
            GUI.Box(dropArea, "Drag & Drop an image here to pixelate"); // Changed text

            Rect buttonRect = new Rect(dropArea.center.x - 50f, dropArea.center.y - 15f, 100f, 30f);
            if (GUI.Button(buttonRect, "Choose Image"))
            {
                HandleChooseImageClick();
            }
        }

        private static void HandleChooseImageClick()
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

        private Rect DrawLeftSection(Rect position)
        {
            // Left section
            GUILayout.BeginVertical(GUILayout.Width(position.width * 0.85f));
            float requiredWidth = itemsPerRow * (256 + padding) + padding;
            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, requiredWidth, position.height - 20), scrollPosition, new Rect(0, 0, requiredWidth, position.height - 20));
            itemsPerRow = 5;

            for (int i = 0; i < pixelatedImages.Count; i++)
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
            Texture2D texture = pixelatedImages[i];

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

        /// <summary>
        /// Prepares the pixelate request and launches the job.
        /// Once the API returns a job ID, it calls Jobs.CheckJobStatus to poll the job,
        /// and when complete, downloads and displays the pixelated image.
        /// </summary>
        /// <param name="imgUrl">The image URL or data URL.</param>
        private void FetchPixelatedImage(string imgUrl)
        {
            string json = GetJsonPayload(imgUrl);
            Debug.Log("JSON Payload for Pixelate: " + json);

            if (string.IsNullOrEmpty(assetId))
            {
                ApiClient.RestPost("assets", json, response =>
                {
                    var jsonResponse = JsonConvert.DeserializeObject<PixelRoot>(response.Content);
                    assetId = jsonResponse.asset.id;

                    json = GetJsonPayload(imgUrl);
                    Debug.Log("JSON Payload for Pixelate (after asset upload): " + json);

                    ApiClient.RestPost("generate/pixelate", json, response =>
                    {
                        var pixelateResponse = JsonConvert.DeserializeObject<PixelRoot>(response.Content);
                        var jobId = pixelateResponse.job.jobId;

                        Scenario.Editor.Jobs.CheckJobStatus(jobId, asset =>
                        {
                            Texture2D texture = new Texture2D(2, 2);
                            CommonUtils.FetchTextureFromURL(asset.url, fetchedTexture =>
                            {
                                texture = fetchedTexture;
                                ImageDataStorage.ImageData newImageData = new ImageDataStorage.ImageData
                                {
                                    Id = asset.id,
                                    Url = asset.url,
                                };
                                if (pixelatedImages.Count > 0 && pixelatedImages[0] == null)
                                {
                                    pixelatedImages[0] = texture;
                                }
                                else
                                {
                                    pixelatedImages.Insert(0, texture);
                                }
                                imageDataList.Insert(0, newImageData);
                            });
                        });
                    }, errorAction =>
                    {
                        pixelatedImages.RemoveAt(0);
                    });
                }, errorAction =>
                {
                    pixelatedImages.RemoveAt(0);
                });
            }
            else
            {
                json = GetJsonPayload(imgUrl);
                Debug.Log("JSON Payload for Pixelate (using existing assetId): " + json);

                ApiClient.RestPost("generate/pixelate", json, response =>
                {
                    var pixelateResponse = JsonConvert.DeserializeObject<PixelRoot>(response.Content);
                    var jobId = pixelateResponse.job.jobId;

                    Scenario.Editor.Jobs.CheckJobStatus(jobId, asset =>
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        CommonUtils.FetchTextureFromURL(asset.url, fetchedTexture =>
                        {
                            texture = fetchedTexture;
                            ImageDataStorage.ImageData newImageData = new ImageDataStorage.ImageData
                            {
                                Id = asset.id,
                                Url = asset.url,
                            };
                            if (pixelatedImages.Count > 0 && pixelatedImages[0] == null)
                            {
                                pixelatedImages[0] = texture;
                            }
                            else
                            {
                                pixelatedImages.Insert(0, texture);
                            }
                            imageDataList.Insert(0, newImageData);
                        });
                    });
                }, errorAction =>
                {
                    pixelatedImages.RemoveAt(0);
                });
            }
        }

        /// <summary>
        /// Prepares the JSON payload for the pixelate request. // Changed summary
        /// </summary>
        /// <param name="imgUrl">The image URL or data URL.</param>
        /// <returns>The JSON payload as a string.</returns>
        private string GetJsonPayload(string imgUrl)
        {
            string json;

            if (string.IsNullOrEmpty(assetId))
            {
                var payload = new
                {
                    image = imgUrl,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new
                {
                    image = assetId,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = CommonUtils.GetRandomImageFileName()
                };
                json = JsonConvert.SerializeObject(payload);
            }

            return json;
        }

        /// <summary>
        /// Clears all stored UI data so that the next time the window is opened,
        /// no stale data is displayed for Pixel Editor. // Changed Summary
        /// </summary>
        public void ClearData()
        {
            currentImage = null;
            imageData = null;
            imageDataList.Clear();
            pixelatedImages.Clear();
            selectedTexture = null;
            assetId = "";
            imageDataUrl = "";
            pixelGridSize = 32;
            selectedGridSizeIndex = 0;
            removeNoise = false;
            removeBackground = false;
        }
    }
}