using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Scenario
{
    public static class CommonUtils
    {
        public static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static void SaveTextureAsPNGAtPath(Texture2D texture2D, string filePath)
        {
            if (filePath == null || filePath == "") { Debug.LogError("Must have valid file path"); return; }
            byte[] pngBytes = texture2D.EncodeToPNG();
            SaveImageBytesToPath(filePath, pngBytes);
        }

        public static void SaveImageBytesToPath(string filePath, byte[] pngBytes)
        {
            File.WriteAllBytes(filePath, pngBytes);
            RefreshAssetDatabase();
            Debug.Log("Saved image to: " + filePath);
        }

        public static void SaveTextureAsPNG(Texture2D texture2D, string fileName = "")
        {
            if (fileName == null || fileName == "") { fileName = GetRandomImageFileName(); }
            byte[] pngBytes = texture2D.EncodeToPNG();
            SaveImageBytesToFile(fileName, pngBytes);
        }
    
        public static void SaveImageBytesToFile(string fileName, byte[] pngBytes)
        {
            string downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
            string filePath = downloadPath + "/" + fileName;
            File.WriteAllBytes(filePath, pngBytes);
            RefreshAssetDatabase();
            Debug.Log("Downloaded image to: " + filePath);
        }

        public static void RefreshAssetDatabase()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(Routine_RefreshDatabase());
        }
    
        private static IEnumerator Routine_RefreshDatabase()
        {
            yield return null;
            AssetDatabase.Refresh();
        }

        public static void FetchTextureFromURL(string imageUrl, Action<Texture2D> response)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(Routine_FetchTextureFromUrl(imageUrl, response));
        }
    
        private static IEnumerator Routine_FetchTextureFromUrl(string imageUrl, Action<Texture2D> response)
        {
            using UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        
            yield return www.SendWebRequest();
        
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                response?.Invoke(texture);
            }
        }
    
        public static async Task<Texture2D> FetchTextureFromURLAsync(string imageUrl)
        {
            using UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        
            var task = www.SendWebRequest();

            while (!task.isDone)
            {
                await Task.Delay(1000);
            }
        
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                return null;
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                return texture;
            }
        }

        public static string GetRandomImageFileName()
        {
            return "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
        }

        public static string Texture2DToDataURL(Texture2D texture2D)
        {
            var imgBytes = texture2D.EncodeToPNG();
            string base64String = Convert.ToBase64String(imgBytes);
            string dataUrl = $"data:image/png;base64,{base64String}";
            return dataUrl;
        }
    
        public static Texture2D DataURLToTexture2D(string dataUrl)
        {
            string base64 = dataUrl.Replace("data:image/png;base64,","");
            base64 = base64.Replace("data:image/jpeg;base64,", "");
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(1,1);
            ImageConversion.LoadImage(texture, bytes);
            return texture;
        }

        public static string GetRandomSeedValue()
        {
            return UnityEngine.Random.Range(ulong.MinValue, ulong.MaxValue).ToString("n", CultureInfo.InvariantCulture).Replace(",", "").Substring(0, 19);
        }
    }
}