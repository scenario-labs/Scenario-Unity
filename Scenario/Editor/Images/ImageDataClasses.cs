using System;
using System.Collections.Generic;
using System.Numerics;

namespace Scenario
{
    public static class ImageDataStorage
    {
        public static List<ImageData> imageDataList = new List<ImageData>();

        public class ImageData
        {
            public string Id { get; set; }
            public string Url { get; set; }
            public string InferenceId { get; set; }
            public string Prompt { get; set; }
            public float Steps { get; set; }
            public UnityEngine.Vector2 Size { get; set; }
            public float Guidance { get; set; }
            public string Scheduler { get; set; }
            public string Seed { get; set; }
        }
    }

    public class InferencesResponse
    {
        public List<Inference> inferences { get; set; }
        public string nextPaginationToken { get; set; }
    }

   /* public class Inference
    {
        public string id { get; set; }
        public List<Image> images { get; set; }
    }*/

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
        public int guidance { get; set; }
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
        public int progress { get; set; }
        public string displayPrompt { get; set; }
    }


}