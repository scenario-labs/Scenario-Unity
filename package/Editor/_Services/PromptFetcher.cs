using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Scenario.Editor
{
    public class PromptFetcher
    {
        public static List<string> cancelledInferences = new();
        
        public static void PostInferenceRequest(string inputData, int imagesliderIntValue,
            string promptinputText, float samplesliderValue, float widthSliderValue, float heightSliderValue,
            float guidancesliderValue, string seedinputText)
        {
            Debug.Log("Requesting image generation please wait..");
            
            string modelName = UnityEditor.EditorPrefs.GetString("postedModelName");
            string modelId = DataCache.instance.SelectedModelId;
            
            ApiClient.RestPost($"models/{modelId}/inferences", inputData,response =>
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
                    "Default",
                    seedinputText,
                    modelId);

                GetInferenceStatus(inferenceId, modelId);
                Images.ShowWindow();
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
                    Debug.Log("Commission in process, please wait..");
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
                        /*Debug.Log("Image URL: " + item);*/
                        var img = JsonConvert.DeserializeObject<ImageDataAPI>(item.ToString());
                        DataCache.instance.FillReservedSpaceForImageData(
                            inferenceId, 
                            img.Id,
                            img.Url,
                            inferenceStatusRoot.inference.createdAt);
                    }
                }
            });
        }
        
        public class ImageDataAPI
        {
            public string Id { get; set; }
            public string Url { get; set; }
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
            public string image { get; set; }
            public string prompt { get; set; }
            public string mask { get; set; }
        }
    }
}