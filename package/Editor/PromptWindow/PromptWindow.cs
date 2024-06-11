using System;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class PromptWindow : EditorWindow
    {
        public static PromptWindowUI promptWindowUI;
        
        public CreationMode ActiveMode { get { return activeMode; } set { activeMode = value; } }

        private bool processReceivedUploadImage = false;
        private byte[] pngBytesUploadImage = null;

        private CreationMode activeMode = null;

        [MenuItem("Window/Scenario/Prompt Window", false, 5)]
        public static void ShowWindow()
        {
            var promptWindow = GetWindow<PromptWindow>("Prompt Window");
            promptWindow.minSize = new Vector2(500, 750);
        }

        #region Static Methods
        

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            promptWindowUI = new PromptWindowUI(this);
            UpdateSelectedModel();
        }

        private void OnFocus()
        {
            if (promptWindowUI != null)
            {
                UpdateSelectedModel();
            }
        }

        private void Update()
        {
            if (!processReceivedUploadImage) return;

            processReceivedUploadImage = false;
            PromptWindowUI.imageUpload.LoadImage(pngBytesUploadImage);
        }

        private void OnGUI()
        {
            promptWindowUI.Render(this.position);
        }

        #endregion

        public void SetSeed(string _seed)
        {
            // Set the seed value here
            PromptPusher.Instance.seedInput = _seed;
        }

        /// <summary>
        /// Force the Image Control Tab to opens at a spcifi tab
        /// </summary>
        public static void SetImageControlTab(int tabIndex)
        {
            promptWindowUI.imageControlTab = tabIndex;
        }

        /// <summary>
        /// Call to set the image from the drop content.
        /// </summary>
        /// <param name="_newContent"></param>
        public static void SetDropImageContent(Texture2D _newContent)
        {
            promptWindowUI.SetDropImage(_newContent);
        }

        /// <summary>
        /// Call to set the image from the drop mask content.
        /// </summary>
        /// <param name="_newContent"></param>
        public static void SetDropMaskImageContent(Texture2D _newContent)
        {
            promptWindowUI.SetDropMaskImage(_newContent);
        }

        /// <summary>
        /// Call to set the image from the additional drop content.
        /// </summary>
        /// <param name="_newContent"></param>
        public static void SetDropAdditionalImageContent(Texture2D _newContent)
        {
            promptWindowUI.SetAdditionalDropImage(_newContent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_creationMode"></param>
        public void SetActiveModeUI(ECreationMode _creationMode)
        {
            promptWindowUI.imageControlTab = (int)_creationMode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_active"></param>
        public void ActiveAdvanceSettings(bool _active)
        { 
            promptWindowUI.isAdvancedSettings = _active;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_selectedModality"></param>
        public void SetAdvancedModality(int _selectedModality)
        { 
            promptWindowUI.selectedOptionIndex = _selectedModality;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_value"></param>
        public void SetAdvancedModalityValue(int _value)
        { 
            promptWindowUI.sliderDisplayedValue = _value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_indexValue"></param>
        public void SetImageSettingWidth(int _indexValue)
        { 
            promptWindowUI.WidthSliderValue = _indexValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_indexValue"></param>
        public void SetImageSettingHeight(int _indexValue)
        { 
            promptWindowUI.HeightSliderValue = _indexValue;
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateSelectedModel()
        {
            string selectedModelId = DataCache.instance.SelectedModelId;
            string selectedModelName = EditorPrefs.GetString("SelectedModelName");

            if (!string.IsNullOrEmpty(selectedModelId) && !string.IsNullOrEmpty(selectedModelName))
            {
                promptWindowUI.selectedModelName = selectedModelName;
            }
            else
            {
                promptWindowUI.selectedModelName = "Choose Model";
            }
        }

        #region API_DTO

        [Serializable]
        public class InferenceRoot
        {
            public Inference inference { get; set; }
        }

        [Serializable]
        public class Inference
        {
            public string id { get; set; }
            public string userId { get; set; }
            public string ownerId { get; set; }
            public string authorId { get; set; }
            public string modelId { get; set; }
            public DateTime createdAt { get; set; }
            public Parameters parameters { get; set; }
            public string status { get; set; }
            public List<object> images { get; set; }
            public int imagesNumber { get; set; }
            public string displayPrompt { get; set; }
        }

        [Serializable]
        public class Parameters
        {
            public string negativePrompt { get; set; }
            public int numSamples { get; set; }
            public double guidance { get; set; }
            public int numInferenceSteps { get; set; }
            public bool enableSafetyCheck { get; set; }
            public ulong seed { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string type { get; set; }
            public string image { get; set; }
            public string prompt { get; set; }
            public string mask { get; set; }
        }

        [Serializable]
        public class InferenceStatusRoot
        {
            public Inference inference { get; set; }
        }

        [Serializable]
        public class InferenceStatus
        {
            public string id { get; set; }
            public string userId { get; set; }
            public string ownerId { get; set; }
            public string authorId { get; set; }
            public string modelId { get; set; }
            public DateTime createdAt { get; set; }
            public ParametersStatus parameters { get; set; }
            public string status { get; set; }
            public List<Image> images { get; set; }
        }

        [Serializable]
        public class ParametersStatus
        {
            public string image { get; set; }
            public double guidance { get; set; }
            public int numInferenceSteps { get; set; }
            public int numSamples { get; set; }
            public int width { get; set; }
            public string type { get; set; }
            public string prompt { get; set; }
            public bool enableSafetyCheck { get; set; }
            public int height { get; set; }
            public string mask { get; set; }
        }

        [Serializable]
        public class Image
        {
            public string id { get; set; }
            public string url { get; set; }
        }

        #endregion
    }
}
