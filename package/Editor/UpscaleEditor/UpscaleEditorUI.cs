using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor.UpscaleEditor
{
    public class UpscaleEditorUI
    {
        #region Public Fields

        public static Texture2D currentImage = null;
        public static ImageDataStorage.ImageData imageData = null;

        public UpscaleEditor UpscaleEditor { get { return upscaleEditor; } set { upscaleEditor = value; } }

        #endregion

        #region Private Fields

        private static List<ImageDataStorage.ImageData> imageDataList = new();

        /// <summary>
        /// Reference object to the upscale editor parent class.
        /// </summary>
        private UpscaleEditor upscaleEditor = null;

        private List<Texture2D> upscaledImages = new();
        private Texture2D selectedTexture = null;
        private Vector2 scrollPosition = Vector2.zero;
        private string imageDataUrl = "";
        private string assetId = string.Empty;
        private bool returnImage = true;

        /// <summary>
        /// Default scaling factor
        /// </summary>
        private int scalingFactor = 2;

        /// <summary>
        /// Style selected on the upscale
        /// </summary>
        private string styleSelected = "Standard";

        /// <summary>
        /// Preset selected on the upscale
        /// </summary>
        private string presetSelected = "Balanced";

        /// <summary>
        /// All style available in upscale
        /// </summary>
        private string[] styleChoices = new string[]
        {
            "Standard",
            "Cartoon",
            "Anime",
            "Comic",
            "Minimalist",
            "Photorealistic",
            "3D Rendered"
        };

        /// <summary>
        /// Flag of the style selected
        /// </summary>
        private int styleFlag = 0;

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

                EditorStyle.Button("Clear Image", () =>
                {
                    currentImage = null;
                    assetId = string.Empty;
                });
            }

            EditorStyle.Label("Upscale Image Options", bold: true);

            styleFlag = EditorGUILayout.Popup("Style: ", styleFlag, styleChoices);
            styleSelected = styleChoices[styleFlag];

            EditorStyle.Label("Scaling Factor:");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Toggle(scalingFactor == 2, "2x", EditorStyles.miniButtonLeft))
                {
                    scalingFactor = 2;
                }
                if (GUILayout.Toggle(scalingFactor == 4, "4x", EditorStyles.miniButtonRight))
                {
                    scalingFactor = 4;
                }
                if (GUILayout.Toggle(scalingFactor == 8, "8x", EditorStyles.miniButtonRight))
                {
                    scalingFactor = 8;
                }
                if (GUILayout.Toggle(scalingFactor == 16, "16x", EditorStyles.miniButtonRight))
                {
                    scalingFactor = 16;
                }
            }
            GUILayout.EndHorizontal();

            CustomStyle.Space(25);

            EditorStyle.Label("Preset:");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Toggle(presetSelected.Equals("Precise"), "Precise", EditorStyles.miniButtonLeft))
                {
                    presetSelected = "Precise";
                }
                if (GUILayout.Toggle(presetSelected.Equals("Balanced"), "Balanced", EditorStyles.miniButtonRight))
                {
                    presetSelected = "Balanced";
                }
                if (GUILayout.Toggle(presetSelected.Equals("Creative"), "Creative", EditorStyles.miniButtonRight))
                {
                    presetSelected = "Creative";
                }
            }
            GUILayout.EndHorizontal();

            EditorStyle.Button("Upscale Image", () =>
            {
                if (currentImage == null) return;
                upscaledImages.Add(null);
                imageDataUrl = CommonUtils.Texture2DToDataURL(currentImage);
                if (imageData != null)
                {
                    assetId = imageData.Id;
                }
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

        /// <summary>
        /// Prepares the upscale request and launches the job.
        /// Once the API returns a job ID, it calls Jobs.CheckJobStatus to poll the job,
        /// and when complete, downloads and displays the upscaled image.
        /// </summary>
        /// <param name="imgUrl">The image URL or data URL.</param>
        private void FetchUpscaledImage(string imgUrl)
        {
            string json = GetJsonPayload(imgUrl);

            if (string.IsNullOrEmpty(assetId))
            {
                ApiClient.RestPost("assets", json, response =>
                {
                    var jsonResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                    assetId = jsonResponse.asset.id;

                    json = GetJsonPayload(imgUrl);

                    ApiClient.RestPost("generate/upscale", json, response =>
                    {
                        var upscaleResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                        var jobId = upscaleResponse.job.jobId;

                        // Call Jobs.CheckJobStatus with a callback to handle the completed asset.
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
                                if (upscaledImages.Count > 0 && upscaledImages[0] == null)
                                {
                                    upscaledImages[0] = texture;
                                }
                                else
                                {
                                    upscaledImages.Insert(0, texture);
                                }
                                imageDataList.Insert(0, newImageData);
                            });
                        });
                    });
                }, errorAction =>
                {
                    upscaledImages.RemoveAt(0);
                });
            }
            else
            {
                ApiClient.RestPost("generate/upscale", json, response =>
                {
                    var upscaleResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                    var jobId = upscaleResponse.job.jobId;

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
                            if (upscaledImages.Count > 0 && upscaledImages[0] == null)
                            {
                                upscaledImages[0] = texture;
                            }
                            else
                            {
                                upscaledImages.Insert(0, texture);
                            }
                            imageDataList.Insert(0, newImageData);
                        });
                    });
                });
            }
        }

        /// <summary>
        /// Prepares the JSON payload for the upscale request.
        /// </summary>
        /// <param name="imgUrl">The image URL or data URL.</param>
        /// <returns>The JSON payload as a string.</returns>
        private string GetJsonPayload(string imgUrl)
        {
            string json;

            switch (styleSelected)
            {
                case "3D Rendered":
                    styleSelected = "3d-rendered";
                    break;
                case "Photorealistic":
                    styleSelected = "photography";
                    break;
            }

            if (string.IsNullOrEmpty(assetId))
            {
                var payload = new
                {
                    image = imgUrl,
                    preset = presetSelected.ToLower(),
                    style = styleSelected.ToLower(),
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
                    image = assetId,
                    assetId = assetId,
                    preset = presetSelected.ToLower(),
                    style = styleSelected.ToLower(),
                    scalingFactor = scalingFactor,
                    returnImage = returnImage,
                    name = CommonUtils.GetRandomImageFileName()
                };
                json = JsonConvert.SerializeObject(payload);
            }

            return json;
        }
    }
}
