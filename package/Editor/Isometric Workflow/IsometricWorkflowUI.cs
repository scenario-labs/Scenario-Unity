using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using static Scenario.Editor.IsometricWorkflowSettings;
using static Scenario.Editor.IsometricWorkflow;

namespace Scenario.Editor
{
    public class IsometricWorkflowUI
    {
        #region Public Fields
        #endregion

        #region Private Fields

        private static IsometricWorkflow isometricWorkflow;
        
        /// <summary>
        /// Using no base to generate isometric tile 
        /// </summary>
        private bool baseNone = false;

        /// <summary>
        /// Using square base to generate isometric tile 
        /// </summary>
        private bool baseSquare = true;

        /// <summary>
        /// Using custom base to generate isometric tile 
        /// </summary>
        private bool baseCustom = false;
        
        /// <summary>
        /// When the workflow use the api to know the cost and also generate.
        /// </summary>
        private bool isProcessing = false;

        /// <summary>
        /// if the user choose a custom texture as reference at Step 1
        /// </summary>
        internal Texture2D customTexture = null;

        /// <summary>
        /// The scrollview at step 4
        /// </summary>
        private Vector2 assetScrollView = Vector2.zero;

        /// <summary>
        /// The scrollview at step 5
        /// </summary>
        private Vector2 validationScrollView = Vector2.zero;

        /// <summary>
        /// The current value of the input field when the user wants to add an asset to the list
        /// </summary>
        private string inputAssetName = string.Empty;

        /// <summary>
        /// On fourth step, we send all requests to the API to generate all the image. This is TRUE only when all inferences has been requested
        /// </summary>
        private RequestsStatus requestStatus = RequestsStatus.NotRequested;

        /// <summary>
        /// Default background color.
        /// </summary>
        private Color defaultBackgroundColor;

        /// <summary>
        /// Dictionary to contain each behaviour of each line item.
        /// </summary>
        private Dictionary<string, Action> drawActionPanels = new Dictionary<string, Action>();

        /// <summary>
        /// Number of model item expected on a row.
        /// </summary>
        private int numberItemPerRow = 3;

        #endregion

        public void Init(IsometricWorkflow _isometricWorkflow)
        {
            isometricWorkflow = _isometricWorkflow;
            requestStatus = RequestsStatus.NotRequested;
            defaultBackgroundColor = GUI.backgroundColor;
        }

        /// <summary>
        /// Draws the background of the UI element with the specified position.
        /// This function fills the background of a UI element with the start screen file.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        private void DrawImageBackground(Rect position)
        {
            if (isometricWorkflow != null)
            {
                if (isometricWorkflow.IsometricStartScreen != null)
                {
                    GUI.DrawTexture(new Rect(0, 0, position.width, position.height), isometricWorkflow.IsometricStartScreen);
                }
                else
                {
                    DrawBackground(position);
                }
            }
            else
            { 
                DrawBackground(position);
            }
        }

        /// <summary>
        /// Draws the background of the UI element with the specified position.
        /// This function fills the background of a UI element with a given color.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        private void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        #region Draw UI

        /// <summary>
        /// This function is responsible for rendering the interface for the start screen.
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawStartGUI(Rect _dimension)
        {
            DrawImageBackground(_dimension);

            GUILayout.FlexibleSpace();
            //Bottom
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                CustomStyle.ButtonPrimary("Next", 30, 150, () =>
                {
                    isometricWorkflow.currentStep = IsometricWorkflow.Step.Base;
                });
            }
            GUILayout.EndHorizontal();
            CustomStyle.Space();
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the first step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawBaseGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 1. Choose a Base", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Label($"A base is crucial for aligning the asset with the isometric grid. You can use the default base or select an image of your choice. If you do not select a base, the images may have slight variations in alignment with the grid.", 12, TextAnchor.UpperLeft, bold: false);
            CustomStyle.Space(50);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                //None // When the base non will be available once more.
                /*GUILayout.BeginVertical();
                {
                    if (!baseNone)
                    { 
                        CustomStyle.Space(45);
                        baseNone = GUILayout.Toggle(baseNone, "", GUILayout.Height(10));
                        CustomStyle.Space(45);
                    }

                    if (baseNone)
                    {
                        CustomStyle.Space(45);
                        GUILayout.Toggle(baseNone, "", GUILayout.Height(10));
                        CustomStyle.Space(45);
                        isometricWorkflow.selectedBase = IsometricWorkflow.Base.None;
                        baseSquare = false;
                        baseCustom = false;
                    }
                }
                GUILayout.EndVertical();
                CustomStyle.Space(-25);
                GUILayout.BeginVertical();
                {
                    GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(100));
                    CustomStyle.Label("None", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                //Space
                GUILayout.FlexibleSpace();*/

                //Square
                GUILayout.BeginVertical();
                {
                    if (!baseSquare)
                    { 
                        CustomStyle.Space(45);
                        baseSquare = GUILayout.Toggle(baseSquare, "", GUILayout.Height(10));
                        CustomStyle.Space(45);
                    }
                    if(baseSquare)
                    {
                        CustomStyle.Space(45);
                        GUILayout.Toggle(true, "", GUILayout.Height(10));
                        CustomStyle.Space(45);
                        isometricWorkflow.selectedBase = IsometricWorkflow.Base.Square;
                        baseNone = false;
                        baseCustom = false;
                    }
                }
                GUILayout.EndVertical();
                CustomStyle.Space(-25);
                GUILayout.BeginVertical();
                {
                    if (IsometricWorkflow.settings != null && IsometricWorkflow.settings.squareBaseTexture != null)
                    { 
                        GUILayout.Box(IsometricWorkflow.settings.squareBaseTexture, GUILayout.Width(100), GUILayout.Height(100));
                    }
                    CustomStyle.Label("Square", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                //Space
                GUILayout.FlexibleSpace();

                //Custom
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            if (customTexture == null)
                            {
                                CustomStyle.Space(45);
                                GUILayout.Toggle(baseCustom, "", GUILayout.Height(10));
                                CustomStyle.Space(45);
                                baseCustom = false;
                            }
                            else
                            {
                                if (!baseCustom)
                                { 
                                    CustomStyle.Space(45);
                                    baseCustom = GUILayout.Toggle(baseCustom, "", GUILayout.Height(10));
                                    CustomStyle.Space(45);
                                }
                            }

                            if (baseCustom)
                            {
                                CustomStyle.Space(45);
                                GUILayout.Toggle(baseCustom, "", GUILayout.Height(10));
                                CustomStyle.Space(45);

                                isometricWorkflow.selectedBase = IsometricWorkflow.Base.Custom;
                                baseNone = false;
                                baseSquare = false;
                            }
                        }
                        GUILayout.EndVertical();

                        CustomStyle.Space(-25);
                
                        customTexture = (Texture2D)EditorGUILayout.ObjectField(customTexture, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
                        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(customTexture));
                        if (customTexture != null)
                        {
                            importer.isReadable = true;
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            EditorUtility.SetDirty(importer);
                            if (AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath))
                            {
                                importer.SaveAndReimport();
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    CustomStyle.Label("Custom Isometric Base", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            if (!baseNone && !baseSquare && !baseCustom)
            {
                baseNone = true;
            }

            //Bottom
            GUILayout.FlexibleSpace();
            CustomStyle.ButtonPrimary("Next", 30, 0, () =>
            {
                isometricWorkflow.currentStep = IsometricWorkflow.Step.Style;
            });
            CustomStyle.Space();
        }


        /// <summary>
        /// This function is responsible for rendering the interface for the second step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawStyleGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 2. Choose a Style", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Label($"Choose from a variety of 2D and 3D styles, with building shapes that vary according to each style.", 12, TextAnchor.UpperLeft, bold: false);
            CustomStyle.Space(50);

            GUILayout.BeginVertical(); // Begin vertical grouping
            {
                assetScrollView = GUILayout.BeginScrollView(assetScrollView, GUILayout.ExpandWidth(true));
                {
                    var modelStyles = Instance.GetModelsIdByStyle();
                    if (isometricWorkflow.selectedModel == null)
                    {
                        if (modelStyles != null && modelStyles.Count > 0)
                        { 
                            isometricWorkflow.selectedModel = modelStyles[0];
                        }
                    }

                    int selected = (int)isometricWorkflow.selectedModel.style;

                    int countModel = 0;

                    GUILayout.BeginHorizontal(); // Organize in rows
                    {
                        GUILayout.FlexibleSpace();
                        foreach (ModelIdByStyle modelStyle in modelStyles)
                        {
                            CustomStyle.Space(25); // Space between each containers
                            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(150), GUILayout.Height(175)); // Container for each item
                            {
                                GUILayout.BeginVertical();
                                {
                                    // Thumbnail
                                    if (IsometricWorkflow.settings.isometricModelThumbnails.Exists(x => x.style == modelStyle.style))
                                    {
                                        bool isSelected = GUILayout.Toggle(selected == (int)modelStyle.style, IsometricWorkflow.settings.isometricModelThumbnails.Find(x => x.style == modelStyle.style).thumbnail, GUILayout.Width(150), GUILayout.Height(150)); // Adjust size as needed

                                        if (isSelected && selected != (int)modelStyle.style)
                                        {
                                            isometricWorkflow.selectedModel = modelStyle; // Update the selected model
                                            selected = (int)modelStyle.style;
                                        }
                                    }

                                    // Name
                                    if (string.IsNullOrEmpty(modelStyle.Name))
                                    {
                                        CustomStyle.Label(modelStyle.style.ToString(), width: 150, alignment: TextAnchor.MiddleCenter); // Centered text under the thumbnail
                                    }
                                    else
                                    { 
                                        CustomStyle.Label(modelStyle.Name.ToString(), width: 150, alignment: TextAnchor.MiddleCenter); // Centered text under the thumbnail
                                    }
                                }
                                GUILayout.EndVertical();

                            }
                            GUILayout.EndHorizontal();
                            countModel++;
                            if (countModel == numberItemPerRow) // Assuming you want a specific number of items per row
                            {
                                GUILayout.FlexibleSpace(); //flexible space at the right side
                                GUILayout.EndHorizontal();

                                CustomStyle.Space(25); // Space between each containers

                                GUILayout.BeginHorizontal(); // Start a new row after every a specific number of items items
                                GUILayout.FlexibleSpace(); //flexible space at the left side
                                countModel = 0;
                            }
                        }
                    }
                    GUILayout.FlexibleSpace(); //flexible space at the right side
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            //Bottom
            GUILayout.BeginHorizontal();
            { 
                //GUILayout.FlexibleSpace();
                CustomStyle.ButtonPrimary("Previous", 30, 0, () =>
                {
                    isometricWorkflow.currentStep = IsometricWorkflow.Step.Base;
                });
                CustomStyle.ButtonPrimary("Next", 30, 0, () =>
                {
                    DataCache.instance.SelectedModelId = isometricWorkflow.selectedModel.id;
                    EditorPrefs.SetString("postedModelName", DataCache.instance.SelectedModelId);
                    isometricWorkflow.currentStep = IsometricWorkflow.Step.Theme;
                });
            }
            GUILayout.EndHorizontal();
            CustomStyle.Space();
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the third step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawThemeGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 3. Choose a Theme", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Label($"Themes define the era and help create more detail and context for the images. Choose a theme or add a custom one.", 12, TextAnchor.UpperLeft, bold: false);
            CustomStyle.Space(25);

            GUILayout.BeginHorizontal();
            {
                CustomStyle.Space(25);
                GUILayout.BeginVertical(); // Begin vertical grouping
                {
                    var themes = Enum.GetValues(typeof(IsometricWorkflow.Theme));
                    int selected = (int)isometricWorkflow.selectedTheme;
                    bool isSelected = false;

                    foreach (IsometricWorkflow.Theme theme in themes)
                    {
                        CustomStyle.Space(10); // Space between each toggles
                        if (theme == Theme.Other)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                isSelected = GUILayout.Toggle(selected == (int)theme, theme.ToString().Replace("_", " ") + ":");
                                isometricWorkflow.customTheme = GUILayout.TextField(isometricWorkflow.customTheme, GUILayout.Height(25), GUILayout.MaxWidth(100));
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        { 
                            isSelected = GUILayout.Toggle(selected == (int)theme, theme.ToString().Replace("_", " "));
                        }

                        if (isSelected && selected != (int)theme)
                        {
                            isometricWorkflow.selectedTheme = theme; // Update the selected theme
                            selected = (int)theme;
                        }
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            //Bottom
            GUILayout.BeginHorizontal();
            {
                //GUILayout.FlexibleSpace();
                CustomStyle.ButtonPrimary("Previous", 30, 0, () =>
                {
                    isometricWorkflow.currentStep = IsometricWorkflow.Step.Style;
                });
                CustomStyle.ButtonPrimary("Next", 30, 0, () =>
                {
                    isometricWorkflow.currentStep = IsometricWorkflow.Step.Asset;
                });
            }
            GUILayout.EndHorizontal();
            CustomStyle.Space();
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the fourth step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawAssetsGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 4. Select any number of assets you wish to create.", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Label($"Add the assets you want to create and click the + button to add them to the list. Besides naming the building, you can also add more details to the prompt at this step.", 12, TextAnchor.UpperLeft, bold: false);
            CustomStyle.Space(25);

            GUILayout.BeginHorizontal();
            {
                CustomStyle.Space();
                GUILayout.BeginVertical(GUI.skin.box); // Begin vertical grouping
                {
                    assetScrollView = GUILayout.BeginScrollView(assetScrollView, GUILayout.ExpandWidth(true));
                    {
                        if (isometricWorkflow.assetList != null && isometricWorkflow.assetList.Count > 0)
                        { 
                            bool removing = false;
                            string toRemove = string.Empty;
                            foreach (string assetName in isometricWorkflow.assetList)
                            {
                                CustomStyle.Space();
                                GUILayout.BeginHorizontal();
                                {
                                    CustomStyle.Label(assetName, alignment: TextAnchor.MiddleLeft, height: 30, fontSize: 16, bold: true);
                                    GUILayout.FlexibleSpace();
                                    if (CommonIcons.GetIcon(CommonIcons.Icon.wastebasket) != null)
                                    { 
                                        if (GUILayout.Button(CommonIcons.GetIcon(CommonIcons.Icon.wastebasket), GUILayout.Width(30), GUILayout.Height(30)))
                                        {
                                            removing = true;
                                            toRemove = assetName;
                                        }
                                    }
                                }
                                GUILayout.EndHorizontal();
                                CustomStyle.Space();
                            }
                            if (removing)
                            { 
                                isometricWorkflow.assetList.Remove(toRemove);
                                removing = false;
                            }
                        }
                    }
                    GUILayout.EndScrollView();//end scroll view

                    GUILayout.BeginHorizontal(); //horizontal group of button + asset name for adding in list 
                    {
                        CustomStyle.ButtonPrimary("+", 30, 30, () =>
                        {
                            if (!string.IsNullOrEmpty(inputAssetName))
                            {
                                isometricWorkflow.assetList.Add(inputAssetName);
                                inputAssetName = string.Empty;
                            }
                        });

                        inputAssetName = GUILayout.TextField(inputAssetName, GUILayout.Height(30));
                        if (string.IsNullOrEmpty(inputAssetName))
                        { 
                            var guiColor = GUI.color;
                            GUI.color = Color.grey;
                            var textRect = GUILayoutUtility.GetLastRect();
                            var position = new Rect(textRect.x, textRect.y, textRect.width, textRect.height);
                            EditorGUI.LabelField(position, "Describe what to generate");
                            GUI.color = guiColor;
                        }

                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical(); //end of vertical group
                CustomStyle.Space();
            }
            GUILayout.EndHorizontal();
            CustomStyle.Space();

            switch (isometricWorkflow.selectedTheme)
            {
                case Theme.Medieval:
                    if (isometricWorkflow.medievalSample != null && isometricWorkflow.medievalSample.Count > 0)
                    {
                        CustomStyle.Space();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            foreach (string sample in isometricWorkflow.medievalSample)
                            {
                                CustomStyle.ButtonPrimary(sample, 30, 0, () =>
                                {
                                    isometricWorkflow.FillAssetSample(sample);
                                });
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        CustomStyle.Space();
                    }
                    break;

                case Theme.Contemporary:
                    if (isometricWorkflow.contemporarySample != null && isometricWorkflow.contemporarySample.Count > 0)
                    {
                        CustomStyle.Space();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            foreach (string sample in isometricWorkflow.contemporarySample)
                            {
                                CustomStyle.ButtonPrimary(sample, 30, 0, () =>
                                {
                                    isometricWorkflow.FillAssetSample(sample);
                                });
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        CustomStyle.Space();
                    }
                    break;

                case Theme.Post_Apocalyptic:
                    if (isometricWorkflow.postApoSample != null && isometricWorkflow.postApoSample.Count > 0)
                    {
                        CustomStyle.Space();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            foreach (string sample in isometricWorkflow.postApoSample)
                            {
                                CustomStyle.ButtonPrimary(sample, 30, 0, () =>
                                {
                                    isometricWorkflow.FillAssetSample(sample);
                                });
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        CustomStyle.Space();
                    }
                    break;

                default:
                    if (isometricWorkflow.sampleList != null && isometricWorkflow.sampleList.Count > 0)
                    {
                        CustomStyle.Space();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            foreach (string sample in isometricWorkflow.sampleList)
                            {
                                CustomStyle.ButtonPrimary(sample, 30, 0, () =>
                                {
                                    isometricWorkflow.FillAssetSample(sample);
                                });
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        CustomStyle.Space();
                    }
                    break;
            }

            //Bottom
            GUILayout.BeginHorizontal();
            {
                CustomStyle.Space();
                GUILayout.FlexibleSpace();

                switch (requestStatus)
                {
                    //before requesting, buttons add sample + next
                    case RequestsStatus.NotRequested:
                        CustomStyle.ButtonPrimary("Previous", 30, 100, () =>
                        {
                            isometricWorkflow.currentStep = IsometricWorkflow.Step.Theme;
                        });

                        //Disable next part if the assetlist is empty
                        EditorGUI.BeginDisabledGroup(isometricWorkflow.assetList.Count == 0);
                        if (isometricWorkflow.assetList.Count > 0)
                        {
                            GUI.backgroundColor = Color.cyan;
                        }
                        else
                        {
                            GUI.backgroundColor = Color.grey;
                        }

                        CustomStyle.ButtonPrimary("Next", 30, 100, () =>
                        {
                            if (ScenarioSession.Instance != null)
                            {
                                if (ScenarioSession.Instance.GetInferenceBatchSize() >= isometricWorkflow.assetList.Count && !isProcessing)
                                {
                                    isProcessing = true;

                                    isometricWorkflow.AskGenerateImages((string response) => {
                                        if (EditorUtility.DisplayDialog($"Are you sure to launch ?", $"Are you sure to launch {isometricWorkflow.assetList.Count} inference(s) so {(isometricWorkflow.assetList.Count * 4)} images.\n\nThis consume {int.Parse(response) * isometricWorkflow.assetList.Count} credits.", "Launch", "Edit"))
                                        {
                                            requestStatus = RequestsStatus.Requesting;
                                            InferenceManager.SilenceMode = true;
                                            isometricWorkflow.GenerateImages(() =>
                                            {
                                                requestStatus = RequestsStatus.Requested;
                                            });
                                        }
                                        else
                                        {
                                            isProcessing = false;
                                            requestStatus = RequestsStatus.NotRequested;
                                        }
                                    });
                                }
                                else if(ScenarioSession.Instance.GetInferenceBatchSize() < isometricWorkflow.assetList.Count && !isProcessing)
                                {
                                    if (EditorUtility.DisplayDialog($"Parallel Inference Limit Reached", $"Your plan allows for a maximum of {ScenarioSession.Instance.GetInferenceBatchSize()} parallel inferences. Please select up to {ScenarioSession.Instance.GetInferenceBatchSize()} assets. Number of inference selected: ({isometricWorkflow.assetList.Count})", "Edit"))
                                    {
                                        isometricWorkflow.currentStep = IsometricWorkflow.Step.Asset;
                                    }
                                }
                            }
                        });
                        EditorGUI.EndDisabledGroup();

                        GUI.backgroundColor = defaultBackgroundColor;
                        break;
                    //Please wait during the request
                    case RequestsStatus.Requesting:
                        CustomStyle.Label("Please wait", height: 30f, width: 100f);
                        break;
                    //when request is finished, go to next page
                    case RequestsStatus.Requested:
                        isProcessing = false;
                        isometricWorkflow.currentStep = IsometricWorkflow.Step.Validation;
                        break;
                    default:
                        break;
                }

                CustomStyle.Space();
            }
            GUILayout.EndHorizontal();
            CustomStyle.Space();
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the last step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawValidationGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 5. Validation", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Label("Choose the final images:", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Space(25);

            GUILayout.BeginVertical(GUI.skin.box); // Begin vertical grouping of the scrollview
            {
                validationScrollView = GUILayout.BeginScrollView(validationScrollView, GUILayout.ExpandWidth(true));
                {
                    foreach (var keyValuePair in isometricWorkflow.inferenceIdByAssetList)
                    {
                        string assetName = keyValuePair.Key;
                        string inferenceId = keyValuePair.Value;
                        if (!drawActionPanels.ContainsKey(assetName))
                        { 
                            drawActionPanels.Add(assetName, null);
                        }

                        if (!isometricWorkflow.selectedImages.ContainsKey(assetName))
                        {
                            isometricWorkflow.selectedImages.Add(assetName, null);
                        }

                        CustomStyle.Space();
                        GUILayout.BeginHorizontal(); //begin horizontal group of one asset
                        {
                            DrawTextureBoxes(assetName, inferenceId); // draw the 4 boxes (textures) for one asset
                        }

                        GUILayout.BeginVertical(); //begin right side (with name & buttons) for one asset
                        {
                            CustomStyle.Label(assetName, fontSize: 18, alignment: TextAnchor.UpperLeft);

                            if (drawActionPanels[assetName] != null)
                            {
                                DrawButtonDetailPanel(assetName);
                            }
                            else
                            {
                                DrawInferenceButton(assetName);
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                        CustomStyle.Space();
                    }
                }
                GUILayout.EndScrollView();//end scroll view
            }
            CustomStyle.Space();
            GUILayout.EndVertical(); //end of vertical group


            //Bottom
            GUILayout.FlexibleSpace();
            CustomStyle.ButtonPrimary("Restart", 30, 200, () =>
            {
                if (EditorUtility.DisplayDialog("Are you sure you want \r\nto restart the process ?", "This will prevent you from converting the generated image into sprites. However, the generated images will remain accessible on the Scenario web app and in the images window.", "Restart", "Stay on page"))
                {
                    isometricWorkflow.selectedImages.Clear();
                    drawActionPanels.Clear();
                    isometricWorkflow.Restart();
                }
            });
            CustomStyle.Space();
        }

        #endregion

        #region Utils

        /// <summary>
        /// When using one of the action button, sometime they opens a panel with some informations and other steps
        /// </summary>
        private void DrawButtonDetailPanel(string _assetName)
        {
            drawActionPanels[_assetName]?.Invoke();

            if (GUILayout.Button("< Back", EditorStyles.miniButtonLeft))
            {
                drawActionPanels[_assetName] = null;
            }
        }

        /// <summary>
        /// Draw buttons attached to inference
        /// </summary>
        /// <param name="_assetName"></param>
        private void DrawInferenceButton(string _assetName)
        {
            EditorGUI.BeginDisabledGroup(isometricWorkflow.selectedImages[_assetName] == null);
            if (isometricWorkflow.selectedImages[_assetName] == null)
            {
                GUI.backgroundColor = Color.grey;
            }
            else
            { 
                GUI.backgroundColor = Color.cyan;
            }

            CustomStyle.ButtonPrimary("Convert to Sprite", 30, 0, () => Images.DownloadAsSprite(isometricWorkflow.selectedImages[_assetName].texture, isometricWorkflow.selectedImages[_assetName].Id, () =>
            {
                drawActionPanels[_assetName] = () =>
                {
                    string messageSuccess = "The image has been downloaded to the directory specified in the settings of the Scenario Plugin.";
                    GUILayout.Label(messageSuccess, EditorStyles.wordWrappedLabel);
                };
            },
            () =>
            {
                drawActionPanels[_assetName] = () =>
                {
                    string messageWhileDownloading = "Please wait while the background is being removed. The processed image will be saved to the directory set in the Settings of the Scenario Plugin.";
                    GUILayout.Label(messageWhileDownloading, EditorStyles.wordWrappedLabel);
                };
            }));

            CustomStyle.ButtonPrimary("Convert to Tile", 30, 0, () =>
            {
                if (isometricWorkflow.selectedImages[_assetName] != null)
                {
                    /// Contains the side window when the user want to download an image as a tile
                    if (TileCreator.Instance == null)
                    {
                        TileCreator.Instance = new(isometricWorkflow.selectedImages[_assetName].Id);
                    }

                    TileCreator.Instance.SetImageData(isometricWorkflow.selectedImages[_assetName]);

                    drawActionPanels[_assetName] = TileCreator.Instance.OnGUI;
                }
            });

            CustomStyle.ButtonPrimary("Customize (webapp)", 30, 0, () =>
            {
                Application.OpenURL($"{PluginSettings.WebAppUrl}/images?openAssetId={isometricWorkflow.selectedImages[_assetName].Id}");
            });

            CustomStyle.ButtonPrimary("Regenerate 4 images", 30, 0, () =>
            {
                requestStatus = RequestsStatus.Requesting;
                    
                isometricWorkflow.RegenerateImages(_assetName, () =>
                {
                    requestStatus = RequestsStatus.Requested;
                });
            });

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = defaultBackgroundColor;
        }

        /// <summary>
        /// Draws a grid of texture boxes, each containing an image or loading indicator, and handles interactions.
        /// </summary>
        /// <param name="_assetName">Each boxes will contain an image that represent this asset.</param>
        /// <param name="_inferenceId">Each inference will show 4 images. I need the inference ID to retrieve the correct images.</param>
        private void DrawTextureBoxes(string _assetName, string _inferenceId)
        {
            //Create a list of Image Data
            List<ImageDataStorage.ImageData> imagesToDisplay = new List<ImageDataStorage.ImageData>();

            //Add the 4 images of this inference
            imagesToDisplay.AddRange(DataCache.instance.GetImageDataByInferenceId(_inferenceId));

            for (int i = 0; i < imagesToDisplay.Count; i++)
            {
                Texture2D texture = imagesToDisplay[i].texture;

                if (texture != null)
                {
                    if (isometricWorkflow.selectedImages.ContainsKey(_assetName))
                    {
                        if (isometricWorkflow.selectedImages[_assetName] != null && isometricWorkflow.selectedImages[_assetName].Id == imagesToDisplay[i].Id)
                        { 
                            GUI.backgroundColor = Color.cyan;
                        }
                        else
                        {
                            GUI.backgroundColor = defaultBackgroundColor;
                        }
                    }

                    if (GUILayout.Button(texture, GUILayout.MaxWidth(256), GUILayout.MaxHeight(256)))
                    {
                        isometricWorkflow.selectedImages[_assetName] = imagesToDisplay[i];

                        if (TileCreator.Instance != null)
                        {
                            TileCreator.Instance.SetImageData(imagesToDisplay[i]);
                        }
                    }
                }
                else
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        stretchWidth = true,
                        stretchHeight = true
                    };
                    GUI.color = Color.white;
                    GUILayout.Label($"Loading...", style);
                }
            }
            GUI.backgroundColor = defaultBackgroundColor;
        }

        #endregion


        #region Utility Methods

        private enum RequestsStatus
        {
            NotRequested,
            Requesting,
            Requested
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


        #endregion
    }
}
