using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using static Models;

public class ModelsUI
{
    private int itemsPerRow = 5;
    private int firstImageIndex;
    private int pageImageCount;
    private float padding = 10f;
    private List<ModelData> pageList = new();
    private List<(Texture2D, string)> textures = new();
    private int selectedTab = 0;
    private bool showNextButton = true;
    private bool showPreviousButton = false;
    private Vector2 scrollPosition = Vector2.zero;

    public void DisableNextButton()
    {
        showNextButton = false;
    }

    public void EnableNextButton()
    {
        showNextButton = true;
    }

    public void SetFirstPage()
    {
        firstImageIndex = 0;
        pageImageCount = 15;

        showPreviousButton = false;
    }

    public void SetNextPage()
    {
        firstImageIndex += pageImageCount;
        if (firstImageIndex > Models.models.Count - pageImageCount)
        {
            firstImageIndex = Models.models.Count - pageImageCount;
        }
        showPreviousButton = true;
    }

    public void SetPreviousPage()
    {
        firstImageIndex -= pageImageCount;
        showPreviousButton = firstImageIndex > 0;
    }

    public void ResetPaginationToken()
    {
        Models.paginationToken = "";
        EditorPrefs.SetString(Models.PaginationTokenKey, Models.paginationToken);
    }

    public void UpdatePage()
    {
        pageList.Clear();
        if (models.Count < pageImageCount)
        {
            pageList.AddRange(models);
            DisableNextButton();
        }
        else
        {
            pageList = Models.models.GetRange(firstImageIndex, pageImageCount);
            if (Models.models.Count - (firstImageIndex + pageImageCount) >= 15)
            {
                EnableNextButton();
            }
            else
            {
                DisableNextButton();
            }
        }
        Models.loadedModels.Clear();
        foreach (var item in pageList)
        {
            Models.loadedModels.Add(item.id);
        }
        
        UpdateTextures();
    }

    public void ResetTabSelection()
    {
        selectedTab = 0;
    }

    public void ClearData()
    {
        textures.Clear();
    }

    public void OnGUI(Rect position)
    {
        DrawBackground(position);

        string[] tabs = { "Private Models", "Public Models" };
        HandleTabSelection(tabs);

        position = DrawModelsGrid(position);

        GUILayout.BeginArea(new Rect(0, position.height - 50, position.width, 50));
        GUILayout.BeginHorizontal();

        if (showPreviousButton)
        {
            EditorStyle.Button("Previous Page", () =>
            {
                RunModelsDataOperation(-1);
            });
        }

        if (showNextButton)
        {
            EditorStyle.Button("Next Page", () =>
            {
                RunModelsDataOperation(1);
            });
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private static void DrawBackground(Rect position)
    {
        Color backgroundColor = EditorStyle.GetBackgroundColor();
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
    }

    private Rect DrawModelsGrid(Rect position)
    {
        float boxWidth = (position.width - padding * (itemsPerRow - 1)) / itemsPerRow;
        float boxHeight = boxWidth;

        int numRows = Mathf.CeilToInt((float)textures.Count / itemsPerRow);
        float rowPadding = 10f;
        float scrollViewHeight = (boxHeight + padding + rowPadding) * numRows - rowPadding;

        scrollPosition = GUI.BeginScrollView(new Rect(0, 70, position.width, position.height - 20), scrollPosition, new Rect(0, 0, position.width - 20, scrollViewHeight));

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter
        };
        
        for (int i = 0; i < textures.Count; i++)
        {
            DrawTextureBox(boxWidth, boxHeight, rowPadding, style, i);
        }

        GUI.EndScrollView();
        return position;
    }

    private void DrawTextureBox(float boxWidth, float boxHeight, float rowPadding, GUIStyle style, int i)
    {
        int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
        int colIndex = i % itemsPerRow;

        Rect boxRect = new Rect(colIndex * (boxWidth + padding), rowIndex * (boxHeight + padding + rowPadding), boxWidth, boxHeight);
        Texture2D texture = textures[i].Item1;
        string name = textures[i].Item2;

        if (texture != null)
        {
            if (GUI.Button(boxRect, texture))
            {
                if (i >= 0 && i < Models.loadedModels.Count)
                {
                    EditorPrefs.SetString("SelectedModelId", Models.loadedModels[i].ToString());
                    EditorPrefs.SetString("SelectedModelName", name);
                }

                EditorWindow window = EditorWindow.GetWindow(typeof(Models));
                window.Close();
            }
            GUI.Label(new Rect(boxRect.x, boxRect.y + boxHeight, boxWidth, 20), name, style);
        }
        else
        {
            GUI.Box(boxRect, "Loading...");
        }
    }

    private void HandleTabSelection(string[] tabs)
    {
        int previousTab = selectedTab;
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        if (previousTab != selectedTab)
        {
            ClearData();

            if (selectedTab == 0)
            {
                Models.SetTab("private");
            }
            else if (selectedTab == 1)
            {
                Models.SetTab("public");
            }
        }
    }

    private void RunModelsDataOperation(int direction)
    {
        GetModelsData(direction);
    }

    private void UpdateTextures()
    {
        textures.Clear();
        
        foreach (var item in pageList)
        {
            string downloadUrl = null;
            
            if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
            {
                downloadUrl = item.thumbnail.url;
            }
            else if (item.trainingImages != null && item.trainingImages.Count > 0)
            {
                downloadUrl = item.trainingImages[0].downloadUrl;
            }

            if (!string.IsNullOrEmpty(downloadUrl))
            {
                CommonUtils.FetchTextureFromURL(downloadUrl, texture =>
                {
                    textures.Add((texture, item.name));
                });
            }
        }
    }
}
