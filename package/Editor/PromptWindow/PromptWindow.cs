using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class PromptWindow : EditorWindow
    {
        public static PromptWindowUI promptWindowUI;
        
        private bool processReceivedUploadImage = false;
        private byte[] pngBytesUploadImage = null;
        private string fileName;

        [MenuItem("Window/Scenario/Prompt Window")]
        public static void ShowWindow()
        {
            GetWindow<PromptWindow>("Prompt Window");
        }

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

        private void OnGUI()
        {
            promptWindowUI.Render(this.position);
        }

        public void GenerateImage(string seed)
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
                    promptWindowUI.seedinputText);
            }
        }

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
            string image = $"\"{dataUrl}\"";
            string mask = $"\"{maskDataUrl}\"";
            string prompt = promptWindowUI.promptinputText;
            string seedField = "";
            
            if (promptWindowUI.seedinputText != "-1")
            {
                ulong seed = ulong.Parse(promptWindowUI.seedinputText);
                seedField = $@"""seed"": {seed},";
            }
            
            string negativePrompt = promptWindowUI.negativepromptinputText;
            float strength = Math.Clamp(1-(float)Math.Round(promptWindowUI.influncesliderValue, 2), 0.01f, 0.99f); //strength is 1-influence
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

            if (promptWindowUI.selectedOption1Index > 0)
            {
                string option1Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption1Index - 1];
                if (!modalitySettings.ContainsKey(option1Name))
                    modalitySettings.Add(option1Name, $"{promptWindowUI.sliderValue1:0.00}");
            }

            if (promptWindowUI.selectedOption2Index > 0)
            {
                string option2Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption2Index - 1];
                if (!modalitySettings.ContainsKey(option2Name))
                    modalitySettings.Add(option2Name, $"{promptWindowUI.sliderValue2:0.00}");
            }

            if (promptWindowUI.selectedOption3Index > 0)
            {
                string option3Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption3Index - 1];
                if (!modalitySettings.ContainsKey(option3Name))
                    modalitySettings.Add(option3Name, $"{promptWindowUI.sliderValue3:0.00}");
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
