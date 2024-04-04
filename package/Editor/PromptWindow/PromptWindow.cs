using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class PromptWindow : EditorWindow
    {
        public static PromptWindowUI promptWindowUI;

        private bool processReceivedUploadImage = false;
        private byte[] pngBytesUploadImage = null;
        private string fileName;

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Send a inference request to the API
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="_onInferenceRequested">return a callback when the inference has been posted to the API with the inferenceID</param>
        public void GenerateImage(string seed, Action<string> _onInferenceRequested = null)
        {
            Debug.Log("Generate Image button clicked. Model: " + promptWindowUI.selectedModelName + ", Seed: " + seed);
            if (IsPromptDataValid(out string inputData))
            {
                PromptFetcher.PostInferenceRequest(inputData,
                    promptWindowUI.imagesliderIntValue,
                    promptWindowUI.promptinputText,
                    promptWindowUI.samplesliderValue,
                    promptWindowUI.widthSliderValue,
                    promptWindowUI.heightSliderValue,
                    promptWindowUI.guidancesliderValue,
                    promptWindowUI.seedinputText,
                    _onInferenceRequested);
            }
        }
        public void SetSeed(string seed)
        {
            // Set the seed value here
        }

        /// <summary>
        /// Force the Image Control Tab to opens at a spcifi tab
        /// </summary>
        public static void SetImageControlTab(int tabIndex)
        {
            promptWindowUI.imageControlTab = tabIndex;
        }

        /// <summary>
        /// Update the selectedModel in the Prompt Window then save it in the editorprefs. You can specify the model. If nothing is specified it will try to find a value in the editorprefs
        /// </summary>
        /// <param name="_selectedModelName">The name of the model you want to update in the prompt window. Can be null</param>
        public static void UpdateSelectedModel(string _selectedModelName = null)
        {
            string selectedModelName = null;

            if (string.IsNullOrEmpty(_selectedModelName))
                selectedModelName = EditorPrefs.GetString("scenario/selectedModelName");

            if (string.IsNullOrEmpty(selectedModelName))
            {
                promptWindowUI.selectedModelName = "Choose Model";
                EditorPrefs.SetString("scenario/selectedModelName", null);
            }
            else
            {
                promptWindowUI.selectedModelName = selectedModelName;
                EditorPrefs.SetString("scenario/selectedModelName", selectedModelName);
            }
        }
        #endregion

        #region Private Methods

        private bool IsPromptDataValid(out string inputData)
        {
            string modality = "";
            string operationType = "txt2img";
            string dataUrl = "\"\"";
            string maskDataUrl = "\"\"";

            inputData = "";

            if (promptWindowUI.isImageToImage)
            {
                operationType = "img2img";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("Img2Img Must have a image uploaded.");
                    return false;
                }

                dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
            }
            else if (promptWindowUI.isControlNet)
            {
                operationType = "controlnet";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("ControlNet Must have a image uploaded.");
                    return false;
                }

                dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
            }

            if (promptWindowUI.isImageToImage && promptWindowUI.isControlNet)
            {
                operationType = "controlnet";
            }

            Dictionary<string, string> modalitySettings = PrepareModalitySettings(ref modality, ref operationType);

            modality = PrepareModality(modalitySettings);

            if (promptWindowUI.isInpainting)
            {
                operationType = "inpaint";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("Inpainting Must have an image uploaded.");
                    return false;
                }
                else
                {
                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
                }

                if (PromptWindowUI.imageMask == null)
                {
                    Debug.LogError("Inpainting Must have a mask uploaded.");
                    return false;
                }
                else
                {
                    maskDataUrl = ProcessMask();
                }
            }

            inputData = PrepareInputData(modality, operationType, dataUrl, maskDataUrl);
            Debug.Log("Input Data: " + inputData);

            return true;
        }

        private string PrepareInputData(string modality, string operationType, string dataUrl, string maskDataUrl)
        {
            bool hideResults = false;
            string type = operationType;
            string mask = $"\"{maskDataUrl}\"";
            string prompt = promptWindowUI.promptinputText;
            string seedField = "";
            string image = $"\"{dataUrl}\"";

            if (promptWindowUI.seedinputText != "-1")
            {
                ulong seed = ulong.Parse(promptWindowUI.seedinputText);
                seedField = $@"""seed"": {seed},";
            }

            string negativePrompt = promptWindowUI.negativepromptinputText;
            float strength = Mathf.Clamp((float)Math.Round((100 - promptWindowUI.influenceSliderValue) * 0.01f, 2), 0.01f, 1f); //strength is 100-influence (and between 0.01 & 1)
            float guidance = promptWindowUI.guidancesliderValue;
            int width = (int)promptWindowUI.widthSliderValue;
            int height = (int)promptWindowUI.heightSliderValue;
            int numInferenceSteps = (int)promptWindowUI.samplesliderValue;
            int numSamples = (int)promptWindowUI.imagesliderIntValue;
            string scheduler = promptWindowUI.schedulerOptions[promptWindowUI.schedulerIndex];

            string inputData = $@"{{
                ""parameters"": {{
                    ""hideResults"": {hideResults.ToString().ToLower()},
                    ""type"": ""{type}"",
                    {(promptWindowUI.isImageToImage || promptWindowUI.isInpainting || promptWindowUI.isControlNet ? $@"""image"": ""{dataUrl}""," : "")}
                    {(promptWindowUI.isControlNet || promptWindowUI.isAdvancedSettings ? $@"""modality"": ""{modality}""," : "")}
                    {(promptWindowUI.isInpainting ? $@"""mask"": ""{maskDataUrl}""," : "")}
                    ""prompt"": ""{prompt}"",
                    {seedField}
                    {(string.IsNullOrEmpty(negativePrompt) ? "" : $@"""negativePrompt"": ""{negativePrompt}"",")}
                    {(promptWindowUI.isImageToImage || promptWindowUI.isControlNet ? $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)}," : "")}
                    ""guidance"": {guidance.ToString("F2", CultureInfo.InvariantCulture)},
                    ""numInferenceSteps"": {numInferenceSteps},
                    ""width"": {width},
                    ""height"": {height},
                    ""numSamples"": {numSamples}
                    {(scheduler != "Default" ? $@",""scheduler"": ""{scheduler}""" : "")}
                }}
            }}";
            return inputData;
        }

        private void PrepareAdvancedModalitySettings(out string modality, out string operationType, Dictionary<string, string> modalitySettings)
        {
            operationType = "controlnet";

            if (promptWindowUI.selectedOptionIndex > 0)
            {
                string optionName = promptWindowUI.correspondingOptionsValue[promptWindowUI.selectedOptionIndex - 1];
                if (!modalitySettings.ContainsKey(optionName))
                { 
                    modalitySettings.Add(optionName, $"{promptWindowUI.sliderValue:0.00}");
                }
            }

            modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
        }

        private string PrepareModality(Dictionary<string, string> modalitySettings)
        {
            string modality;
            if (promptWindowUI.isControlNet && promptWindowUI.isAdvancedSettings)
            {
                modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
            }
            else
            {
                modality = promptWindowUI.selectedPreset;
            }

            return modality;
        }

        private Dictionary<string, string> PrepareModalitySettings(ref string modality, ref string operationType)
        {
            Dictionary<string, string> modalitySettings = new();

            if (promptWindowUI.isAdvancedSettings)
            {
                PrepareAdvancedModalitySettings(out modality, out operationType, modalitySettings);
            }

            return modalitySettings;
        }

        private static string ProcessMask()
        {
            Texture2D processedMask = Texture2D.Instantiate(PromptWindowUI.imageMask);

            Color[] pixels = processedMask.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0)
                {
                    pixels[i] = Color.black;
                }
            }

            processedMask.SetPixels(pixels);
            processedMask.Apply();

            return CommonUtils.Texture2DToDataURL(processedMask);
        }

        #endregion

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
