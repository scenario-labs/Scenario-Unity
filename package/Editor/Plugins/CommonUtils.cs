using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Scenario.Editor
{
    [ExecuteInEditMode]
    public static class CommonUtils
    {
        public static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static void SaveImageDataAsPNG(byte[] pngBytes, string fileName = "", Preset importPreset = null, Action<string> callback_OnSaved = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = GetRandomImageFileName();
            }

            SaveImage(pngBytes, fileName, importPreset, callback_OnSaved);
        }

        public static void SaveTextureAsPNG(Texture2D texture2D, string fileName = "", Preset importPreset = null, Action<string> callback_OnSaved = null)
        {
            SaveImageDataAsPNG(texture2D.EncodeToPNG(), fileName, importPreset, callback_OnSaved);
        }

        private static void SaveImage(byte[] pngBytes, string fileName, Preset importPreset, Action<string> callback_OnSaved = null)
        {
            if (importPreset == null || string.IsNullOrEmpty(importPreset.name))
            {
                importPreset = PluginSettings.TexturePreset;
            }

            string downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
            string filePath = Path.Combine(downloadPath, fileName);

            File.WriteAllBytesAsync(filePath, pngBytes).ContinueWith(task =>
            {
                try
                {
                    RefreshAssetDatabase(() =>
                    {
                        Debug.Log("Downloaded image to: " + filePath);
                        ApplyImportSettingsFromPreset(filePath, importPreset);
                        callback_OnSaved?.Invoke(filePath);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static void RefreshAssetDatabase(Action callback_OnRefreshed = null)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(Routine_RefreshDatabase(callback_OnRefreshed));
        }

        private static IEnumerator Routine_RefreshDatabase(Action callback_OnRefreshed = null)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            yield return new WaitForEndOfFrame();
            callback_OnRefreshed?.Invoke();
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
            return "image" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
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
            string base64 = dataUrl.Replace("data:image/png;base64,", "");
            base64 = base64.Replace("data:image/jpeg;base64,", "");
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(1, 1);
            ImageConversion.LoadImage(texture, bytes);
            return texture;
        }

        public static string GetRandomSeedValue()
        {
            return UnityEngine.Random.Range(ulong.MinValue, ulong.MaxValue).ToString("n", CultureInfo.InvariantCulture).Replace(",", "").Substring(0, 19);
        }

        private static void ApplyImportSettingsFromPreset(string filePath, Preset importPreset)
        {
            TextureImporter tImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (tImporter != null)
            {
                importPreset.ApplyTo(tImporter);
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath));
            }
            else
            {
                Debug.LogError("There was an issue when applying the preset. please restart the editor.");
            }
        }

        public static List<T> GetSubObjectsOfType<T>(Object asset) where T : Object
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));
            List<T> ofType = new List<T>();
            foreach (Object o in objs)
            {
                if (o is T)
                {
                    ofType.Add(o as T);
                }
            }
            return ofType;
        }

        public static void ApplyPixelsPerUnit(string filePath)
        {
            TextureImporter tImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (tImporter != null)
            {
                int width = 0;
                int height = 0;
                tImporter.GetSourceTextureWidthAndHeight(out width, out height);

                tImporter.spritePixelsPerUnit = width;
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                Debug.LogError("There was an issue when applying the Pixels Per Unit parameter. please restart the editor.");
            }
        }

        public static void ReplaceSprite(SpriteRenderer spriteRenderer, Texture2D newTexture)
        {
            if (spriteRenderer != null && newTexture != null)
            {
                Rect spriteRect = new Rect(0, 0, newTexture.width, newTexture.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Sprite newSprite = Sprite.Create(newTexture, spriteRect, pivot);

                spriteRenderer.sprite = newSprite;
            }
        }
    }
}
