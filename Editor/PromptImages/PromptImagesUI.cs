using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class PromptImagesUI
    {
        public int itemsPerRow = 5;
        public float padding = 10f;
        public Vector2 scrollPosition = Vector2.zero;
        public Texture2D selectedTexture = null;
        public string selectedImageId = null;
        internal PromptImages promptImages;
        private int selectedTextureIndex = 0;
        private bool isModalOpen = false;
        
        public void Init(PromptImages promptImg)
        {
            promptImages = promptImg;
        }
    
        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        public void OnGUI(Rect position)
        {
            DrawBackground(position);

            if (GUILayout.Button("Clear"))
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
            var scrollViewRect = new Rect(0, 20, scrollViewWidth, position.height - 70);
            var viewRect = new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight);
            
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition,viewRect);
            {
                DrawTextureBoxes(boxWidth, boxHeight);
            }
            GUI.EndScrollView();

            GUILayout.FlexibleSpace();

            if (isModalOpen)
            {
                DrawImageModal(new Rect(0, 0, position.width, position.height));
            }
        
            DrawSelectedTextureSection(position, previewWidth, scrollViewWidth);
        }

        private void DrawSelectedTextureSection(Rect position, float previewWidth, float scrollViewWidth)
        {
            if (selectedTexture == null)
            {
                return;
            }

            if (DataCache.instance.GetImageDataCount() <= 0)
            {
                return;
            }
        
            float paddedPreviewWidth = previewWidth - 2 * padding;
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;

            GUILayout.BeginArea(new Rect(scrollViewWidth, 20, previewWidth, position.height - 20));
            {
                DrawScrollableArea(previewWidth);
            }
            GUILayout.EndArea();
        }
    
        private void DrawScrollableArea(float previewWidth)
        {
            DrawSelectedImage(previewWidth);
            CustomStyle.Space(10);
            GUILayout.BeginVertical();
            {
                DrawFirstButtons();
                CustomStyle.Space(10);
                DrawSecondButtons();
                CustomStyle.Space(10);
                DrawImageData();
            }
            GUILayout.EndVertical();
            CustomStyle.Space(10);
        }

        private void DrawSelectedImage(float previewWidth)
        {
            GUILayout.Label("Selected Image", EditorStyles.boldLabel);

            CustomStyle.Space(10);

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

        private void DrawFirstButtons()
        {
            string[] buttonNames = { "Refine Image", "Download", "Delete" };
            System.Action[] buttonCallbacks =
            {
                () => PromptWindowUI.imageUpload = selectedTexture,
                () => CommonUtils.SaveTextureAsPNG(selectedTexture),
                () =>
                {
                    promptImages.DeleteImageAtIndex(selectedTextureIndex);
                    selectedTexture = null;
                }
            };

            GUILayout.BeginHorizontal();
            for (int i = 0; i < buttonNames.Length; i++)
            {
                if (GUILayout.Button(buttonNames[i], GUILayout.Height(40)))
                {
                    buttonCallbacks[i]();
                }

                // Add spacing between buttons but not after the last button
                if (i < buttonNames.Length - 1)
                {
                    CustomStyle.Space(10);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSecondButtons()
        {
            string[] buttonNames = { "Remove Background", "Pixelate Image", "Upscale Image" /*, "Generate More Images"*/ };
            System.Action[] buttonCallbacks =
            {
                () => promptImages.RemoveBackground(selectedTextureIndex),
                () => PixelEditor.ShowWindow(selectedTexture, DataCache.instance.GetImageDataAtIndex(selectedTextureIndex)),
                () => UpscaleEditor.ShowWindow(selectedTexture, DataCache.instance.GetImageDataAtIndex(selectedTextureIndex)) /*,
                () => {
                    // TODO: Implement generate more images functionality
                }*/
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(buttonNames[i], GUILayout.Height(40)))
                {
                    buttonCallbacks[i]();
                }
                GUILayout.EndHorizontal();
                if (i < buttonNames.Length - 1)
                {
                    CustomStyle.Space(10);
                }
            }
        }

        private void DrawImageModal(Rect position)
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

        private void DrawTextureBoxes(float boxWidth, float boxHeight)
        {
            for (int i = 0; i < DataCache.instance.GetImageDataCount(); i++)
            {
                int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
                int colIndex = i % itemsPerRow;

                Rect boxRect = new Rect(colIndex * (boxWidth + padding), rowIndex * (boxHeight + padding), boxWidth, boxHeight);
                Texture2D texture = DataCache.instance.GetImageDataAtIndex(i).texture;

                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                
                if (texture != null)
                {
                    // Detect double click
                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && boxRect.Contains(Event.current.mousePosition))
                    {
                        selectedTexture = texture;
                        isModalOpen = true;
                        Event.current.Use();
                        return; // Exit early since we've handled the double-click
                    }
                
                    if (GUI.Button(boxRect, ""))
                    {
                        selectedTexture = texture;
                        selectedTextureIndex = i;
                    }

                    GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.color = Color.white;
                    GUI.Label(boxRect, "Loading...", style);
                    //var url = DataCache.instance.GetImageDataAtIndex(i).Url;
                    /*if (PromptImages.cancelUrls.Contains(url))
                    {
                        GUI.color = Color.white;
                        GUI.Label(boxRect, "Cancelling...", style);
                    }
                    else
                    {
                        
                        var buttonRect = boxRect;
                        buttonRect.x += buttonRect.width / 4;
                        buttonRect.y += buttonRect.height - 20;
                        buttonRect.width /= 2;
                        buttonRect.height /= 4;
                        if (GUI.Button(buttonRect,"Cancel"))
                        {
                            Debug.Log($"Cancel {i}");
                            DataCache.instance.CancelUrlAtIndex(i);
                        }
                    }*/
                }
            }
        }
    }
}