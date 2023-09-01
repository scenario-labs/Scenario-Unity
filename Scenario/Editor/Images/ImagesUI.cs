using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using static ImageDataStorage;

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
        pageList = ImageDataStorage.imageDataList.GetRange(firstImageIndex, pageImageCount);
        FetchPageTextures();
    }

    public async void FetchPageTextures()
    {
        textures.Clear();
        foreach (var item in pageList)
        {
            Texture2D texture = await CommonUtils.FetchTextureFromURLAsync(item.Url);
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

        scrollPosition = GUI.BeginScrollView(new Rect(0, 20, scrollViewWidth, position.height - 70), scrollPosition,
            new Rect(0, 0, scrollViewWidth - 20, scrollViewHeight));

        for (int i = 0; i < textures.Count; i++)
        {
            DrawTextureButton(boxWidth, boxHeight, i);
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

        DrawSelectedTextureSection(position, previewWidth, scrollViewWidth);
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
        GUILayout.Space(10);
        GUILayout.BeginVertical();
        {
            DrawFirstButtons();
            GUILayout.Space(10);
            DrawSecondButtons();
            GUILayout.Space(10);
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
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
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(padding);

                if (GUILayout.Button(buttonNames[i], GUILayout.Height(40)))
                {
                    buttonCallbacks[i]();
                }

                GUILayout.Space(padding);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }

    private void DrawFirstButtons()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(padding);

            string[] buttonNames = { "Refine Image", "Download", "Delete" };
            System.Action[] buttonCallbacks =
            {
                () => PromptWindowUI.imageUpload = selectedTexture,
                () => CommonUtils.SaveTextureAsPNG(selectedTexture),
                () =>
                {
                    images.DeleteImageAtIndex(selectedTextureIndex + firstImageIndex);
                    selectedTexture = null;
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
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSelectedImage(float previewWidth)
    {
        GUILayout.Label("Selected Image", EditorStyles.boldLabel);

        GUILayout.Space(10);

        float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
        float paddedPreviewWidth = previewWidth - 2 * padding;
        float paddedPreviewHeight = paddedPreviewWidth / aspectRatio;

        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(padding);
            GUILayout.Label(selectedTexture, GUILayout.Width(paddedPreviewWidth),
                GUILayout.Height(paddedPreviewHeight));
            GUILayout.Space(padding);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawTextureButton(float boxWidth, float boxHeight, int i)
    {
        int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
        int colIndex = i % itemsPerRow;

        Rect boxRect = new Rect(colIndex * (boxWidth + padding), rowIndex * (boxHeight + padding), boxWidth, boxHeight);
        Texture2D texture = textures[i];

        if (texture == null)
        {
            GUI.Box(boxRect, "Loading...");
        }
        else
        {
            if (GUI.Button(boxRect, ""))
            {
                selectedTexture = texture;
                selectedTextureIndex = i;
            }

            GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
        }
    }
}