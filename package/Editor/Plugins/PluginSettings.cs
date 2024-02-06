using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace Scenario
{
    public class PluginSettings : EditorWindow
    {
        private static string assemblyDefinitionFileName = "com.scenarioinc.scenario.editor";
        private string apiKey;
        private string secretKey;
        private string saveFolder;
        private int imageFormatIndex;
        public static string ApiUrl => "https://api.cloud.scenario.com/v1";
        public static string ApiKey => EditorPrefs.GetString("ApiKey");
        public static string SecretKey => EditorPrefs.GetString("SecretKey");

        private static string vnumber => GetVersionFromPackageJson();
        private static string version => $"Scenario Beta Version {vnumber}";

        [System.Serializable]
        private class PackageInfo
        {
            public string version;
        }

        /// <summary>
        /// Get the correct version number from the package JSON
        /// </summary>
        /// <returns>The version of the plugin, as a string</returns>
        private static string GetVersionFromPackageJson()
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
            folderPath = folderPath.Remove(folderPath.IndexOf($"Editor/{assemblyDefinitionFileName}.asmdef"));

            //find the package.json inside this folder
            string packageJsonPath = $"{folderPath}/package.json";
            string packageJsonContent = File.ReadAllText(packageJsonPath);
            return JsonUtility.FromJson<PackageInfo>(packageJsonContent).version;
        }

        public static string EncodedAuth
        {
            get
            {
                string apiKey = EditorPrefs.GetString("ApiKey");
                string secretKey = EditorPrefs.GetString("SecretKey");
                string authString = apiKey + ":" + secretKey;
                byte[] authBytes = System.Text.Encoding.UTF8.GetBytes(authString);
                string encodedAuth = Convert.ToBase64String(authBytes);
                return encodedAuth;
            }
        }

        private static float minimumWidth = 400f;

        [MenuItem("Window/Scenario/Scenario Settings")]
        public static void ShowWindow()
        {
            GetWindow<PluginSettings>("Scenario Settings");
            PluginSettings window = GetWindow<PluginSettings>("Scenario Settings");
            window.minSize = new Vector2(minimumWidth, window.minSize.y);
        }

        private void OnEnable()
        {
            GetVersionFromPackageJson();
            LoadSettings();
        }

        private void OnGUI()
        {
            Color backgroundColor = new Color32(18, 18, 18, 255);
            EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), backgroundColor);

            GUILayout.Space(10);

            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            secretKey = EditorGUILayout.PasswordField("Secret Key", secretKey);

            GUILayout.Space(10);

            GUILayout.Label("Download Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Save Folder: ", GUILayout.Width(80));
            saveFolder = EditorGUILayout.TextField(saveFolder);

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Save Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    if (folder.StartsWith(Application.dataPath))
                    {
                        saveFolder = "Assets" + folder.Replace(Application.dataPath, "");
                    }
                    else
                    {
                        saveFolder = folder;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(version, EditorStyles.boldLabel);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("ApiKey", apiKey);
            EditorPrefs.SetString("SecretKey", secretKey);
            EditorPrefs.SetString("SaveFolder", saveFolder);

            PlayerPrefs.SetString("EncodedAuth", EncodedAuth);
        }

        private void LoadSettings()
        {
            apiKey = EditorPrefs.GetString("ApiKey");
            secretKey = EditorPrefs.GetString("SecretKey");
            saveFolder = EditorPrefs.GetString("SaveFolder", "Assets");
        }
    }
}