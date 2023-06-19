using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public class PromptImagesUI
{
    public List<Texture2D> textures = new List<Texture2D>();
    public int itemsPerRow = 5;
    public float padding = 10f;

    public Vector2 scrollPosition = Vector2.zero;
    public Texture2D selectedTexture = null;
    public string selectedImageId = null;

    internal PromptImages promptImages;
    private int selectedTextureIndex = 0;

    public void Init(PromptImages promptImg)
    {
        promptImages = promptImg;
    }

    public void OnGUI(Rect position)
    {
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        float maxPreviewWidth = position.width * 0.15f;
        float scrollViewWidth = selectedTexture != null ? position.width * 0.85f : position.width;
        float boxWidth = (scrollViewWidth - padding * (itemsPerRow - 1)) / itemsPerRow;
        float boxHeight = boxWidth;

        int numRows = Mathf.CeilToInt((float)textures.Count / itemsPerRow);
        float scrollViewHeight = Mathf.Max((boxHeight + padding) * numRows, position.height - 20);

        scrollPosition = GUI.BeginScrollView(new Rect(0, 20, scrollViewWidth, position.height - 20), scrollPosition, new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight));


        for (int i = 0; i < textures.Count; i++)
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
                GUI.Box(boxRect, "Loading...");
            }
        }

        GUI.EndScrollView();

        GUILayout.FlexibleSpace();

        if (selectedTexture != null)
        {
            float paddedPreviewWidth = maxPreviewWidth - 2 * padding;
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;
            GUILayout.BeginArea(new Rect(scrollViewWidth, 20, maxPreviewWidth, position.height - 20));
            GUILayout.Label("Selected Image", EditorStyles.boldLabel);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
            GUILayout.Label(selectedTexture, GUILayout.Width(paddedPreviewWidth), GUILayout.Height(paddedPreviewHeight));
            GUILayout.Space(padding);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);

            string[] buttonNames = { "Refine Image", "Download", "Delete" };
            System.Action[] buttonCallbacks = {
                () => {
                    PromptWindowUI.imageUpload = selectedTexture;
                },
                () => {
                    string fileName = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
                    byte[] pngBytes = selectedTexture.EncodeToPNG();
                    promptImages.DownloadImage(fileName, pngBytes);
                },
                () => {
                    promptImages.DeleteImageAtIndex(selectedTextureIndex);
                }
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                if (GUILayout.Button(buttonNames[i], GUILayout.Height(40)))
                {
                    buttonCallbacks[i]();
                }
                GUILayout.Space(padding);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            string[] buttonNames2 = { "Remove Background", "Pixelate Image", "Upscale Image"/*, "Generate More Images"*/ };
            System.Action[] buttonCallbacks2 = {
                () => {
                    promptImages.RemoveBackground(selectedTextureIndex);
                },
                () => {
                    PixelEditorUI.currentImage = selectedTexture;
                    PixelEditorUI.imageData = PromptImages.imageDataList[selectedTextureIndex];
                    PixelEditor.ShowWindow();
                },
                () => {
                    UpscaleEditorUI.currentImage = selectedTexture;
                    UpscaleEditorUI.imageData = PromptImages.imageDataList[selectedTextureIndex];
                    UpscaleEditor.ShowWindow();
                }/*,
                () => {
                    // Assuming that selectedTexture is of type Texture2D
                    PromptWindowUI.imageUpload = selectedTexture;
                },
                () => {
                    // TODO: Implement generate more images functionality
                }*/
            };

            for (int i = 0; i < buttonNames2.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(padding);

                if (GUILayout.Button(buttonNames2[i], GUILayout.Height(40)))
                {
                    buttonCallbacks2[i]();
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.Space(10);

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.EndArea();
        }
    }
}