using System;
using System.Collections;
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
            string url = $"images/erase-background";
            string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{fileName}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";

            Debug.Log("Requesting background removal, please wait..");

            ApiClient.RestPut(url,param,response =>
            {
                try
                {
                    Debug.Log(response.Content);
                    Root jsonResponse = JsonConvert.DeserializeObject<BackgroundRemoval.Root>(response.Content);
                    string imageUrl = jsonResponse.asset.url;
                    CommonUtils.FetchTextureFromURL(imageUrl, texture =>
                    {
                        byte[] textureBytes = texture.EncodeToPNG();
                        callback?.Invoke(textureBytes);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred while processing the response: " + ex.Message);
                }
            });
        }
        
        public class Asset
        {
            public string id { get; set; }
            public string url { get; set; }
            public string mimeType { get; set; }
            public Metadata metadata { get; set; }
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

        public class Metadata
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

        public class Root
        {
            public Asset asset { get; set; }
            public string image { get; set; }
        }
    }
}
