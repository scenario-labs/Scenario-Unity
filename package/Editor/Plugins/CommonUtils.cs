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

        private static string assemblyDefinitionFileName = "com.scenarioinc.scenario.editor";

        /// <summary>
        /// Return the path to the root of the folder of the plugin
        /// </summary>
        /// <returns></returns>
        public static string PluginFolderPath()
        {
            //Find the assembly Definition which should be at package/Editor/ folder because it's a unique file.
            string[] guids = AssetDatabase.FindAssets($"{assemblyDefinitionFileName} t:assemblydefinitionasset");

            if (guids.Length > 1)
            {
                Debug.LogError($"it seems that you have multiple file '{assemblyDefinitionFileName}.asmdef'. Please delete one");
                return "0";
            }

            if (guids.Length == 0)
            {
                Debug.LogError($"It seems that you don't have the file '{assemblyDefinitionFileName}.asmdef'. Please redownload the plugin from the asset store.");
                return "0";
            }

            //find the folder of that file
            string folderPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return folderPath.Remove(folderPath.IndexOf($"Editor/{assemblyDefinitionFileName}.asmdef"));
        }
        public static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Take a byte array and save it as a file in the folder set in the Plugin Settings.
        /// </summary>
        /// <param name="pngBytes">The byte array to save as an image</param>
        /// <param name="fileName">The file name you want. If null, it will take a random number</param>
        /// <param name="importPreset">The preset you want to apply. if null, will use the default texture preset</param>
        public static void SaveImageDataAsPNG(byte[] pngBytes, string fileName = "", Preset importPreset = null, Action<string> callback_OnSaved = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = GetRandomImageFileName();
            }

            SaveImage(pngBytes, fileName, importPreset, callback_OnSaved);
        }

        /// <summary>
        /// Take a Texture2D and save it as a file in the folder set in the Plugin Settings.
        /// </summary>
        /// <param name="pngBytes">The byte array to save as an image</param>
        /// <param name="fileName">The file name you want. If null, it will take a random number</param>
        /// <param name="importPreset">The preset you want to apply. if null, will use the default texture preset</param>
        public static void SaveTextureAsPNG(Texture2D texture2D, string fileName = "", Preset importPreset = null, Action<string> callback_OnSaved = null)
        {
            SaveImageDataAsPNG(texture2D.EncodeToPNG(), fileName, importPreset, callback_OnSaved);
        }

        //possible improvement : Implement error handling and messages for cases where image loading or actions like "Download as Texture" fail. Inform the user of the issue and provide options for resolution or retries.
        private static void SaveImage(byte[] pngBytes, string fileName, Preset importPreset, Action<string> callback_OnSaved = null)
        {
            if (importPreset == null || string.IsNullOrEmpty(importPreset.name))
            {
                //Debug.LogWarning("Preset for this image is not set, using the one by default.");
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
        /// Found here : https://discussions.unity.com/t/editor-class-quot-texture-importer-quot-apply-import-settings-question/2538/4
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

                importPreset.ApplyTo(tImporter);
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath));
            }
            else
            {
                Debug.LogError("There was an issue when applying the preset. please restart the editor.");
            }
        }

        /// <summary>
        /// Get all sub assets from an asset.
        /// found here : https://forum.unity.com/threads/accessing-subobjects-in-an-asset.266731/#post-1762981
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Return a specific subObject inside an object
        /// </summary>
        /// <typeparam name="T"> Type expected </typeparam>
        /// <param name="asset"> Reference asset </param>
        /// <returns></returns>
        public static T GetSubObjectOfType<T>(Object asset) where T : Object
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));
            foreach (Object o in objs)
            {
                if (o is T)
                {
                    return (T)o;
                }
            }
            return null;
        }

        /// <summary>
        /// Use this function to modify the Pixels per Unit parameter of the texture
        /// </summary>
        /// <param name="filePath"></param>
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
    }
}