using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Scenario.Editor
{
    public class BackgroundRemoval
    {
        public static void RemoveBackground(Texture2D texture2D, Action<byte[]> callback)
        {
            string dataUrl = CommonUtils.Texture2DToDataURL(texture2D);
            string fileName = CommonUtils.GetRandomImageFileName();

            UploadTexture(fileName, dataUrl, assetId =>
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError("Asset upload failed, background removal cannot proceed.");
                    callback?.Invoke(null);
                    return;
                }

                RequestBackgroundRemovalJob(assetId, callback);
            });
        }

        private static void UploadTexture(string fileName, string dataUrl, Action<string> assetIdCallback)
        {
            string url = $"assets";
            var payload = new
            {
                image = dataUrl,
                name = fileName
            };
            string param = JsonConvert.SerializeObject(payload);

            Debug.Log("Uploading texture as asset...");

            ApiClient.RestPost(url, param, response =>
            {
                try
                {
                    Debug.Log("Asset upload response: " + response.Content);
                    BgAssetResponse assetResponse = JsonConvert.DeserializeObject<BgAssetResponse>(response.Content);
                    if (assetResponse != null && assetResponse.asset != null && !string.IsNullOrEmpty(assetResponse.asset.id))
                    {
                        Debug.Log("Asset uploaded successfully, assetId: " + assetResponse.asset.id);
                        assetIdCallback?.Invoke(assetResponse.asset.id);
                    }
                    else
                    {
                        Debug.LogError("Asset upload response did not contain a valid assetId.");
                        assetIdCallback?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error during asset upload: " + ex.Message);
                    assetIdCallback?.Invoke(null);
                }
            }, errorAction =>
            {
                Debug.LogError("API request for asset upload failed: " + errorAction);
                assetIdCallback?.Invoke(null);
            });
        }

        private static void RequestBackgroundRemovalJob(string assetId, Action<byte[]> callback)
        {
            string url = $"generate/remove-background";

            var payload = new
            {
                image = assetId,
                backgroundColor = "",
                format = "png",
                returnImage = false,
                originalAssets = true
            };
            string param = JsonConvert.SerializeObject(payload);

            Debug.Log("Requesting background removal job using assetId...");

            ApiClient.RestPost(url, param, response =>
            {
                try
                {
                    Debug.Log("Background removal job response: " + response.Content);
                    BgRoot jsonResponse = JsonConvert.DeserializeObject<BgRoot>(response.Content);
                    string jobId = jsonResponse.job.jobId;

                    Scenario.Editor.Jobs.CheckJobStatus(jobId, asset =>
                    {
                        CommonUtils.FetchTextureFromURL(asset.url, texture =>
                        {
                            byte[] textureBytes = texture.EncodeToPNG();
                            callback?.Invoke(textureBytes);
                        });
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error during background removal job processing: " + ex.Message);
                    callback?.Invoke(null);
                }
            }, errorAction =>
            {
                Debug.LogError("API request for background removal job failed: " + errorAction);
                callback?.Invoke(null);
            });
        }

        public class BgRoot
        {
            public BgAsset asset { get; set; }
            public BgJob job { get; set; }
            public string image { get; set; }
        }

        public class BgAsset
        {
            public string id { get; set; }
            public string url { get; set; }
            public string mimeType { get; set; }
            public BgMetadata metadata { get; set; }
            public string ownerId { get; set; }
            public string authorId { get; set; }
            public string description { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public string privacy { get; set; }
            public List<object> tags { get; set; }
            public List<object> collectionIds { get; set; }
            public string status { get; set; }
            public List<string> editCapabilities { get; set; }
        }

        public class BgMetadata
        {
            public string type { get; set; }
            public string parentId { get; set; }
            public string rootParentId { get; set; }
            public string kind { get; set; }
            public string backgroundColor { get; set; }
            public string format { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int size { get; set; }
        }

        public class BgJob
        {
            public string jobId { get; set; }
            public string jobType { get; set; }
            public string status { get; set; }
            public float progress { get; set; }
            public BgMetadata metadata { get; set; }
            public string type { get; set; }
            public string preset { get; set; }
            public string parentId { get; set; }
            public string rootParentId { get; set; }
            public string kind { get; set; }
            public string[] assetIds { get; set; }
            public int scalingFactor { get; set; }
            public bool magic { get; set; }
            public bool forceFaceRestoration { get; set; }
            public bool photorealist { get; set; }
        }

        // Corrected BgAssetResponse DTO to match nested JSON structure
        public class BgAssetResponse
        {
            [JsonProperty("asset")]
            public BgAssetResponseAsset asset { get; set; }

            public class BgAssetResponseAsset
            {
                public string id { get; set; }
            }
        }
    }
}