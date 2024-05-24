using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    class IsometricWorkflowSettings : ScriptableObject
    {
        public const string settingsFolderPath = "Assets/Editor/Scenario/Settings";
        public const string settingsFilePath = settingsFolderPath + "/IsometricWorkflowSettings.asset";

        public static IsometricWorkflowSettings Instance = null;

        /// <summary>
        /// This field contains the reference image of the square base
        /// </summary>
        internal Texture2D squareBaseTexture;

        /// <summary>
        /// This List contains the 6 models we have curated as the best models for making isometric tiles. They are listed by ModelStyle
        /// </summary>
        internal List<ModelDataByStyle> isometricModels = new List<ModelDataByStyle>();

        /// <summary>
        /// This List contains the thumbnails for the models we have curated. They are listed by ModelStyle
        /// </summary>
        internal List<ModelThumbnailByStyle> isometricModelThumbnails = new List<ModelThumbnailByStyle>();

        internal List<ModelIdByStyle> modelsIdByStyle = new List<ModelIdByStyle>();

        [Serializable]
        public class ModelDataByStyle
        {
            public IsometricWorkflow.ModelStyle style;
            public Models.ModelData modelData;

            public ModelDataByStyle(IsometricWorkflow.ModelStyle style, Models.ModelData modelData)
            {
                this.style = style;
                this.modelData = modelData;
            }
        }

        [Serializable]
        public class ModelThumbnailByStyle
        {
            public IsometricWorkflow.ModelStyle style;
            public Texture2D thumbnail;

            public ModelThumbnailByStyle(IsometricWorkflow.ModelStyle style, Texture2D thumbnail)
            {
                this.style = style;
                this.thumbnail = thumbnail;
            }
        }

        [Serializable]
        public class ModelIdByStyle
        {
            /// <summary>
            /// Model style affected to the object.
            /// </summary>
            public IsometricWorkflow.ModelStyle style;

            /// <summary>
            /// Reference model id affected to the object
            /// </summary>
            public string id;

            /// <summary>
            /// Reference Model name affected to the object
            /// </summary>
            public string Name = string.Empty;

            /// <summary>
            /// Reference default prompt attached to the model
            /// </summary>
            public string DefaultPrompt = string.Empty;

            /// <summary>
            /// Reference influence affected to the canny/Structure modality
            /// </summary>
            public float Influence = 0.8f;

            public ModelIdByStyle(IsometricWorkflow.ModelStyle style, string id)
            {
                this.style = style;
                this.id = id;
            }

            public ModelIdByStyle(IsometricWorkflow.ModelStyle style, string id, string name, string defaultPrompt) : this(style, id)
            {
                Name = name;
                DefaultPrompt = defaultPrompt;
            }

            public ModelIdByStyle(IsometricWorkflow.ModelStyle style, string id, string name, string defaultPrompt, float influence) : this(style, id, name, defaultPrompt)
            {
                Influence = influence;
            }
        }

        public List<ModelIdByStyle> GetModelsIdByStyle()
        { 
            return Instance.modelsIdByStyle;
        }

        /// <summary>
        /// Load or create specific settings to the Isometric Workflow.
        /// </summary>
        /// <returns></returns>
        private static IsometricWorkflowSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IsometricWorkflowSettings>(settingsFilePath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<IsometricWorkflowSettings>();

                settings.InitializeTextures();
                settings.InitializeModels();

                if (!Directory.Exists(settingsFolderPath))
                    Directory.CreateDirectory(settingsFolderPath);

                AssetDatabase.CreateAsset(settings, settingsFilePath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (settings.IsCorrupted(settings))
                {
                    AssetDatabase.DeleteAsset(settingsFilePath);
                    return GetOrCreateSettings();
                }
            }
            Instance = settings;
            return settings;
        }

        /// <summary>
        /// Call to load or create isometric settings.
        /// </summary>
        /// <returns></returns>
        public static IsometricWorkflowSettings GetSerializedSettings()
        {
            return GetOrCreateSettings();
        }

        /// <summary>
        /// Check if the setting file is corrupted.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>FALSE if the file is safe. TRUE if the file is corrupted</returns>
        private bool IsCorrupted(IsometricWorkflowSettings settings)
        {
            if (settings.isometricModelThumbnails == null
                || settings.modelsIdByStyle == null
                || settings.isometricModels == null
                || settings.isometricModelThumbnails.Count != settings.modelsIdByStyle.Count
                || settings.isometricModels.Count != settings.modelsIdByStyle.Count
                || settings.modelsIdByStyle.Count == 0
                || settings.squareBaseTexture == null)
            {
                Debug.Log("Isometric Workflow Settings is empty or corrupted. Creating a new Isometric Workflow Settings.");
                return true;

            }
            return false;
        }

        /// <summary>
        /// Initialize square base texture
        /// </summary>
        private void InitializeTextures()
        {
            squareBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(CommonUtils.PluginFolderPath(), "Assets", "Reference Images", "IsometricBase_Square.png"));
        }

        /// <summary>
        /// Prepare Isometric with models and load it.
        /// </summary>
        private async void InitializeModels()
        {
            //Debug.Log(modelsIdByStyle.Count);
            string defaultPrompt = "solid color background, isometric building";

            //modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Isometric_2, "model_cpBBUtyovdNYnmLo7FS1idVg", "2D Pencil Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Fairytale_Isometric_Buildings, "model_BadrJ8rQCJiHji9j83Xo1TMN", "2D Cartoon Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Illustrated_Isometric_2, "model_BxqG76YQ1W5y2dyCjTig4p98", "2D Detailed Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Isometric_05, "model_f48WwPk6k1Rd6MwkS6KwjkuW", "3D Cartoon Style", defaultPrompt, 0.50f));
            //modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Isometric_Vignette_Constructor, "model_wK5soMR3RzpVf8KSYdFnAXwz", "3D Realistic Style", defaultPrompt, 0.50f));
            //modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Isometric_Blend_All, "B7SV_qMLR12Sy0Sp07oyaw", "2D Realistic Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Classic_Isometric_Models, "model_r7pp9bdUccQmTxw3Jg1yqkV3", "3D Pastel Style", defaultPrompt, 0.50f));
            //modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Iso_Cute_3, "model_amJuxh8Zchzs96ZVvVw767EJ", "2D Outlined Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.New_Isometric_3, "model_nrEFVerqgGp76yeC2vKBiQs5", "3D Realistic Style", defaultPrompt, 0.50f));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.Illustrative_Isometric_Buildings, "model_bCvToHu9Thf7JTPfH9tKLBgd", "2D Vivid Style", defaultPrompt, 0.50f));

            
            try
            {
                foreach (var model in modelsIdByStyle)
                {
                    Models.ModelData modelData = await Models.FetchModelById(model.id); //fetch the model
                    Texture2D texture = new Texture2D(2, 2);
                    if (modelData.thumbnail != null)
                    { 
                        texture = await Models.FetchThumbnailForModel(modelData); //fetch the texture
                    }

                    if (!isometricModels.Exists(x => x.style == model.style))
                    {
                        isometricModels.Add(new ModelDataByStyle(model.style, modelData)); //add the model to the dictionary
                    }

                    if (!isometricModelThumbnails.Exists(x => x.style == model.style))
                    {
                        isometricModelThumbnails.Add(new ModelThumbnailByStyle(model.style, texture)); //add the thumbnail to the dictionary
                    }
                }
            }
            catch (Exception e)
            {
                if (EditorUtility.DisplayDialog("Settings from Scenario Settings are not correctly filled.", "Settings from Scenario Settings are not correctly filled.\n Isometric workflow need to be linked to your Scenario account. (API Key, Secret key, TeamID key)", "Open Settings", "Exit"))
                {
                    PluginSettings.ShowWindow();
                    IsometricWorkflow.CloseWindow();
                }
            }
            
        }
    }
}
