using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Method = RestSharp.Method;
using System.Globalization;
using System.Linq;

public class PromptWindow : EditorWindow
{
    internal static List<ImageDataStorage.ImageData> generatedImagesData = new List<ImageDataStorage.ImageData>();

    public PromptWindowUI promptWindowUI;

    private string inferenceId = "";
    private EditorCoroutine inferenceStatusCoroutine;
    private bool processReceivedUploadImage = false;    // for main thread receiving callback
    private byte[] pngBytesUploadImage = null;
    
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
        var imgBytes = texture2D.EncodeToPNG();
        string base64String = Convert.ToBase64String(imgBytes);
        string dataUrl = $"data:image/png;base64,{base64String}";
        EditorCoroutineUtility.StartCoroutineOwnerless(PutRemoveBackground(dataUrl));
    }

    IEnumerator PutRemoveBackground(string dataUrl)
    {
        string name = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";

        Debug.Log("Requesting background removal, please wait..");

        string url = $"{PluginSettings.ApiUrl}/images/erase-background";
        Debug.Log(url);

        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest(Method.PUT);

        string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{name}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";
        Debug.Log(param);

        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");
        request.AddParameter("application/json", param, ParameterType.RequestBody);

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.Log($"Error: {response.ErrorException.Message}");
            }
            else
            {
                Debug.Log($"Response: {response.Content}");

                try
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                    string imageUrl = jsonResponse.asset.url;

                    EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImageIntoMemory(imageUrl));
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred while processing the response: " + ex.Message);
                }
            }
        });
    }

    IEnumerator DownloadImageIntoMemory(string imageUrl)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                byte[] textureBytes = texture.EncodeToPNG();

                Callback_BackgroundRemoved(textureBytes);
            }
        }
    }

    private void Callback_BackgroundRemoved(byte[] textureBytes)
    {
        PromptWindowUI.imageUpload.LoadImage(textureBytes);
    }

    private void Update()
    {
        if (processReceivedUploadImage)
        {
            processReceivedUploadImage = false;
            Callback_BackgroundRemoved(pngBytesUploadImage);
        }
    }

    private void UpdateSelectedModel()
    {
        string selectedModelId = EditorPrefs.GetString("SelectedModelId");
        string selectedModelName = EditorPrefs.GetString("SelectedModelName");

        if (!string.IsNullOrEmpty(selectedModelId) && !string.IsNullOrEmpty(selectedModelName))
        {
            promptWindowUI.SelectedModelName = selectedModelName;
        }
        else
        {
            promptWindowUI.SelectedModelName = "Choose Model";
        }
    }

    private void OnGUI()
    {
        promptWindowUI.Render(this.position);
    }

    public void GenerateImage(string seed)
    {
        Debug.Log("Generate Image button clicked. Model: " + promptWindowUI.SelectedModelName + ", Seed: " + seed);

        string modelId = EditorPrefs.GetString("SelectedModelId");

        EditorCoroutineUtility.StartCoroutineOwnerless(PostInferenceRequest(modelId));
    }

    private IEnumerator PostInferenceRequest(string modelId)
    {
        Debug.Log("Requesting image generation please wait..");

        string apiKey = PluginSettings.EncodedAuth;

        string modality;
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

            var imgBytes = PromptWindowUI.imageUpload.EncodeToPNG();
            string base64String = Convert.ToBase64String(imgBytes);
            dataUrl = $"data:image/png;base64,{base64String}";
        }
        else if (promptWindowUI.isControlNet)
        {
            operationType = "controlnet";

            if (PromptWindowUI.imageUpload == null)
            {
                Debug.LogError("ControlNet Must have a image uploaded.");
                yield break;
            }

            var imgBytes = PromptWindowUI.imageUpload.EncodeToPNG();
            string base64String = Convert.ToBase64String(imgBytes);
            dataUrl = $"data:image/png;base64,{base64String}";
        }

        if (promptWindowUI.isImageToImage && promptWindowUI.isControlNet)
        {
            operationType = "controlnet";
        }

        Dictionary<string, string> modalitySettings = new Dictionary<string, string>();

        if (promptWindowUI.isAdvancedSettings)
        {
            operationType = "controlnet";

            if (promptWindowUI.selectedOption1Index > 0) {
                string option1Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption1Index - 1];
                if (!modalitySettings.ContainsKey(option1Name))
                    modalitySettings.Add(option1Name, $"{promptWindowUI.sliderValue1:0.00}");
            }

            if (promptWindowUI.selectedOption2Index > 0) {
                string option2Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption2Index - 1];
                if (!modalitySettings.ContainsKey(option2Name))
                    modalitySettings.Add(option2Name, $"{promptWindowUI.sliderValue2:0.00}");
            }

            if (promptWindowUI.selectedOption3Index > 0) {
                string option3Name = promptWindowUI.dropdownOptions[promptWindowUI.selectedOption3Index - 1];
                if (!modalitySettings.ContainsKey(option3Name))
                    modalitySettings.Add(option3Name, $"{promptWindowUI.sliderValue3:0.00}");
            }

            modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
        }

        if (promptWindowUI.isControlNet && promptWindowUI.isAdvancedSettings)
        {
            modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
        }
        else
        {
            modality = promptWindowUI.selectedPreset;
        }

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
                var imgBytes = PromptWindowUI.imageUpload.EncodeToPNG();
                string base64String = Convert.ToBase64String(imgBytes);
                dataUrl = $"data:image/png;base64,{base64String}";
            }

            if (PromptWindowUI.imageMask == null)
            {
                Debug.LogError("Inpainting Must have a mask uploaded.");
                yield break;
            }
            else
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

                var maskBytes = processedMask.EncodeToPNG();
                string maskBase64String = Convert.ToBase64String(maskBytes);
                maskDataUrl = $"data:image/png;base64,{maskBase64String}";
            }
        }

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
        int numSamples = (int)promptWindowUI.imagesliderValue;

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

        Debug.Log("Input Data: " + inputData);

        var client = new RestClient(ApiClient.apiUrl + "/models/" + modelId + "/inferences");

        var request = new RestRequest(Method.POST);

        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");

        request.AddParameter("application/json", inputData, ParameterType.RequestBody);

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.Log(response.ErrorException);
            }
            else
            {
                Debug.Log(response.Content);
                InferenceRoot inferenceRoot = JsonConvert.DeserializeObject<InferenceRoot>(response.Content);
                inferenceId = inferenceRoot.inference.id;

                inferenceStatusCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GetInferenceStatus());
            }
        });
    }

    IEnumerator GetInferenceStatus()
    {
        Debug.Log("Requesting status please wait..");

        yield return new WaitForSecondsRealtime(1.0f);

        string baseUrl = ApiClient.apiUrl + "/models";
        string modelId = UnityEditor.EditorPrefs.GetString("postedModelName");

        string url = $"{baseUrl}/{modelId}/inferences/{inferenceId}";
        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest(Method.GET);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.Log($"Error: {response.ErrorException.Message}");
            }
            else
            {
                Debug.Log($"Response: {response.Content}");
                InferenceStatusRoot inferenceStatusRoot = JsonConvert.DeserializeObject<InferenceStatusRoot>(response.Content);

                if (inferenceStatusRoot.inference.status == "in-progress")
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
                        Debug.Log("Image URL: " + item);
                        var img = JsonConvert.DeserializeObject<ImageDataAPI>(item.ToString());
                        generatedImagesData.Add(new ImageDataStorage.ImageData()
                        {
                            Id = img.Id,
                            Url = img.Url,
                            InferenceId = this.inferenceId
                        });
                    }
                    EditorCoroutineUtility.StopCoroutine(inferenceStatusCoroutine);
                    EditorCoroutineUtility.StartCoroutineOwnerless(ShowPromptImagesWindow());
                }
            }
        });
    }

    public class ImageDataAPI
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }

    public IEnumerator ShowPromptImagesWindow()
    {
        yield return null;
        PromptImages.ShowWindow();
    }

    IEnumerator PeriodicStatusCheck()
    {
        yield return new WaitForSecondsRealtime(4.0f);
        EditorCoroutineUtility.StopCoroutine(inferenceStatusCoroutine);
        inferenceStatusCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GetInferenceStatus());
    }

    public void SetSeed(string seed)
    {
        // Set the seed value here
    }

    public void SetSelectedModelName(string modelName)
    {
        promptWindowUI.SelectedModelName = modelName;
    }

    [System.Serializable]
    public class InferenceRoot
    {
        public Inference inference { get; set; }
    }

    [System.Serializable]
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

    [System.Serializable]
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

    [System.Serializable]
    public class InferenceStatusRoot
    {
        public Inference inference { get; set; }
    }

    [System.Serializable]
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

    [System.Serializable]
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

    [System.Serializable]
    public class Image
    {
        public string id { get; set; }
        public string url { get; set; }
    }
}