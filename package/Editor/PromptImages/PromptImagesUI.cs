using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Scenario
{
    public class PromptImagesUI
    {
        public int itemsPerRow = 5;
        public float padding = 10f;
        public Vector2 scrollPosition = Vector2.zero;
        public Vector2 selectedTextureSectionScrollPosition = Vector2.zero;
        public Texture2D selectedTexture = null;
        public string selectedImageId = null;

        internal PromptImages promptImages;

        private int selectedTextureIndex = 0;
        private bool isModalOpen = false;

        // Dictionary containing button labels and associated actions
        private Dictionary<string, Action> buttonActions = new Dictionary<string, Action>();

        /// <summary>
        /// Contain the draw function of the current Detail Panel according to the button clicked
        /// </summary>
        private Action buttonDetailPanelDrawFunction = null;

        public void Init(PromptImages promptImg)
        {
            promptImages = promptImg;
            InitializeButtons();
        }

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
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been set in the Image to Image parameter in the Prompt Window.", EditorStyles.wordWrappedLabel);
                        };

                    }
                },
                {
                    "Download as Texture",  () =>
                    {
                        CommonUtils.SaveTextureAsPNG(selectedTexture);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been dowloaded as a Texture in the folder you specified in the Scenario Plugin Settings.", EditorStyles.wordWrappedLabel);
                        };
                    }
                },
                {
                    "Delete", () =>
                    {
                        // Delete the image at the selected index and clear the selected texture
                        promptImages.DeleteImageAtIndex(selectedTextureIndex);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been deleted.");
                        };
                        selectedTexture = null;
                    }
                },
                {
                    "Remove Background", () =>
                    {
                        promptImages.RemoveBackground(selectedTextureIndex);
                        buttonDetailPanelDrawFunction = () =>
                        {
                            GUILayout.Label("Your image has been dowloaded without background in the folder you specified in the Scenario Plugin Settings.", EditorStyles.wordWrappedLabel);
                        };
                    }
                },
                { "Pixelate Image", () => PixelEditor.ShowWindow(selectedTexture, DataCache.instance.GetImageDataAtIndex(selectedTextureIndex))},
                { "Upscale Image",  () => UpscaleEditor.ShowWindow(selectedTexture, DataCache.instance.GetImageDataAtIndex(selectedTextureIndex))}
            };
        }

        /// <summary>
        /// Handles the GUI rendering and interaction for the Prompt Images UI.
        /// This function is responsible for rendering image boxes, buttons, and modal pop-ups.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        public void OnGUI(Rect position)
        {
            DrawBackground(position);

            if (GUILayout.Button("Clear All Images"))
            {
                Debug.Log("Clearing Prompt Images");
                DataCache.instance.ClearAllImageData();
            }

            float previewWidth = 309f;
            float scrollViewWidth = selectedTexture != null ? position.width - previewWidth : position.width;
            float boxWidth = (scrollViewWidth - padding * (itemsPerRow - 1)) / itemsPerRow;
            float boxHeight = boxWidth;

            int numRows = Mathf.CeilToInt((float)DataCache.instance.GetImageDataCount() / itemsPerRow);

            float scrollViewHeight = (boxHeight + padding) * numRows;
            var scrollViewRect = new Rect(0, 25, scrollViewWidth, position.height - 70);
            var viewRect = new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight);

            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);
            {
                DrawTextureBoxes(boxWidth, boxHeight);
            }
            GUI.EndScrollView();

            GUILayout.FlexibleSpace();

            if (isModalOpen)
            {
                DrawZoomedImage(new Rect(0, 0, position.width, position.height));
            }

                DrawSelectedTextureSection(position, previewWidth, scrollViewWidth);
        }

        /// <summary>
        /// Draws the section displaying the title, the close button, the selected texture along with its details.
        /// This function calculates and renders the selected image, its dimensions, and associated UI elements.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        /// <param name="previewWidth">The width of the preview section.</param>
        /// <param name="scrollViewWidth">The width of the scrollable area.</param>
        private void DrawSelectedTextureSection(Rect position, float previewWidth, float scrollViewWidth)
        {
            if (selectedTexture == null || DataCache.instance.GetImageDataCount() <= 0)
                return;

            GUILayout.BeginArea(new Rect(scrollViewWidth, 20, previewWidth, position.height - 20));
            {
                CustomStyle.Space(5);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Selected Image", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    {
                        promptImages.CloseSelectedTextureSection();
                    }
                }
                GUILayout.EndHorizontal();

                if (selectedTexture != null) //if you click on close, the selected texture can be null
                    DrawScrollableArea(previewWidth, position.height);
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws the scrollable area containing the selected image, the action buttons, and the image data.
        /// </summary>
        /// <param name="previewWidth">The width of the scrollable area.</param>
        /// <param name="previewHeight">The height of the scrollable area.</param>
        private void DrawScrollableArea(float previewWidth, float previewHeight)
        {
            var scrollViewRect = new Rect(0, 30, previewWidth, previewHeight);
            var viewRect = new Rect(0, 0, previewWidth - 20, previewHeight + 150);

            selectedTextureSectionScrollPosition = GUI.BeginScrollView(scrollViewRect, selectedTextureSectionScrollPosition, viewRect);
            {
                DrawSelectedImage(previewWidth);
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
            GUI.EndScrollView();
        }

        /// <summary>
        /// Draws the selected image in the preview section.
        /// This function renders the selected image along with its label and close button.
        /// </summary>
        /// <param name="previewWidth">The width of the preview section.</param>
        private void DrawSelectedImage(float previewWidth)
        {
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float paddedPreviewWidth = previewWidth - 2 * padding;
            float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;

            GUILayout.BeginHorizontal();
            {
                CustomStyle.Space(padding);
                GUILayout.Label(selectedTexture, GUILayout.Width(paddedPreviewWidth),
                GUILayout.Height(paddedPreviewHeight));
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
            var currentImageData = DataCache.instance.GetImageDataAtIndex(selectedTextureIndex);
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
        /// Draws a modal displaying the selected image in a bigger size with the option to close it when clicking outside the image.
        /// This function renders a modal pop-up for a selected image and allows it to be closed when clicking outside the image boundaries.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        private void DrawZoomedImage(Rect position)
        {
            // Determine image dimensions at 2x scale
            float imageWidth = selectedTexture.width * 2;
            float imageHeight = selectedTexture.height * 2;

            // If the scaled image exceeds the window dimensions, adjust it
            if (imageWidth > position.width)
            {
                float ratio = position.width / imageWidth;
                imageWidth = position.width;
                imageHeight *= ratio;
            }

            if (imageHeight > position.height)
            {
                float ratio = position.height / imageHeight;
                imageHeight = position.height;
                imageWidth *= ratio;
            }

            // Compute the position to center the image in the window
            float imageX = (position.width - imageWidth) / 2;
            float imageY = (position.height - imageHeight) / 2;

            // Draw the image
            GUI.DrawTexture(new Rect(imageX, imageY, imageWidth, imageHeight), selectedTexture, ScaleMode.ScaleToFit);

            // Close the modal if clicked outside the image
            if (Event.current.type == EventType.MouseDown &&
                !new Rect(imageX, imageY, imageWidth, imageHeight).Contains(Event.current.mousePosition))
            {
                isModalOpen = false;
                Event.current.Use();
            }
        }

        /// <summary>
        /// Draws a grid of texture boxes, each containing an image or loading indicator, and handles interactions like double-clicking.
        /// This function renders a grid of image boxes and handles interactions such as double-clicking an image for further details.
        /// Possible improvement : Provide visual feedback when images are being loaded. For example, display a loading spinner or progress bar within the texture boxes while images are being fetched or loaded asynchronously.
        /// </summary>
        /// <param name="boxWidth">The width of each texture box.</param>
        /// <param name="boxHeight">The height of each texture box.</param>
        private void DrawTextureBoxes(float boxWidth, float boxHeight)
        {
            for (int i = 0; i < DataCache.instance.GetImageDataCount(); i++)
            {
                int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
                int colIndex = i % itemsPerRow;

                Rect boxRect = CalculateBoxRect(boxWidth, boxHeight, rowIndex, colIndex);
                Texture2D texture = DataCache.instance.GetImageDataAtIndex(i).texture;

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
        /// Manages interactions with texture boxes, including detecting double-click events to trigger specific actions or selecting images when clicked.
        /// </summary>
        /// <param name="boxRect">The Rect representing the boundaries of the texture box.</param>
        /// <param name="texture">The Texture2D associated with the texture box.</param>
        /// <param name="index">The index of the texture box in the grid.</param>
        private void HandleImageClickEvents(Rect boxRect, Texture2D texture, int index)
        {
            if (IsDoubleClick(boxRect))
            {
                HandleDoubleClickAction(texture, index);
            }
            else if (GUI.Button(boxRect, ""))
            {
                HandleImageSelection(texture, index);
            }
        }

        /// <summary>
        /// Determines whether a double-click event has occurred within the boundaries of a texture box, helping identify when to activate the enlarged view of an image.
        /// </summary>
        /// <param name="boxRect">The Rect representing the boundaries of the texture box.</param>
        /// <returns>True if a double-click event occurred within the texture box; otherwise, false.</returns>
        private bool IsDoubleClick(Rect boxRect)
        {
            return Event.current.type == EventType.MouseDown &&
                   Event.current.clickCount == 2 &&
                   boxRect.Contains(Event.current.mousePosition);
        }

        /// <summary>
        /// Responds to a double-click event by displaying the selected image in an enlarged modal view and marking the modal as open.
        /// </summary>
        /// <param name="texture">The Texture2D to display in the enlarged view.</param>
        /// <param name="index">The index of the selected image in the grid.</param>
        private void HandleDoubleClickAction(Texture2D texture, int index)
        {
            selectedTexture = texture;
            isModalOpen = true;
            Event.current.Use();
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

        private void DrawDownloadAsTextureDetailPanel()
        {
        }

        private void DrawRemoveBackgroundDetailPanel()
        {
            
        }

    }
}