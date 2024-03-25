using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Scenario.Editor.Models;

namespace Scenario.Editor
{
    public class ModelsUI
    {
        /// <summary>
        /// List all models containing inside this window
        /// </summary>
        public List<ModelData> pageModels = new();

        /// <summary>
        /// Global padding apply on element like box, grid.
        /// </summary>
        private float padding = 10f;

        /// <summary>
        /// Number of item displaying per row.
        /// </summary>
        private int itemsPerRow = 5;

        /// <summary>
        /// Starting position inside the list of models
        /// </summary>
        private int firstImageIndex = 0;

        /// <summary>
        /// Maximum of models displaying per page
        /// </summary>
        private int maxModelsPerPage = 15;

        /// <summary>
        /// Default tab selected in the window
        /// </summary>
        private int selectedTab = 0;

        /// <summary>
        /// Allow to display the next button
        /// </summary>
        private bool showNextButton = false;

        /// <summary>
        /// Allow to display the previous button
        /// </summary>
        private bool showPreviousButton = false;

        /// <summary>
        /// Default scroll position
        /// </summary>
        private Vector2 scrollPosition = Vector2.zero;

        /// <summary>
        /// Reference to all tabs available in the window
        /// </summary>
        private string[] tabs = { "Quickstart Models", "Private Models", "Public Models" };

        /// <summary>
        /// Number of pages inside models display.
        /// </summary>
        private int numberPages = 0;

        /// <summary>
        /// Kept the current page active
        /// </summary>
        private int currentPage = 1;

        private void SetFirstPage()
        {
            pageModels.Clear();
            

            showPreviousButton = false;
            showNextButton = false;

            firstImageIndex = 0;
            currentPage = 1;

            UpdatePage();
        }

        private void SetNextPage()
        {
            firstImageIndex += maxModelsPerPage;

            currentPage++;

            var models = GetModels();
        
            if (firstImageIndex + maxModelsPerPage > models.Count)
            {
                showNextButton = false;
            }

            showPreviousButton = true;

            UpdatePage();
        }

        private void SetPreviousPage()
        {
            firstImageIndex -= maxModelsPerPage;

            if (firstImageIndex < maxModelsPerPage)
            {
                firstImageIndex = 0;
                showPreviousButton = false;
            }
            else
            {
                showPreviousButton = true;
            }

            currentPage--;

            UpdatePage();
        }

        private void UpdatePage()
        {
            pageModels.Clear();
        
            var models = GetModels();

            for (int i = firstImageIndex; i < firstImageIndex + maxModelsPerPage; i++)
            {
                if (i > models.Count - 1)
                {
                    continue;
                }

                pageModels.Add(models[i]); 
            }
            numberPages = (models.Count / maxModelsPerPage) + 1;

            if (currentPage < numberPages)
            {
                showNextButton = true;
            }
        }
    
        public void OnGUI(Rect position)
        {
            DrawBackground(position);
            
            HandleTabSelection(tabs);

            DrawModelsGrid(position);

            GUILayout.BeginArea(new Rect(0, position.height - 25, position.width, 50));
            GUILayout.BeginHorizontal();

            if (showPreviousButton)
            {
                EditorStyle.Button($"Previous Page ({currentPage - 1}/{numberPages})", () => { RedrawPage(-1); });
            }

            if (showNextButton)
            {
                if (currentPage == 1)
                {
                    EditorStyle.Button($"Next Page ({currentPage}/{numberPages})", () => { RedrawPage(1); });
                }
                else
                { 
                    EditorStyle.Button($"Next Page ({currentPage+1}/{numberPages})" , () => { RedrawPage(1); });
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void DrawModelsGrid(Rect position)
        {
            float boxWidth = (position.width - (padding * 2) * itemsPerRow) / itemsPerRow;

            float boxHeight = boxWidth;
            var textures = GetTextures();

            int numRows = maxModelsPerPage / itemsPerRow;
            float rowPadding = 10f;
            float scrollViewHeight = (boxHeight + padding + rowPadding) * numRows + rowPadding;

            scrollPosition = GUI.BeginScrollView(new Rect(0, 35, position.width, position.height - 65), scrollPosition, new Rect(-padding*2, 0, position.width - boxWidth/2, scrollViewHeight));
            {
                DrawTextureBox(boxWidth, boxHeight, rowPadding, textures);
            }
            GUI.EndScrollView();
        }

        private void DrawTextureBox(float boxWidth, float boxHeight, float rowPadding, List<TexturePair> textures)
        {
            var models = GetModels();

            for (int i = 0; i < pageModels.Count; i++)
            {

                int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
                int colIndex = i % itemsPerRow;

                var i1 = i;
                var item = textures.FirstOrDefault(x => x.name == pageModels[i1].name);
                if (item == null) { return; }

                Rect boxRect = new Rect(colIndex * (boxWidth + padding), rowIndex * (boxHeight + padding + rowPadding), boxWidth, boxHeight);
                Texture2D texture = item.texture;
                string name = item.name;

                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                };

                if (texture != null)
                {
                    if (GUI.Button(boxRect, texture))
                    {
                        int globalIndex = firstImageIndex + i;
                        if (globalIndex >= 0 && globalIndex < models.Count)
                        {
                            DataCache.instance.SelectedModelId = models[globalIndex].id;
                            DataCache.instance.SelectedModelType = models[globalIndex].type;
                            
                            EditorPrefs.SetString("SelectedModelName", name);
                        }

                        EditorWindow window = EditorWindow.GetWindow(typeof(Models));
                        window.Close();
                    }

                    string modelType = pageModels[i].type;
                    string bubbleText;

                    if (modelType == "sd-xl-composition" || modelType == "sd-xl-lora" || modelType == "sd-xl")
                    {
                        bubbleText = "SDXL";
                    }
                    else if (modelType == "sd-1_5")
                    {
                        bubbleText = "SD1.5";
                    }
                    else
                    {
                        bubbleText = "Unknown";
                    }

                    Rect bubbleRect = new Rect(boxRect.x + boxWidth - 50f, boxRect.y + 10, 40f, 20f);

                    GUIStyle bubbleStyle = new GUIStyle();
                    bubbleStyle.alignment = TextAnchor.MiddleCenter;
                    bubbleStyle.normal.textColor = Color.white;
                    bubbleStyle.normal.background = MakeTex((int)bubbleRect.width, (int)bubbleRect.height, new Color(0.15f, 0.15f, 0.15f, 0.9f));

                    GUI.Box(bubbleRect, bubbleText, bubbleStyle);

                    GUI.Label(new Rect(boxRect.x, boxRect.y + boxHeight, boxWidth, 20), name, style);
                }
                else
                {
                    GUI.Label(new Rect(boxRect.x, boxRect.y + boxHeight, boxWidth, 20), "Loading ...", style);
                }
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        private void HandleTabSelection(string[] tabs)
        {
            int previousTab = selectedTab;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);

            if (previousTab != selectedTab)
            {
                switch (selectedTab)
                { 
                    case 0:
                        SetTab(privacyQuickStart);
                        break;

                    case 1:
                        SetTab(privacyPrivate);
                        break;

                    case 2:
                        SetTab(privacyPublic);
                        break;
                }
            }
        }

        /// <summary>
        /// After fetching model redraw the window
        /// </summary>
        /// <param name="_updateType"></param>
        public void RedrawPage(int _updateType)
        {
            if (IsQuickStartTab())
            {
                selectedTab = 0;
            }
            else
            {
                selectedTab = (IsPrivateTab()) ? 1 : 2;
            }

            switch (_updateType)
            {
                case 0:
                    SetFirstPage();
                    break;
                case 1:
                    SetNextPage();
                    break;
                case -1:
                    SetPreviousPage();
                    break;
            }
        }
    }
}
