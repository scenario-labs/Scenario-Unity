using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class ImagesUI
    {
        public int itemsPerRow = 5;
        public float padding = 10f;
        public Vector2 scrollPosition = Vector2.zero;
        public Vector2 selectedTextureSectionScrollPosition = Vector2.zero;
        public Texture2D selectedTexture = null;
        public List<Texture2D> textures = new List<Texture2D>();

        internal Images images;

        private int selectedTextureIndex = 0;

        // Dictionary containing button labels and associated actions
        private Dictionary<string, Action> buttonActions = new Dictionary<string, Action>();

        /// <summary>
        /// Contain the draw function of the current Detail Panel according to the button clicked
        /// </summary>
        private Action buttonDetailPanelDrawFunction = null;

        /// <summary>
        /// Usefull to show a small message when the images are loading
        /// </summary>
        private bool isLoading = false;


        #region Initialization

        /// <summary>
        /// Initializes the PromptImagesUI class with the provided PromptImages instance.
        /// </summary>
        /// <param name="_images">The images instance to initialize with.</param>
        public void Init(Images _images)
        {
            images = _images;
            InitializeButtons();
        }

        #endregion


        #region UI Drawing

        /// <summary>
        /// Draws the background of the UI element with the specified position.
        /// This function fills the background of a UI element with a given color.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void InitializeButtons()
        {
            // Dictionary containing button labels and associated actions
            buttonActions = new Dictionary<string, Action>()
            {
                {
                    "Set Image as Reference", () =>
                    {
                        PromptWindowUI.imageUpload = selectedTexture;
                        PromptWindow.ShowWindow();
                        PromptWindow.SetImageControlTab(1);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been set in the Image to Image parameter in the Prompt Window.", EditorStyles.wordWrappedLabel);
                        };

                    }
                },
                {
                    "Download as Texture",  () =>
                    {
                        CommonUtils.SaveTextureAsPNG(selectedTexture, importPreset:PluginSettings.TexturePreset);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been dowloaded as a Texture in the folder you specified in the Scenario Plugin Settings.", EditorStyles.wordWrappedLabel);
                        };
                    }
                },
                {
                    "Download as Sprite", () =>
                    {
                        string messageWhileDownloading = "Please wait... The background is currently being removed. The result will be downloaded in the folder you specified in the Scenario Plugin Settings.";
                        string messageSuccess = "Your image has been downloaded in the folder you specified in the Scenario Plugin Settings.";

                        //What to do when file is downloaded
                        Action<string> successAction = (filePath) =>
                        {
                            buttonDetailPanelDrawFunction = () =>
                            {
                                GUILayout.Label(messageSuccess, EditorStyles.wordWrappedLabel);
                            };

                            if (PluginSettings.UsePixelsUnitsEqualToImage)
                            {
                                CommonUtils.ApplyPixelsPerUnit(filePath);
                            }
                        };


                        if (PluginSettings.AlwaysRemoveBackgroundForSprites)
                        {
                            images.RemoveBackground(selectedTextureIndex, (imageBytes) =>
                            {
                                CommonUtils.SaveImageDataAsPNG(imageBytes, null, PluginSettings.SpritePreset, successAction);
                            });

                            buttonDetailPanelDrawFunction = () =>
                            {
                                GUILayout.Label(messageWhileDownloading, EditorStyles.wordWrappedLabel);
                            };
                        }
                        else
                        {
                            CommonUtils.SaveTextureAsPNG(selectedTexture, null, PluginSettings.SpritePreset, successAction);
                        }
                    }
                },
                {
                    "Download as a Tile", () =>
                    {
                        /// Contains the side window when the user want to download an image as a tile
                        //PromptImagesTileCreator tileCreator = new(images, selectedTextureIndex);
                        //buttonDetailPanelDrawFunction = tileCreator.OnGUI;
                    }
                },
                { "Pixelate Image", () => PixelEditor.ShowWindow(selectedTexture, Images.imageDataList[selectedTextureIndex])},
                { "Upscale Image",  () => UpscaleEditor.ShowWindow(selectedTexture, Images.imageDataList[selectedTextureIndex])},
                {
                    "Delete", () =>
                    {
                        // Delete the image at the selected index and clear the selected texture
                        images.DeleteImageAtIndex(selectedTextureIndex);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been deleted.");
                        };
                        selectedTexture = null;
                    }
                }
            };
        }

        /// <summary>
        /// This function is responsible for rendering all the interface
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void OnGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            float previewWidth = 320f;
            float imageListWidth = selectedTexture != null ? _dimension.width - previewWidth : _dimension.width;
            float boxWidth = (imageListWidth - padding * (itemsPerRow - 1)) / itemsPerRow;
            float boxHeight = boxWidth;

            int numRows = Mathf.CeilToInt((float)Images.imageDataList.Count / itemsPerRow);

            float scrollViewHeight = (boxHeight + padding) * numRows;
            var scrollViewRect = new Rect(0, 25, imageListWidth, _dimension.height);
            var viewRect = new Rect(0, 0, imageListWidth - 20, scrollViewHeight + 50);
            float totalHeight = 0;

            if (Images.imageDataList.Count == 0)
            {
                ShowLoadingPage();
            }
            else
            {
                scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);
                {
                    DrawTextureBoxes(boxWidth, boxHeight, out totalHeight);
                }
                if(!isLoading)
                {
                    if (GUI.Button(new Rect(0, totalHeight + 10, imageListWidth, 20), new GUIContent("Load More", "Load next images from your account.")))
                    {
                        isLoading = true;
                        Images.GetInferencesData( () =>
                        {
                            isLoading = false;
                            images.Repaint();
                        });
                    }
                }
                GUI.EndScrollView();
            }

            GUILayout.FlexibleSpace();

            DrawSelectedTextureSection(_dimension, previewWidth, imageListWidth);
        }

        private void ShowLoadingPage()
        {
            CustomStyle.Label("Loading images...");
        }

        /// <summary>
        /// Draws the section displaying the title, the close button, the selected texture along with its details.
        /// This function calculates and renders the selected image, its dimensions, and associated UI elements.
        /// </summary>
        /// <param name="_parentDimension">The position and dimensions of the parent container.</param>
        /// <param name="_sectionWidth">The width of the preview section.</param>
        /// <param name="_leftPosition">The position, on X axe, where the selected texture section should begin to draw.</param>
        private void DrawSelectedTextureSection(Rect _parentDimension, float _sectionWidth, float _leftPosition)
        {
            if (selectedTexture == null || textures.Count <= 0)
                return;

            GUILayout.BeginArea(new Rect(_leftPosition, 20, _sectionWidth, _parentDimension.height - 20));
            {
                CustomStyle.Space(5);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Selected Image", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    {
                        CloseSelectedTextureSection();
                    }
                }
                GUILayout.EndHorizontal();

                if (selectedTexture != null) //if you click on close, the selected texture can be null
                    DrawScrollableArea(_sectionWidth, _parentDimension.height - 70);
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws the scrollable area containing the selected image, the action buttons, and the image data.
        /// </summary>
        /// <param name="_sectionWidth">The width of the scrollable area.</param>
        /// <param name="_textureSectionHeight">The height of the scrollable area.</param>
        private void DrawScrollableArea(float _sectionWidth, float _textureSectionHeight)
        {
            selectedTextureSectionScrollPosition = GUILayout.BeginScrollView(selectedTextureSectionScrollPosition, GUILayout.Width(_sectionWidth), GUILayout.Height(_textureSectionHeight));
            {
                DrawSelectedImage(_sectionWidth);
                CustomStyle.Space(10);
                GUILayout.BeginVertical();
                {
                    if (buttonDetailPanelDrawFunction != null)
                    {
                        DrawButtonDetailPanel();
                    }
                    else
                    {
                        DrawButtons();
                        CustomStyle.Space(10);
                        DrawImageData();
                        
                    }
                }
                GUILayout.EndVertical();
                CustomStyle.Space(10);
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the selected image in the preview section.
        /// This function renders the selected image along with its label and close button.
        /// </summary>
        /// <param name="previewWidth">The width of the preview section.</param>
        private void DrawSelectedImage(float previewWidth)
        {
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float paddedPreviewWidth = previewWidth - 4 * padding;
            float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;

            GUILayout.BeginHorizontal();
            {
                CustomStyle.Space(padding);
                GUILayout.Label(selectedTexture, GUILayout.Width(paddedPreviewWidth), GUILayout.Height(paddedPreviewHeight));
                CustomStyle.Space(padding);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Renders the UI in the modal of a selected image with a set of buttons, each associated with specific actions. 
        /// The buttons are displayed horizontally, and when clicked, they trigger their respective actions.
        /// </summary>
        private void DrawButtons()
        {

            // Iterate through the buttons and display them in a horizontal layout
            foreach (var button in buttonActions)
            {
                GUILayout.BeginHorizontal();
                // Create a button with the button label
                if (GUILayout.Button(button.Key, GUILayout.Height(40)))
                {
                    // When the button is clicked, execute the associated action
                    button.Value();
                }
                GUILayout.EndHorizontal();
                // Add spacing between buttons for visual separation
                CustomStyle.Space(5);
            }
        }

        /// <summary>
        /// Draws the data associated with the currently selected image, including prompt, steps, size, guidance, and scheduler.
        /// This function displays textual information about the selected image's attributes.
        /// </summary>
        private void DrawImageData()
        {
            var currentImageData = Images.imageDataList[selectedTextureIndex];
            GUILayout.BeginVertical();
            {
                CustomStyle.Label("Prompt:");
                CustomStyle.Label($"{currentImageData.Prompt}");
                CustomStyle.Space(padding);
                GUILayout.BeginHorizontal();
                {
                    CustomStyle.Label($"Steps: {currentImageData.Steps}");
                    CustomStyle.Label($"Size: {currentImageData.Size}");
                }
                GUILayout.EndHorizontal();
                CustomStyle.Space(padding);
                GUILayout.BeginHorizontal();
                {
                    CustomStyle.Label($"Guidance: {currentImageData.Guidance}");
                    CustomStyle.Label($"Scheduler: {currentImageData.Scheduler}");
                }
                GUILayout.EndHorizontal();
                CustomStyle.Space(padding);
                GUILayout.BeginHorizontal();
                {
                    CustomStyle.Label($"Seed: {currentImageData.Seed}");
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// When using one of the action button, sometime they opens a panel with some informations and other steps
        /// </summary>
        private void DrawButtonDetailPanel()
        {
            buttonDetailPanelDrawFunction?.Invoke();

            if (GUILayout.Button("< Back", EditorStyles.miniButtonLeft))
            {
                buttonDetailPanelDrawFunction = null;
            }
            
        }

        /// <summary>
        /// Draws a grid of texture boxes, each containing an image or loading indicator, and handles interactions.
        /// This function renders a grid of image boxes and handles interactions .
        /// Possible improvement : Provide visual feedback when images are being loaded. For example, display a loading spinner or progress bar within the texture boxes while images are being fetched or loaded asynchronously.
        /// </summary>
        /// <param name="boxWidth">The width of each texture box.</param>
        /// <param name="boxHeight">The height of each texture box.</param>
        private void DrawTextureBoxes(float boxWidth, float boxHeight, out float totalHeight)
        {
            totalHeight = 0;

            for (int i = 0; i < Images.imageDataList.Count; i++)
            {
                int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
                int colIndex = i % itemsPerRow;

                Rect boxRect = CalculateBoxRect(boxWidth, boxHeight, rowIndex, colIndex);
                Texture2D texture = null;
                if (textures.Count > i && textures[i] != null)
                    texture = textures[i];

                totalHeight = boxRect.y + boxRect.height;

                if (texture != null)
                {
                    HandleImageClickEvents(boxRect, texture, i);
                    RenderTextureBox(boxRect, texture);
                }
                else
                {
                    RenderLoadingBox(boxRect);
                }
            }
        }


        #endregion


        #region Utility Methods


        /// <summary>
        /// Calculates the position and dimensions of each texture box within the grid based on the specified box width, box height, row index, and column index.
        /// </summary>
        /// <param name="boxWidth">The width of each texture box.</param>
        /// <param name="boxHeight">The height of each texture box.</param>
        /// <param name="rowIndex">The row index of the texture box.</param>
        /// <param name="colIndex">The column index of the texture box.</param>
        /// <returns>A Rect representing the position and dimensions of the texture box.</returns>
        private Rect CalculateBoxRect(float boxWidth, float boxHeight, int rowIndex, int colIndex)
        {
            float x = colIndex * (boxWidth + padding);
            float y = rowIndex * (boxHeight + padding);
            return new Rect(x, y, boxWidth, boxHeight);
        }

        /// <summary>
        /// Manages interactions with texture boxes, including selecting images when clicked.
        /// </summary>
        /// <param name="boxRect">The Rect representing the boundaries of the texture box.</param>
        /// <param name="texture">The Texture2D associated with the texture box.</param>
        /// <param name="index">The index of the texture box in the grid.</param>
        private void HandleImageClickEvents(Rect boxRect, Texture2D texture, int index)
        {
            if (GUI.Button(boxRect, ""))
            {
                HandleImageSelection(texture, index);
            }
        }

        /// <summary>
        /// Handles the selection of a texture box, setting the selected texture and index when clicked.
        /// </summary>
        /// <param name="texture">The Texture2D associated with the selected texture box.</param>
        /// <param name="index">The index of the selected texture box in the grid.</param>
        private void HandleImageSelection(Texture2D texture, int index)
        {
            selectedTexture = texture;
            selectedTextureIndex = index;
            buttonDetailPanelDrawFunction = null; //reset the button detail panel when you select a new image
        }

        /// <summary>
        /// Renders a texture box by drawing the associated image within the specified box's boundaries, scaling it to fit.
        /// </summary>
        /// <param name="boxRect">The Rect representing the boundaries of the texture box.</param>
        /// <param name="texture">The Texture2D to render within the texture box.</param>
        private void RenderTextureBox(Rect boxRect, Texture2D texture)
        {
            GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// Renders a loading indicator within a texture box, providing visual feedback to users while images are being fetched or loaded asynchronously.
        /// </summary>
        /// <param name="boxRect">The Rect representing the boundaries of the texture box.</param>
        private void RenderLoadingBox(Rect boxRect)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.color = Color.white;
            GUI.Label(boxRect, "Loading...", style);
        }

        public void CloseSelectedTextureSection()
        {
            //ClearData();
            selectedTexture = null;
        }

        #endregion

    }
}