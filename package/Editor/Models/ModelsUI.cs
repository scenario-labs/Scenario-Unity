using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Scenario.Editor.Models;

namespace Scenario.Editor
{
    public class ModelsUI
    {
        public List<ModelData> pageModels = new();
        private float padding = 10f;
        private int itemsPerRow = 5;
        private int firstImageIndex = 0;
        private int maxModelsPerPage = 15;
        private int selectedTab = 0;
        private bool showNextButton = false;
        private bool showPreviousButton = false;
        private bool drawTabs = false;
        private Vector2 scrollPosition = Vector2.zero;

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

            selectedTab = (Models.IsPrivateTab()) ? 0 : 1;

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

            var models = Models.GetModels();
        
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
        
            var models = Models.GetModels();

            for (int i = firstImageIndex; i < firstImageIndex + maxModelsPerPage; i++)
            {
                if (i > models.Count - 1)
                {
                    continue;
                }
            
                pageModels.Add(models[i]);
            }
            numberPages = (models.Count / maxModelsPerPage) +1;

            if (currentPage < numberPages)
            {
                showNextButton = true;
            }

            drawTabs = true;
        }
    
        public void OnGUI(Rect position)
        {
            DrawBackground(position);

            if (drawTabs)
            {
                string[] tabs = { "Private Models", "Public Models" };
                HandleTabSelection(tabs);    
            }

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
            var textures = Models.GetTextures();

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
            var models = Models.GetModels();

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
                    alignment = TextAnchor.MiddleCenter
                };

                if (texture != null)
                {
                    if (GUI.Button(boxRect, texture))
                    {
                        int globalIndex = firstImageIndex + i;
                        if (globalIndex >= 0 && globalIndex < models.Count)
                        {
                            DataCache.instance.SelectedModelId = models[globalIndex].id;
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

                    GUIStyle bubbleStyle = new GUIStyle(GUI.skin.box);
                    bubbleStyle.normal.background = MakeTex((int)bubbleRect.width, (int)bubbleRect.height, new Color(0.2f,0.2f,0.2f));

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
                if (selectedTab == 0)
                {
                    Models.SetTab(Models.privacyPrivate);
                    drawTabs = false;
                }
                else if (selectedTab == 1)
                {
                    Models.SetTab(Models.privacyPublic);
                    drawTabs = false;
                }
            }
        }

        public void RedrawPage(int _updateType)
        {
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
