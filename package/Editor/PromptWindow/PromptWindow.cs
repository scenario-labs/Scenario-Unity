using System;
using System.Collections.Generic;
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
            GetWindow<PromptWindow>("Prompt Window");
        }

        #region Static Methods
        /// <summary>
        /// If you want to generate an image through script, call this
        /// </summary>
        /// <param name="_modelName"></param>
        /// <param name="_isImageToImage"></param>
        /// <param name="_isControlNet"></param>
        /// <param name="_texture"></param>
        /// <param name="_numberOfImages"></param>
        /// <param name="_promptText"></param>
        /// <param name="_samples"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_guidance"></param>
        /// <param name="_seed"></param>
        /// <param name="_useCanny"></param>
        /// <param name="_cannyStrength"></param>
        public static void GenerateImage(string _modelName, bool _isImageToImage = false, bool _isControlNet = false, Texture2D _texture = null, int _numberOfImages = 4, string _promptText = "", int _samples = 30, int _width = 1024, int _height = 1024, float _guidance = 6.0f, string _seed = "-1", bool _useCanny = false, float _cannyStrength = 0.8f, Action<string> _onInferenceRequested = null)
        {
            if (promptWindowUI != null)
            {
                promptWindowUI.selectedModelName = _modelName;
                promptWindowUI.isImageToImage = _isImageToImage;
                promptWindowUI.isControlNet = _isControlNet;
                PromptWindowUI.imageUpload = _texture;
                promptWindowUI.imagesliderIntValue = _numberOfImages;
                promptWindowUI.promptinputText = _promptText;
                promptWindowUI.samplesliderValue = _samples;
                promptWindowUI.widthSliderValue = _width;
                promptWindowUI.heightSliderValue = _height;
                promptWindowUI.guidancesliderValue = _guidance;
                promptWindowUI.seedinputText = _seed;
                if (_useCanny)
                {
                    promptWindowUI.isAdvancedSettings = _useCanny;
                    promptWindowUI.selectedOptionIndex = Array.IndexOf(promptWindowUI.correspondingOptionsValue, "canny") + 1;
                    promptWindowUI.sliderValue = _cannyStrength;
                }

                GetWindow<PromptWindow>().GenerateImage(_seed == "-1" ? null : _seed, _onInferenceRequested);
            }
            else
            {
                ShowWindow();
                GenerateImage(_modelName, _isImageToImage, _isControlNet, _texture, _numberOfImages, _promptText, _samples, _width, _height, _guidance, _seed, _useCanny, _cannyStrength, _onInferenceRequested);
            }
        }

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            promptWindowUI = new PromptWindowUI(this);
            UpdateSelectedModel(promptWindowUI.selectedModelName);
        }

        private void OnFocus()
        {
            if (promptWindowUI != null)
            {
                UpdateSelectedModel(promptWindowUI.selectedModelName);
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
