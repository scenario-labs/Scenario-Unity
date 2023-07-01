using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using static Models;

public class ModelsUI
{
    public List<(Texture2D, string)> textures = new List<(Texture2D, string)>();
    public int itemsPerRow = 5;
    public float padding = 10f;

    private Vector2 scrollPosition = Vector2.zero;
    private int selectedTab = 0;

    public int firstImageIndex;
    public int pageImageCount;
    public List<ModelData> pageList = new List<ModelData>();

    private bool showNextButton = true;
    private bool showPreviousButton = false;

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
         if (firstImageIndex <= 0)
        {
            showPreviousButton = false;
        }
        else
        {
            showPreviousButton = true;
        }
    }

    public void ResetPaginationToken()
    {
        Models.paginationToken = "";
        EditorPrefs.SetString(Models.paginationTokenKey, Models.paginationToken);
    }

    public async Task UpdatePage()
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
                EnableNextButton();
            else
                DisableNextButton();
        }
        Models.loadedModels.Clear();
        foreach (var item in pageList)
        {
            Models.loadedModels.Add(item.id);
        }
        await UpdateTextures();
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
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        string[] tabs = new string[] { "Private Models", "Public Models" };
        int previousTab = selectedTab;
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        if (previousTab != selectedTab)
        {
            ClearData();

            if (selectedTab == 0)
            {
                Models.ShowWindow("private");
            }
            else if (selectedTab == 1)
            {
                Models.ShowWindow("public");
            }
        }

        float boxWidth = (position.width - padding * (itemsPerRow - 1)) / itemsPerRow;
        float boxHeight = boxWidth;

        int numRows = Mathf.CeilToInt((float)textures.Count / itemsPerRow);
        float rowPadding = 10f;
        float scrollViewHeight = (boxHeight + padding + rowPadding) * numRows - rowPadding;

        scrollPosition = GUI.BeginScrollView(new Rect(0, 70, position.width, position.height - 20), scrollPosition, new Rect(0, 0, position.width - 20, scrollViewHeight));

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        for (int i = 0; i < textures.Count; i++)
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

        GUI.EndScrollView();

        GUILayout.BeginArea(new Rect(0, position.height - 50, position.width, 50));
        GUILayout.BeginHorizontal();

        if (showPreviousButton && GUILayout.Button("Previous Page"))
        {
            RunModelsDataOperation(-1);
        }

        if (showNextButton && GUILayout.Button("Next Page"))
        {
            RunModelsDataOperation(1);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private async void RunModelsDataOperation(int direction)
    {
        await Models.GetModelsData(direction).ConfigureAwait(false);
    }

    public async Task UpdateTextures()
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
                Texture2D texture = await Models.LoadTexture(downloadUrl);
                textures.Add((texture, item.name));
            }
        }
    }
}
