using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using static ImageDataStorage;

public class ImagesUI
{
    public List<Texture2D> textures = new List<Texture2D>();
    public int itemsPerRow = 5;
    public float padding = 10f;

    private Vector2 scrollPosition = Vector2.zero;
    private Texture2D selectedTexture = null;

    internal Images images;
    public int selectedTextureIndex = 0;

    public int firstImageIndex = 0;
    public int pageImageCount = 15;
    public List<ImageData> pageList;

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
        pageList = ImageDataStorage.imageDataList.GetRange(firstImageIndex, pageImageCount);
        UpdateTextures();
    }

    public async void UpdateTextures()
    {
        textures.Clear();
        foreach (var item in pageList)
        {
            Texture2D texture = await Images.LoadTexture(item.Url);
            textures.Add(texture);
        }
    }

    public void ClearSelectedTexture()
    {
        selectedTexture = null;
    }

    public void OnGUI(Rect position)
    {
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        float previewWidth = 309f;
        float scrollViewWidth = selectedTexture != null ? position.width - previewWidth : position.width;
        float boxWidth = (scrollViewWidth - padding * (itemsPerRow - 1)) / itemsPerRow;
        float boxHeight = boxWidth;

        int numRows = Mathf.CeilToInt((float)textures.Count / itemsPerRow);

        float scrollViewHeight = (boxHeight + padding) * numRows;

        scrollPosition = GUI.BeginScrollView(new Rect(0, 20, scrollViewWidth, position.height - 70), scrollPosition, new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight));

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

        GUILayout.BeginArea(new Rect(0, position.height - 50, scrollViewWidth, 50));
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
        GUILayout.EndArea();

        GUILayout.FlexibleSpace();

        if (selectedTexture != null)
        {
            GUILayout.BeginArea(new Rect(scrollViewWidth, 20, previewWidth, position.height - 20));
            GUILayout.Label("Selected Image", EditorStyles.boldLabel);
            GUILayout.Space(10);
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float paddedPreviewWidth = previewWidth - 2 * padding;
            float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
            GUILayout.Label(selectedTexture, GUILayout.Width(paddedPreviewWidth), GUILayout.Height(paddedPreviewHeight));
            GUILayout.Space(padding);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);

            string[] buttonNames1 = { "Refine Image", "Download", "Delete" };
            System.Action[] buttonCallbacks1 = {
                () => {
                    PromptWindowUI.imageUpload = selectedTexture;
                },
                () => {
                    string fileName = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
                    byte[] pngBytes = selectedTexture.EncodeToPNG();
                    images.DownloadImage(fileName, pngBytes);
                },
                () => {
                    images.DeleteImageAtIndex(selectedTextureIndex + firstImageIndex);
                    selectedTexture = null;
                }
            };

            for (int i = 0; i < buttonNames1.Length; i++)
            {
                if (GUILayout.Button(buttonNames1[i], GUILayout.Height(40)))
                {
                    buttonCallbacks1[i]();
                }

                GUILayout.Space(padding);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            string[] buttonNames2 = { "Remove Background", "Pixelate Image", "Upscale Image"/*, "Generate More Images"*/ };
            System.Action[] buttonCallbacks2 = {
                () => {
                    images.RemoveBackground(selectedTextureIndex + firstImageIndex);
                },
                () => {
                    PixelEditorUI.currentImage = selectedTexture;
                    PixelEditorUI.imageData = ImageDataStorage.imageDataList[selectedTextureIndex + firstImageIndex];
                    PixelEditor.ShowWindow();
                },
                () => {
                    UpscaleEditorUI.currentImage = selectedTexture;
                    UpscaleEditorUI.imageData = ImageDataStorage.imageDataList[selectedTextureIndex + firstImageIndex];
                    UpscaleEditor.ShowWindow();
                }/*,
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

                GUILayout.Space(padding);
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