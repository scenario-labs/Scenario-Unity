using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Scenario.Editor.IsometricWorkflowSettings;

namespace Scenario.Editor
{
    public class IsometricWorkflow : EditorWindow
    {
        /// <summary>
        /// Static field that contains all the UI functions
        /// </summary>
        private static readonly IsometricWorkflowUI isometricWorkflowUI = new();

        /// <summary>
        /// is TRUE when the window is visible. FALSE otherwise
        /// </summary>
        private static bool isVisible = false;

        /// <summary>
        /// The isometric workflow is made of multiples steps. This field contains the current step
        /// </summary>
        internal Step currentStep;

        /// <summary>
        /// The first step of the workflow is to select a base. This field contains the base that the user has choosen
        /// </summary>
        internal Base selectedBase = Base.None;


        /// <summary>
        /// The second step of the workflow is to select a Style. This field contains the model that match the style that the user has choosen
        /// </summary>
        internal ModelIdByStyle selectedModel = null;


        /// <summary>
        /// The third step of the workflow is to select a theme. This field contains the theme that match the theme that the user has choosen
        /// </summary>
        internal Theme selectedTheme = Theme.None;

        /// <summary>
        /// Fix by default the prompt text.
        /// </summary>
        internal string basePromptText = $"isometric, solo, centered, solid color background";

        /// <summary>
        /// The fourth step of the workflow is to create a list of asset name. 
        /// </summary>
        internal List<string> assetList = new List<string>();

        /// <summary>
        /// Each asset is linked to an inference (a request of image generation) id
        /// </summary>
        internal Dictionary<string, string> inferenceIdByAssetList = new Dictionary<string, string>();

        /// <summary>
        /// Foreach asset, 4 images are generated. the use will be able to select one texture per asset. This is the Dictionary that contains, foreach asset, the current selected texture
        /// First value is the assetName, second value is the id of the selected image
        /// </summary>
        internal Dictionary<string, ImageDataStorage.ImageData> selectedImages = new Dictionary<string, ImageDataStorage.ImageData>();

        /// <summary>
        /// Stock Image generated to modify
        /// </summary>
        internal ImageDataStorage.ImageData imageDataSelected = null;

        internal List<string> sampleList = new List<string>() 
        {
            "Tavern",
            "Hospital",
            "Police Station",
            "Rocket Launcher",
            "Factory",
            "Treehouse",
            "Arena",
            "Temple",
            "Church",
            "Building Block"
        };

        /// <summary>
        /// Reference to setting class.
        /// </summary>
        internal static IsometricWorkflowSettings settings;

        [MenuItem("Window/Scenario/Workflows/1. Isometric Workflow")]
        public static void ShowWindow()
        {
            if (isVisible)
                return;

            PromptFetcher.SilenceMode = true;

            var isometricWorkflow = GetWindow<IsometricWorkflow>("Isometric Workflow");
            isometricWorkflow.minSize = new Vector2(1250, 500);

            settings = IsometricWorkflowSettings.GetSerializedSettings();
        }

        public void Restart()
        {
            PromptFetcher.SilenceMode = true;

            var isometricWorkflow = (IsometricWorkflow)GetWindow(typeof(IsometricWorkflow));
            
            isometricWorkflowUI.Init(isometricWorkflow);
            isometricWorkflow.currentStep = Step.Base;

            settings = IsometricWorkflowSettings.GetSerializedSettings();
        }

        public static void CloseWindow()
        {
            var isometricWorkflow = GetWindow<IsometricWorkflow>("Isometric Workflow");
            isometricWorkflow.Close();
        }

        /// <summary>
        /// Add to the asset list to be generate a sample select by a click on a button
        /// </summary>
        /// <param name="_sample"> String of the sample to add </param>
        public void FillAssetSample(string _sample)
        {
            assetList.Add(_sample);//
        }

        private void CreateGUI()
        {
            var isometricWorkflow = (IsometricWorkflow)GetWindow(typeof(IsometricWorkflow));
            isometricWorkflowUI.Init(isometricWorkflow);
        }

        /// <summary>
        /// Call the prompt window to generate one inference (four images) per asset in the assetlist
        /// </summary>
        /// <param name="_onRequestSent">callback when ALL requests has been sent</param>
        public void GenerateImages(Action _onRequestSent)
        {
            Texture2D baseTexture = null;
            switch (selectedBase)
            {
                case Base.None:
                    baseTexture = null;
                    break;
                case Base.Square:
                    baseTexture = settings.squareBaseTexture;
                    break;
                case Base.Custom:
                    baseTexture = isometricWorkflowUI.customTexture;
                    break;
                default:
                    break;
            }

            inferenceIdByAssetList.Clear();

            int totalRequests = assetList.Count;
            int completedRequests = 0;

            foreach (string assetName in assetList)
            {
                string tempName = assetName;
                bool useBaseTexture = baseTexture != null;
                string modelName = settings.isometricModels.Find(x => x.style == selectedModel.style).modelData.name;

                string prompt = string.Empty;
                if (string.IsNullOrEmpty(selectedModel.DefaultPrompt))
                {
                    prompt = $"{basePromptText}, {selectedTheme}, {assetName}";
                }
                else
                {
                    prompt = $"{selectedModel.DefaultPrompt}, {selectedTheme}, {assetName}";
                }

                PromptWindow.GenerateImage(modelName, useBaseTexture, useBaseTexture, useBaseTexture ? baseTexture : null, 4, prompt, 30, 1024, 1024, 6, "-1", useBaseTexture, selectedModel.Influence,
                (inferenceId) =>
                {
                    if (inferenceIdByAssetList.ContainsKey(tempName))
                    {
                        inferenceIdByAssetList[tempName] = inferenceId;
                    }
                    else
                    { 
                        inferenceIdByAssetList.Add(tempName, inferenceId);
                    }

                    completedRequests++;
                    if (completedRequests == totalRequests)
                    {
                        _onRequestSent?.Invoke();
                    }
                });
            }
        }

        /// <summary>
        /// Regenerate only on inference.
        /// </summary>
        /// <param name="_assetName"> Based on the previous asset name</param>
        /// <param name="_onRequestSent"> Callback from generation </param>
        public void RegenerateImages(string _assetName, Action _onRequestSent)
        {
            Texture2D baseTexture = null;
            switch (selectedBase)
            {
                case Base.None:
                    baseTexture = null;
                    break;
                case Base.Square:
                    baseTexture = settings.squareBaseTexture;
                    break;
                case Base.Custom:
                    baseTexture = isometricWorkflowUI.customTexture;
                    break;
                default:
                    break;
            }

            int totalRequests = assetList.Count;
            int completedRequests = 0;
;
            bool useBaseTexture = baseTexture != null;
            string modelName = settings.isometricModels.Find(x => x.style == selectedModel.style).modelData.name;
            string prompt = string.Empty;
            if (string.IsNullOrEmpty(selectedModel.DefaultPrompt))
            {
                prompt = $"{basePromptText}, {selectedTheme}, {_assetName}";
            }
            else
            {
                prompt = $"{selectedModel.DefaultPrompt}, {selectedTheme}, {_assetName}";
            }
            
            PromptWindow.GenerateImage(modelName, useBaseTexture, useBaseTexture, useBaseTexture ? baseTexture : null, 4, prompt, 30, 1024, 1024, 6, "-1", useBaseTexture, selectedModel.Influence,
            (inferenceId) =>
            {
                if (inferenceIdByAssetList.ContainsKey(_assetName))
                {
                    inferenceIdByAssetList[_assetName] = inferenceId;
                }
                else
                {
                    inferenceIdByAssetList.Add(_assetName, inferenceId);
                }

                completedRequests++;
                if (completedRequests == totalRequests)
                {
                    _onRequestSent?.Invoke();
                }
            });

        }

        private void OnGUI()
        {
            switch (currentStep)
            {
                case Step.Base:
                    isometricWorkflowUI.DrawBaseGUI(this.position);
                    break;
                case Step.Style:
                    isometricWorkflowUI.DrawStyleGUI(this.position);
                    break;
                case Step.Theme:
                    isometricWorkflowUI.DrawThemeGUI(this.position);
                    break;
                case Step.Asset:
                    isometricWorkflowUI.DrawAssetsGUI(this.position);
                    break;
                case Step.Validation:
                    isometricWorkflowUI.DrawValidationGUI(this.position);
                    break;
                default:
                    break;
            }
        }

        private void OnDestroy()
        {
            PromptFetcher.SilenceMode = false;
        }

        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
        }

        public enum ModelStyle
        {
            lora1,
            lora2,
            lora3,
            lora4,
            lora5,
            lora6,
            Fairytale_Isometric_Buildings,
            Isometric_Buildings,
            Isometric_Buildings_FairyTale
        }


        public enum Theme
        {
            None,
            Medieval,
            Futuristic,
            Contemporary,
            Ancient,
            Magical_Forest,
            World_War,
        }


        public enum Step
        {
            Base = 0,
            Style = 1,
            Theme = 2,
            Asset = 3,
            Validation = 4,
        }

        public enum Base
        {
            None = 0,
            Square = 1,
            Custom = 2,
        }
    }
}
