using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Networking;

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

        //possible improvement : Implement error handling and messages for cases where image loading or actions like "Download as Texture" fail. Inform the user of the issue and provide options for resolution or retries.
        public static void SaveTextureAsPNG(Texture2D texture2D, string fileName = "", Preset importPreset = null)
        {
            if (importPreset == null || string.IsNullOrEmpty(importPreset.name))
            {
                //Debug.LogWarning("Preset for this image is not set, using the one by default.");
                importPreset = PluginSettings.TexturePreset;
            }

            if (fileName == null || fileName == "") { fileName = GetRandomImageFileName(); }

            byte[] pngBytes = texture2D.EncodeToPNG();
            SaveImageBytesToFile(fileName, pngBytes, (filePath) =>
            {
                ApplyImportSettingsFromPreset(filePath, importPreset);
            });
        }


        public static void SaveImageBytesToFile(string fileName, byte[] pngBytes, Action<string> callback_OnAssetSaved = null)
        {
            string downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
            string filePath = downloadPath + "/" + fileName;
            File.WriteAllBytesAsync(filePath, pngBytes).ContinueWith(task =>
            {
                try
                {
                    RefreshAssetDatabase(() =>
                    {
                        Debug.Log("Downloaded image to: " + filePath);
                        callback_OnAssetSaved?.Invoke(filePath);
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
                await Task.Delay(1000); //wtf ?
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

        /// <summary>
        /// Use this function to apply a specific Importer Preset to an image. (for example: apply the sprite settings if the user wants to make a sprite out of a generated image)
        /// </summary>
        /// <param name="image">The image to apply the parameters</param>
        /// <param name="preset">The preset that contains the parameter</param>
        private static void ApplyImportSettingsFromPreset(string filePath, Preset importPreset)
        {
            TextureImporter tImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (tImporter != null)
            {
                //Debug.Log($"Applying preset to {filePath}");
                //Debug.Log($"importPreset C {importPreset.name}");
                //Debug.Log($"tImporter {tImporter}");

                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                importPreset.ApplyTo(tImporter);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath));
            }
            else
            {
                Debug.LogError("There was an issue when applying the preset. please restart the editor.");
            }
        }
    }
}