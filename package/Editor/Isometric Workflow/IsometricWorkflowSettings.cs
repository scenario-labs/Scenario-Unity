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
        public const string settingsFilePath = settingsFolderPath+"/IsometricWorkflowSettings.asset";

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
            public IsometricWorkflow.ModelStyle style;
            public string id;

            public ModelIdByStyle(IsometricWorkflow.ModelStyle style, string id)
            {
                this.style = style;
                this.id = id;
            }
        }


        private static IsometricWorkflowSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IsometricWorkflowSettings>(settingsFilePath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<IsometricWorkflowSettings>();

                settings.InitializeTextures();
                settings.InitializeModels();

                if(!Directory.Exists(settingsFolderPath))
                    Directory.CreateDirectory(settingsFolderPath);

                AssetDatabase.CreateAsset(settings, settingsFilePath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if(settings.IsCorrupted(settings))
                {
                    AssetDatabase.DeleteAsset(settingsFilePath);
                    return GetOrCreateSettings();
                }
            }
            return settings;
        }

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
            if(settings.isometricModelThumbnails == null
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


        private void InitializeTextures()
        {
            squareBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(CommonUtils.PluginFolderPath(), "Assets", "Reference Images", "IsometricBase_Square.png"));
        }

        private async void InitializeModels()
        {
            Debug.Log(modelsIdByStyle.Count);
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora1, "B7SV_qMLR12Sy0Sp07oyaw"));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora2, "Re3h9nZrQ5CDWAkk50bXlg"));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora3, "U054yotpQqyRnhvAhVMiTA"));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora4, "JoHeBmGuQuijNq1HgFXbrQ"));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora5, "fQ48heIJSGCQQalFspW44g"));
            modelsIdByStyle.Add(new ModelIdByStyle(IsometricWorkflow.ModelStyle.lora6, "I5jfHdm-QimzxUWAoAjiQQ"));

            foreach (var model in modelsIdByStyle)
            {
                try
                {
                    Models.ModelData modelData = await Models.FetchModelById(model.id); //fetch the model
                    Texture2D texture = await Models.FetchThumbnailForModel(modelData); //fetch the texture

                    if (!isometricModels.Exists(x => x.style == model.style))
                    {
                        isometricModels.Add(new ModelDataByStyle(model.style, modelData)); //add the model to the dictionary
                    }

                    if (!isometricModelThumbnails.Exists(x => x.style == model.style))
                    {
                        isometricModelThumbnails.Add(new ModelThumbnailByStyle(model.style, texture)); //add the thumbnail to the dictionary
                    }
                }
                catch (Exception e)
                {
                    // Handle exceptions, possibly continue to the next iteration, or log the error
                    Debug.LogError($"Error fetching model or thumbnail for {model.style}: {e.Message}");
                }
            }
        }
    }
}
