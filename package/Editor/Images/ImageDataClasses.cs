using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace Scenario.Editor
{
    public static class ImageDataStorage
    {
        [System.Serializable]
        public class ImageData
        {
            [SerializeField] public string Id;
            [SerializeField] public string Url;
            [SerializeField] public string InferenceId;
            [SerializeField] public string Prompt;
            [SerializeField] public float Steps;
            [SerializeField] public UnityEngine.Vector2 Size;
            [SerializeField] public float Guidance;
            [SerializeField] public string Scheduler;
            [SerializeField] public string Seed;
            [SerializeField] public DateTime CreatedAt;
            [SerializeField] public Texture2D texture;
            [SerializeField] public bool isProcessedByPromptImages;
        }
    }

    public class InferencesResponse
    {
        public List<Inference> inferences { get; set; }
        public string nextPaginationToken { get; set; }
    }

    public class Image
    {
        public string id { get; set; }
        public string url { get; set; }
        public string seed { get; set; }
    }

    public class TokenResponse
    {
        public string nextPaginationToken { get; set; }
    }

    public class Parameters
    {
        public bool intermediateImages { get; set; }
        public float guidance { get; set; }
        public int numInferenceSteps { get; set; }
        public int numSamples { get; set; }
        public int tokenMerging { get; set; }
        public int width { get; set; }
        public bool hideResults { get; set; }
        public string type { get; set; }
        public string prompt { get; set; }
        public string negativePrompt { get; set; }
        public int height { get; set; }
    }

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
        public List<Image> images { get; set; }
        public int imagesNumber { get; set; }
        public float progress { get; set; }
        public string displayPrompt { get; set; }
    }


}