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
        /// Filled when other theme is selected.
        /// </summary>
        internal string customTheme = string.Empty;

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

        /// <summary>
        /// Default sample asset to offer to the user
        /// </summary>
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
        /// Medieval sample assets to offer to the user.
        /// </summary>
        internal List<string> medievalSample = new List<string>()
        {
            "Tavern",
            "Castle",
            "Market",
            "Guard Tower",
            "Blacksmith",
            "Church",
            "Farm",
            "Horse Stable",
            "Small House"
        };

        /// <summary>
        /// Contemporary sample assets to offer to the user.
        /// </summary>
        internal List<string> contemporarySample = new List<string>()
        {
            "Office Building",
            "Hospital",
            "Police Station",
            "Shopping Mall",
            "Residential Building",
            "Nuclear Plant",
            "Factory",
            "Mayor's House",
            "Communications Station",
            "Fire Station"
        };

        /// <summary>
        /// Post Apocalyptic sample assets to offer to the user.
        /// </summary>
        internal List<string> postApoSample = new List<string>()
        {
            "Crumbling Building",
            "Destroyed House",
            "Rusty Guard Tower",
            "Abandoned Store",
            "Survivor Campsite",
            "Broken down Factory",
            "Airplane Shelter",
            "Military Base",
            "Abandoned Gas Station",
            "Destroyed Nuclear Power Plant"
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

            InferenceManager.SilenceMode = true;

            var isometricWorkflow = GetWindow<IsometricWorkflow>("Isometric Workflow");
            isometricWorkflow.minSize = new Vector2(1250, 500);

            settings = IsometricWorkflowSettings.GetSerializedSettings();
        }

        public void Restart()
        {
            InferenceManager.SilenceMode = true;

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
        /// Call the api to get the cost of one inference (four images) per asset in the assetlist
        /// </summary>
        /// <param name="_onRequestSent"> callback when ALL requests has been sent</param>
        public void AskGenerateImages(Action<string> _onRequestSent)
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

            string tempName = string.Empty;
            bool useBaseTexture = true;
            string modelName = string.Empty;
            string prompt = string.Empty;

            foreach (string assetName in assetList)
            {
                tempName = assetName;
                useBaseTexture = baseTexture != null;
                modelName = settings.isometricModels.Find(x => x.style == selectedModel.style).modelData.name;

                prompt = string.Empty;

                if (selectedTheme == Theme.Other)
                {
                    prompt = $"{customTheme}, ";
                }
                else
                {
                    prompt = $"{selectedTheme}, ";
                }

                prompt += $"{assetName}, ";

                if (string.IsNullOrEmpty(selectedModel.DefaultPrompt))
                {
                    prompt += $"{basePromptText} ";
                }
                else
                {
                    prompt += $"{selectedModel.DefaultPrompt} ";
                }
            }

            if (PromptPusher.Instance != null)
            {
                PromptPusher.Instance.AskGenerateIsometricImage(modelName, ECreationMode.ControlNet, useBaseTexture ? baseTexture : null, 4, prompt, 30, 1024, 1024, 6, "-1", useBaseTexture, selectedModel.Influence, _onRequestSent);
            }
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

                if (selectedTheme == Theme.Other)
                {
                    prompt = $"{customTheme}, ";
                }
                else
                {
                    prompt = $"{selectedTheme}, ";
                }

                prompt += $"{assetName}, ";

                if (string.IsNullOrEmpty(selectedModel.DefaultPrompt))
                {
                    prompt += $"{basePromptText} ";
                }
                else
                {
                    prompt += $"{selectedModel.DefaultPrompt} ";
                }

                if (PromptPusher.Instance != null)
                {
                    PromptPusher.Instance.GenerateIsometricImage(modelName, ECreationMode.ControlNet, useBaseTexture ? baseTexture : null, 4, prompt, 30, 1024, 1024, 6, "-1", useBaseTexture, selectedModel.Influence,
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
                prompt = $"{basePromptText}, ";
            }
            else
            {
                prompt = $"{selectedModel.DefaultPrompt}, ";
            }

            if (selectedTheme == Theme.Other)
            {
                prompt += $"{customTheme}, ";
            }
            else
            {
                prompt += $"{selectedTheme}, ";
            }

            prompt += _assetName;

            if (PromptPusher.Instance != null)
            {
                PromptPusher.Instance.GenerateIsometricImage(modelName, ECreationMode.ControlNet, useBaseTexture ? baseTexture : null, 4, prompt, 30, 1024, 1024, 6, "-1", useBaseTexture, selectedModel.Influence,
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
            InferenceManager.SilenceMode = false;
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
            Isometric_2,
            Illustrated_Isometric_2,
            Isometric_05,
            Isometric_Vignette_Constructor,
            Isometric_Blend_All,
            Classic_Isometric_Models,
            Fairytale_Isometric_Buildings,
            Iso_Cute_3,
            New_Isometric_3,
            Illustrative_Isometric_Buildings
        }


        public enum Theme
        {
            None,
            Medieval,
            Futuristic,
            Contemporary,
            Post_Apocalyptic,
            Ancient,
            Magical_Forest,
            World_War,
            Other
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
