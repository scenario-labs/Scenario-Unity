using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Scenario.ImageDataStorage;

namespace Scenario
{
    public class ImagesUI
    {
        public List<Texture2D> textures = new();

        private int itemsPerRow = 5;
        private float padding = 10f;

        private Vector2 scrollPosition = Vector2.zero;
        private Texture2D selectedTexture = null;

        private Images images;
        private int selectedTextureIndex = 0;

        private int firstImageIndex = 0;
        private int pageImageCount = 15;
        private List<ImageData> pageList;

        public void Init(Images img)
        {
            images = img;
        }

        public void SetFirstPage()
        {
            firstImageIndex = 0;
            pageImageCount = 15;
        }

        public void SetNextPage()
        {
            firstImageIndex += pageImageCount;
            if (firstImageIndex >= ImageDataStorage.imageDataList.Count - pageImageCount)
            {
                firstImageIndex -= pageImageCount;
            }
        }

        public void SetPreviousPage()
        {
            firstImageIndex -= pageImageCount;
            if (firstImageIndex <= 0)
            {
                firstImageIndex = 0;
            }
        }

        public void UpdatePage()
        {
            pageList = ImageDataStorage.imageDataList
                    .GetRange(firstImageIndex, pageImageCount)
                    .OrderByDescending(x => x.CreatedAt) // Sort by newest first
                    .ToList();
            FetchPageTextures();
        }

        private void FetchPageTextures()
        {
            textures.Clear();
            foreach (var item in pageList)
            {
                CommonUtils.FetchTextureFromURL(item.Url, texture =>
                {
                    textures.Add(texture);
                });
            }
        }

        public void ClearSelectedTexture()
        {
            selectedTexture = null;
        }

        public void OnGUI(Rect position)
        {
            DrawBackground(position);

            float previewWidth = 309f;
            float scrollViewWidth = selectedTexture != null ? position.width - previewWidth : position.width;
            float boxWidth = (scrollViewWidth - padding * (itemsPerRow - 1)) / itemsPerRow;
            float boxHeight = boxWidth;

            int numRows = Mathf.CeilToInt((float)textures.Count / itemsPerRow);

            float scrollViewHeight = (boxHeight + padding) * numRows;

            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, scrollViewWidth, position.height - 70), scrollPosition,
                new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight));

            for (int i = 0; i < textures.Count; i++)
            {
                DrawTextureButton(boxWidth, boxHeight, i);
            }

            GUI.EndScrollView();

            GUILayout.BeginArea(new Rect(0, position.height - 50, scrollViewWidth, 50));
            {
                GUILayout.BeginHorizontal();
                if (firstImageIndex > 0 && GUILayout.Button("Previous Page"))
                {
                    SetPreviousPage();
                    UpdatePage();
                }
                if (firstImageIndex < imageDataList.Count - pageImageCount && GUILayout.Button("Next Page"))
                {
                    SetNextPage();
                    UpdatePage();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            GUILayout.FlexibleSpace();

            DrawSelectedTextureSection(position, previewWidth, scrollViewWidth);
        }

        private void DrawImageData()
        {
            var currentImageData = PromptImages.imageDataList[selectedTextureIndex];
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

        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void DrawSelectedTextureSection(Rect position, float previewWidth, float scrollViewWidth)
        {
            if (selectedTexture == null)
            {
                return;
            }

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

        private void DrawSecondButtons()
        {
            string[] buttonNames = { "Remove Background", "Pixelate Image", "Upscale Image" /*, "Generate More Images"*/ };
            System.Action[] buttonCallbacks =
            {
                () => images.RemoveBackgroundForImageAtIndex(selectedTextureIndex + firstImageIndex),
                () => PixelEditor.ShowWindow(selectedTexture, ImageDataStorage.imageDataList[selectedTextureIndex + firstImageIndex]),
                () => UpscaleEditor.ShowWindow(selectedTexture, ImageDataStorage.imageDataList[selectedTextureIndex + firstImageIndex]) /*,
            () => {
                // TODO: Implement generate more images functionality
            }*/
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                if (GUILayout.Button(buttonNames[i], GUILayout.Height(40)))
                {
                    buttonCallbacks[i]();
                }
                CustomStyle.Space(10);
            }
        }

        public void RefineImage()
        {
            if (PromptWindow.promptWindowUI == null)
            {
                Debug.LogError("Prompt Window not found: Open Prompt Window, and try again.");
                return;
            }
            var imageData = ImageDataStorage.imageDataList[selectedTextureIndex + firstImageIndex];
            PromptWindowUI.imageUpload = selectedTexture;
            PromptWindow.promptWindowUI.isImageToImage = true;
            PromptWindow.promptWindowUI.isTextToImage = false;
            PromptWindow.promptWindowUI.promptinputText = imageData.Prompt;
            PromptWindow.promptWindowUI.tags = imageData.Prompt.Split(',').ToList();
            PromptWindow.promptWindowUI.samplesliderValue = imageData.Steps;
            PromptWindow.promptWindowUI.widthSliderValue = (int)imageData.Size.x;
            PromptWindow.promptWindowUI.heightSliderValue = (int)imageData.Size.y;
            PromptWindow.promptWindowUI.guidancesliderValue = imageData.Guidance;
            PromptWindow.promptWindowUI.seedinputText = imageData.Seed;
            PromptWindow.ShowWindow();
        }

        private void DrawFirstButtons()
        {
            string[] buttonNames = { "Refine Image", "Download", "Delete" };
            System.Action[] buttonCallbacks =
            {
                () => RefineImage(),
                () => CommonUtils.SaveTextureAsPNG(selectedTexture),
                () =>
                {
                    images.DeleteImageAtIndex(selectedTextureIndex + firstImageIndex);
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
                    CustomStyle.Space(5);
                }
            }
            GUILayout.EndHorizontal();
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

        private void DrawTextureButton(float boxWidth, float boxHeight, int i)
        {
            int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
            int colIndex = i % itemsPerRow;

            Rect boxRect = new Rect(colIndex * (boxWidth + padding), rowIndex * (boxHeight + padding), boxWidth, boxHeight);
            Texture2D texture = textures[i];

            if (texture != null)
            {
                if (GUI.Button(boxRect, ""))
                {
                    selectedTexture = texture;
                    selectedTextureIndex = i;
                }

                GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(new Rect(boxRect.x, boxRect.y + boxHeight, boxWidth, 20), "Loading..");
            }
        }
    }
}