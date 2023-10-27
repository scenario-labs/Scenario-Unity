using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class PromptWindow : EditorWindow
    {
        internal static List<ImageDataStorage.ImageData> generatedImagesData = new();

        public static PromptWindowUI promptWindowUI;

        private string inferenceId = "";
        private EditorCoroutine inferenceStatusCoroutine;
        private bool processReceivedUploadImage = false;    // for main thread receiving callback
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
    
        internal void RemoveBackground(Texture2D texture2D)
        {
            string dataUrl = CommonUtils.Texture2DToDataURL(texture2D);
            fileName = CommonUtils.GetRandomImageFileName();
            string url = $"images/erase-background";
            string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{fileName}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";

            Debug.Log("Requesting background removal, please wait..");

            ApiClient.RestPut(url,param,response =>
            {
                try
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                    string imageUrl = jsonResponse.asset.url;
                    CommonUtils.FetchTextureFromURL(imageUrl, texture =>
                    {
                        byte[] textureBytes = texture.EncodeToPNG();
                        Callback_BackgroundRemoved(textureBytes);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred while processing the response: " + ex.Message);
                }
            });
        }
    
        private void Callback_BackgroundRemoved(byte[] textureBytes)
        {
            PromptWindowUI.imageUpload.LoadImage(textureBytes);
        }

        private void Update()
        {
            if (!processReceivedUploadImage) return;
        
            processReceivedUploadImage = false;
            Callback_BackgroundRemoved(pngBytesUploadImage);
        }

        private void UpdateSelectedModel()
        {
            string selectedModelId = EditorPrefs.GetString("SelectedModelId");
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

            string modelId = EditorPrefs.GetString("SelectedModelId");

            EditorCoroutineUtility.StartCoroutineOwnerless(PostInferenceRequest(modelId));
        }

        private IEnumerator PostInferenceRequest(string modelId)
        {
            Debug.Log("Requesting image generation please wait..");
        
            string modality = "";
            string operationType = "txt2img";
            string dataUrl = "\"\"";
            string maskDataUrl = "\"\"";

            if (promptWindowUI.isImageToImage)
            {
                operationType = "img2img";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("Img2Img Must have a image uploaded.");
                    yield break;
                }

                dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
            }
            else if (promptWindowUI.isControlNet)
            {
                operationType = "controlnet";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("ControlNet Must have a image uploaded.");
                    yield break;
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
                    yield break;
                }
                else
                {
                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
                }

                if (PromptWindowUI.imageMask == null)
                {
                    Debug.LogError("Inpainting Must have a mask uploaded.");
                    yield break;
                }
                else
                {
                    maskDataUrl = ProcessMask();
                }
            }

            string inputData = PrepareInputData(modality, operationType, dataUrl, maskDataUrl);

            Debug.Log("Input Data: " + inputData);

            ApiClient.RestPost($"models/{modelId}/inferences", inputData,response =>
            {
                InferenceRoot inferenceRoot = JsonConvert.DeserializeObject<InferenceRoot>(response.Content);
                inferenceId = inferenceRoot.inference.id;
                inferenceStatusCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GetInferenceStatus());
            });
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
    
        private string PrepareInputData(string modality, string operationType, string dataUrl, string maskDataUrl)
        {
            bool hideResults = false;
            string type = operationType;
            string image = $"\"{dataUrl}\"";
            string mask = $"\"{maskDataUrl}\"";
            string prompt = promptWindowUI.promptinputText;
            int seed = int.Parse(promptWindowUI.seedinputText);
            string negativePrompt = promptWindowUI.negativepromptinputText;
            float strength = (float)Math.Round(promptWindowUI.influncesliderValue, 2);
            float guidance = promptWindowUI.guidancesliderValue;
            int width = (int)promptWindowUI.widthSliderValue;
            int height = (int)promptWindowUI.heightSliderValue;
            int numInferenceSteps = (int)promptWindowUI.samplesliderValue;
            int numSamples = (int)promptWindowUI.imagesliderIntValue;

            string inputData = $@"{{
            ""parameters"": {{
                ""hideResults"": {hideResults.ToString().ToLower()},
                ""type"": ""{type}"",
                {(promptWindowUI.isImageToImage || promptWindowUI.isInpainting || promptWindowUI.isControlNet ? $@"""image"": ""{dataUrl}""," : "")}
                {(promptWindowUI.isControlNet || promptWindowUI.isAdvancedSettings ? $@"""modality"": ""{modality}""," : "")}
                {(promptWindowUI.isInpainting ? $@"""mask"": ""{maskDataUrl}""," : "")}
                ""prompt"": ""{prompt}"",
                {(seed > 0 ? $@"""seed"": {seed}," : "")}
                {(string.IsNullOrEmpty(negativePrompt) ? "" : $@"""negativePrompt"": ""{negativePrompt}"",")}
                {(promptWindowUI.isImageToImage || promptWindowUI.isControlNet ? $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)}," : "")}
                ""guidance"": {guidance.ToString("F2", CultureInfo.InvariantCulture)},
                ""numInferenceSteps"": {numInferenceSteps},
                ""width"": {width},
                ""height"": {height},
                ""numSamples"": {numSamples}
            }}
        }}";
            return inputData;
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

        private IEnumerator GetInferenceStatus()
        {
            Debug.Log("Requesting status please wait..");

            yield return new WaitForSecondsRealtime(1.0f);

            string modelId = UnityEditor.EditorPrefs.GetString("postedModelName");

            ApiClient.RestGet($"models/{modelId}/inferences/{inferenceId}",response =>
            {
                InferenceStatusRoot inferenceStatusRoot = JsonConvert.DeserializeObject<InferenceStatusRoot>(response.Content);

                if (inferenceStatusRoot.inference.status != "succeeded" && inferenceStatusRoot.inference.status != "failed" )
                {
                    Debug.Log("Commission in process, please wait..");
                    EditorCoroutineUtility.StopCoroutine(inferenceStatusCoroutine);
                    inferenceStatusCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(PeriodicStatusCheck());
                }
                else
                {
                    generatedImagesData.Clear();
                    foreach (var item in inferenceStatusRoot.inference.images)
                    {
                        /*Debug.Log("Image URL: " + item);*/
                        var img = JsonConvert.DeserializeObject<ImageDataAPI>(item.ToString());
                        generatedImagesData.Add(new ImageDataStorage.ImageData()
                        {
                            Id = img.Id,
                            Url = img.Url,
                            InferenceId = this.inferenceId,
                            Prompt = promptWindowUI.promptinputText,
                            Steps = promptWindowUI.samplesliderValue,
                            Size = new Vector2(promptWindowUI.widthSliderValue, promptWindowUI.heightSliderValue),
                            Guidance = promptWindowUI.guidancesliderValue,
                            Scheduler = "Default",
                            Seed = promptWindowUI.seedinputText,
                        });
                    }
                    EditorCoroutineUtility.StopCoroutine(inferenceStatusCoroutine);
                    EditorCoroutineUtility.StartCoroutineOwnerless(ShowPromptImagesWindow());
                }
            });
        }

        public class ImageDataAPI
        {
            public string Id { get; set; }
            public string Url { get; set; }
        }

        private IEnumerator ShowPromptImagesWindow()
        {
            yield return null;
            PromptImages.ShowWindow();
        }

        private IEnumerator PeriodicStatusCheck()
        {
            yield return new WaitForSecondsRealtime(4.0f);
            EditorCoroutineUtility.StopCoroutine(inferenceStatusCoroutine);
            inferenceStatusCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GetInferenceStatus());
        }

        public void SetSeed(string seed)
        {
            // Set the seed value here
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
            public int seed { get; set; }
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
