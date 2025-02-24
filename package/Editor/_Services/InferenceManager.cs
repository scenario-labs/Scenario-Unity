using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Scenario.Editor
{
    public class InferenceManager
    {
        public static List<string> cancelledInferences = new List<string>();
        public static bool SilenceMode = false;

        private static string operationName;  // Added declaration
        private static CreationMode activeMode;  // Added declaration

        public static void PostAskInferenceRequest(string inputData, Action<string> _onInferenceRequested)
        {
            activeMode = PromptPusher.Instance.GetActiveMode();  // Assign value

            operationName = activeMode.OperationName;
            operationName = operationName.Replace("_", "-");
            ApiClient.RestPost($"generate/{operationName}?dryRun=true", inputData, response =>
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
            activeMode = PromptPusher.Instance.GetActiveMode();  // Assign value

            Debug.Log("Requesting image generation please wait..");
            operationName = activeMode.OperationName;
            operationName = operationName.Replace("_", "-");

            Debug.Log($"Input Data: {inputData}");
            ApiClient.RestPost($"generate/{operationName}", inputData, response =>
            {
                Debug.Log("Raw API Response: " + response.Content); 

                InferenceJobRoot inferenceJob = JsonConvert.DeserializeObject<InferenceJobRoot>(response.Content);
                string jobId = inferenceJob.job.jobId;
                int numImages = imagesliderIntValue;
                DataCache.instance.ReserveSpaceForImageDatas(numImages, jobId,
                    promptinputText,
                    samplesliderValue,
                    widthSliderValue,
                    heightSliderValue,
                    guidancesliderValue,
                    _schedulerText,
                    seedinputText,
                    DataCache.instance.SelectedModelId);
                Jobs.CheckJobStatus(jobId, asset =>
                {
                    DataCache.instance.FillReservedSpaceForImageData(
                        jobId,
                        asset.id,
                        asset.url,
                        DateTime.Now,
                        "",
                        "");
                    if (!SilenceMode)
                    {
                        Images.ShowWindow();
                    }
                    _onInferenceRequested?.Invoke(jobId);
                });
            });
        }

        private static int GetPriceCost(string _data)
        {
            string pattern = @":(\d+)";
            Regex regex = new Regex(pattern);
            var match = regex.Match(_data);
            int parsedNumber = -1;
            if (match.Success)
            {
                string number = match.Groups[1].Value;
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
        public class InferenceJobRoot
        {
            public InferenceJob job { get; set; }
        }

        [Serializable]
        public class InferenceJob
        {
            public string jobId { get; set; }
            public string status { get; set; }
            public DateTime createdAt { get; set; }
            public string scheduler { get; set; }
            public List<object> images { get; set; }
        }
    }
}
