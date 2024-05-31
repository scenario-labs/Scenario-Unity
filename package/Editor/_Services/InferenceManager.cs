using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Scenario.Editor
{
    /// <summary>
    /// InferenceManager Class manage all API request about image generation (post inference generation, get inferences generated)
    /// </summary>
    public class InferenceManager
    {
        public static List<string> cancelledInferences = new();

        /// <summary>
        /// Active this boolean when user use a specific workflow
        /// </summary>
        public static bool SilenceMode = false;

        /// <summary>
        /// Ask Scenario API to get the cost and also limitation of the inference request.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="_onInferenceRequested"></param>
        public static void PostAskInferenceRequest(string inputData, Action<string> _onInferenceRequested)
        {
            
            string modelName = UnityEditor.EditorPrefs.GetString("postedModelName");
            string modelId = DataCache.instance.SelectedModelId;

            ApiClient.RestPost($"models/{modelId}/inferences?dryRun=true", inputData,response =>
            {
                if (response.Content.Contains("creativeUnitsCost"))
                {
                    string cost = GetPriceCost(response.Content).ToString();
                    _onInferenceRequested?.Invoke(cost);
                }
            });
        }

        public static void PostInferenceRequest(string inputData, int imagesliderIntValue,
            string promptinputText, int samplesliderValue, float widthSliderValue, float heightSliderValue,
            float guidancesliderValue, string _schedulerText, string seedinputText, Action<string> _onInferenceRequested = null)
        {
            Debug.Log("Requesting image generation please wait..");

            string modelName = UnityEditor.EditorPrefs.GetString("postedModelName");
            string modelId = DataCache.instance.SelectedModelId;

            ApiClient.RestPost($"models/{modelId}/inferences", inputData, response =>
            {
                PromptWindow.InferenceRoot inferenceRoot = JsonConvert.DeserializeObject<PromptWindow.InferenceRoot>(response.Content);

                string inferenceId = inferenceRoot.inference.id;
                int numImages = imagesliderIntValue;

                DataCache.instance.ReserveSpaceForImageDatas(numImages, inferenceId,
                    promptinputText,
                    samplesliderValue,
                    widthSliderValue,
                    heightSliderValue,
                    guidancesliderValue,
                    _schedulerText,
                    seedinputText,
                    modelId);

                GetInferenceStatus(inferenceId, modelId);
                if (!SilenceMode)
                {
                    Images.ShowWindow();
                }
                _onInferenceRequested?.Invoke(inferenceId);
            });
        }

        private static async void GetInferenceStatus(string inferenceId, string modelId)
        {
            Debug.Log("Requesting status please wait..");

            await Task.Delay(4000);

            if (cancelledInferences.Contains(inferenceId))
            {
                DataCache.instance.RemoveInferenceData(inferenceId);
                cancelledInferences.Remove(inferenceId);
                return;
            }

            if (DataCache.instance.GetReservedSpaceCount() <= 0)
            {
                return;
            }
            
            ApiClient.RestGet($"models/{modelId}/inferences/{inferenceId}",response =>
            {
                InferenceStatusRoot inferenceStatusRoot = JsonConvert.DeserializeObject<InferenceStatusRoot>(response.Content);

                if (inferenceStatusRoot.inference.status != "succeeded" && 
                    inferenceStatusRoot.inference.status != "failed" )
                {
                    Debug.Log($"Commission in process, please wait...");
                    GetInferenceStatus(inferenceId, modelId);
                }
                else
                {
                    if (inferenceStatusRoot.inference.status == "failed")
                    {
                        Debug.LogError("Api Response: Status == failed, Try Again..");
                        return;
                    }
                    
                    foreach (var item in inferenceStatusRoot.inference.images)
                    {
                        //Debug.Log("Image : " + item.ToString());
                        var img = JsonConvert.DeserializeObject<ImageDataAPI>(item.ToString());
                        DataCache.instance.FillReservedSpaceForImageData(
                            inferenceId, 
                            img.Id,
                            img.Url,
                            inferenceStatusRoot.inference.createdAt,
                            inferenceStatusRoot.inference.parameters.scheduler,
                            img.Seed);
                    }

                    if (!SilenceMode)
                    { 
                        Images.ShowWindow();
                    }
                }
            });
        }

        /// <summary>
        /// Regular expression after api return to extract cost of the result.
        /// </summary>
        /// <param name="_data"></param>
        /// <returns> int cost </returns>
        private static int GetPriceCost(string _data)
        {
            // Define the regular expression pattern to match numbers after colon
            string pattern = @":(\d+)";

            // Create a regex object
            Regex regex = new Regex(pattern);

            // Match the pattern against the data
            Match match = regex.Match(_data);

            int parsedNumber = -1;
            // Check if there's a match
            if (match.Success)
            {
                // Extract the matched number
                string number = match.Groups[1].Value;

                // Convert the number to an integer if needed
                parsedNumber = int.Parse(number);

                return parsedNumber;
            }
            else
            {
                return -1;
            }
        }

        public class ImageDataAPI
        {
            public string Id { get; set; }
            public string Url { get; set; }
            public string Seed { get; set; }
        }
        
        [Serializable]
        public class InferenceStatusRoot
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
            public string scheduler { get; set; }
            public string image { get; set; }
            public string prompt { get; set; }
            public string mask { get; set; }
        }
    }
}