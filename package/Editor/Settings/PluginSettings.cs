using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Presets;

namespace Scenario.Editor
{
    [InitializeOnLoad]
    public class PluginSettings : EditorWindow
    {
        #region Public Properties

        public static string ApiUrl => "https://api.cloud.scenario.com/v1";
        public static Preset TexturePreset { get { return GetPreset(EditorPrefs.GetString("scenario/texturePreset")); } }
        public static Preset SpritePreset { get { return GetPreset(EditorPrefs.GetString("scenario/spritePreset")); } }
        public static Preset TilePreset { get { return GetPreset(EditorPrefs.GetString("scenario/tilePreset")); } }
        public static bool AlwaysRemoveBackgroundForSprites { get { return alwaysRemoveBackgroundForSprites; } }
        public static bool UsePixelsUnitsEqualToImage { get { return usePixelUnitsEqualToImage; } }
        #endregion

        #region Private Properties

        private static string assemblyDefinitionFileName = "com.scenarioinc.scenario.editor";
        private string apiKey;
        private string secretKey;
        private string saveFolder;
        private static float minimumWidth = 400f;

        private static Preset texturePreset;
        private string texturePresetGUID = null;

        private Preset spritePreset;
        private string spritePresetGUID = null;
        private static bool alwaysRemoveBackgroundForSprites = true;
        private static bool usePixelUnitsEqualToImage = true;

        private Preset tilePreset;
        private string tilePresetGUID = null;

        private static string vnumber => GetVersionFromPackageJson();
        private static string version => $"Scenario Beta Version {vnumber}";

        #endregion

        [System.Serializable]
        private class PackageInfo
        {
            public string version;
        }

        #region Unity Methods

        private void OnEnable()
        {
            GetVersionFromPackageJson();
            LoadSettings();
        }


        [MenuItem("Window/Scenario/Scenario Settings")]
        public static void ShowWindow()
        {
            GetWindow<PluginSettings>("Scenario Settings");
            PluginSettings window = GetWindow<PluginSettings>("Scenario Settings");
            window.minSize = new Vector2(minimumWidth, window.minSize.y);
        }


        private void OnGUI()
        {
            Color backgroundColor = new Color32(18, 18, 18, 255);
            EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), backgroundColor);

            GUILayout.Space(10);
            DrawAPISettings();
            GUILayout.Space(10);
            DrawImageSettings();
            GUILayout.Space(10);
            DrawTextureSettings();
            GUILayout.Space(10);
            DrawSpriteSettings();
            GUILayout.Space(10);
            DrawTileSettings();
            GUILayout.Space(10);

            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(version, EditorStyles.boldLabel);
        }

        #endregion

        #region Draw Functions

        private void DrawAPISettings()
        {
            GUILayout.Label("API Settings", EditorStyles.boldLabel);

            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            secretKey = EditorGUILayout.PasswordField("Secret Key", secretKey);
        }

        private void DrawImageSettings()
        {
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
        }

        private void DrawTextureSettings()
        {
            GUILayout.Label("Texture Settings", EditorStyles.boldLabel);
            texturePreset = (Preset)EditorGUILayout.ObjectField("Texture Preset", texturePreset, typeof(Preset), false);
            texturePresetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texturePreset));
        }

        private void DrawSpriteSettings()
        {
            GUILayout.Label("Sprite Settings", EditorStyles.boldLabel);
            spritePreset = (Preset)EditorGUILayout.ObjectField("Sprite Preset", spritePreset, typeof(Preset), false);
            spritePresetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spritePreset));

            alwaysRemoveBackgroundForSprites = GUILayout.Toggle(alwaysRemoveBackgroundForSprites, new GUIContent("Always Remove Background For Sprites", "Will call the remove background API before downloading your images as sprite."));
            usePixelUnitsEqualToImage = GUILayout.Toggle(usePixelUnitsEqualToImage, new GUIContent("Set Pixels Per Unit equal to image width", "If disable, the downloaded sprites will set the Pixels Per Unit settings equal to the value in the Preset. If enable, it will uses the width of the downloaded sprite as the value for Pixels per Unit."));
        }

        private void DrawTileSettings()
        {
            GUILayout.Label("Tile Settings", EditorStyles.boldLabel);
            tilePreset = (Preset)EditorGUILayout.ObjectField("Tile Preset", tilePreset, typeof(Preset), false);
            tilePresetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tilePreset));

            usePixelUnitsEqualToImage = GUILayout.Toggle(usePixelUnitsEqualToImage, new GUIContent("Set Pixels Per Unit equal to image width", "If disable, the downloaded sprites will set the Pixels Per Unit settings equal to the value in the Preset. If enable, it will uses the width of the downloaded sprite as the value for Pixels per Unit."));
        }

        /// <summary>
        /// Get a preset file from its GUID
        /// </summary>
        /// <param name="_GUID"></param>
        /// <returns></returns>
        private static Preset GetPreset(string _GUID)
        {
            if (_GUID != null && !string.IsNullOrEmpty(_GUID))
            {
                return AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(_GUID));
            }
            else
            {
                Debug.LogError("Cannot find a texture preset with a GUID that is null. Please go Windows/Scenario/ScenarioSettings and make sure the Texture Preset is set.");
                return null;
            }
        }


        #endregion


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

        private void SaveSettings()
        {
            EditorPrefs.SetString("scenario/texturePreset", texturePresetGUID);
            EditorPrefs.SetString("scenario/spritePreset", spritePresetGUID);
            EditorPrefs.SetString("scenario/tilePreset", tilePresetGUID);
            EditorPrefs.SetString("ApiKey", apiKey);
            EditorPrefs.SetString("SecretKey", secretKey);
            EditorPrefs.SetString("SaveFolder", saveFolder);
            PlayerPrefs.SetString("EncodedAuth", EncodedAuth);
        }

        private void LoadSettings()
        {
            /// Register default values

            if (!EditorPrefs.HasKey("scenario/texturePreset") /*&& string.IsNullOrEmpty(EditorPrefs.GetString("scenario/texturePreset"))*/)
            {
                EditorPrefs.SetString("scenario/texturePreset", "28269680c775243409a2d470907383f9"); //change this value in case the meta file change
            }
            else if(string.IsNullOrEmpty(EditorPrefs.GetString("scenario/texturePreset")))
            {
                EditorPrefs.SetString("scenario/texturePreset", "28269680c775243409a2d470907383f9"); //change this value in case the meta file change
            }
  
            if (!EditorPrefs.HasKey("scenario/spritePreset"))
            {
                EditorPrefs.SetString("scenario/spritePreset", "d87ceacdb68f56745951dadf104120b1"); //change this value in case the meta file change
            }
            else if (string.IsNullOrEmpty(EditorPrefs.GetString("scenario/spritePreset")))
            {
                EditorPrefs.SetString("scenario/spritePreset", "d87ceacdb68f56745951dadf104120b1"); //change this value in case the meta file change
            }

            if (!EditorPrefs.HasKey("scenario/tilePreset"))
            {
                EditorPrefs.SetString("scenario/tilePreset", "6d537ab5bf7649b44973f061c34b6151"); //change this value in case the meta file change
            }
            else if (string.IsNullOrEmpty(EditorPrefs.GetString("scenario/tilePreset")))
            {
                EditorPrefs.SetString("scenario/tilePreset", "6d537ab5bf7649b44973f061c34b6151"); //change this value in case the meta file change
            }

            //load values
            apiKey = EditorPrefs.GetString("ApiKey");
            secretKey = EditorPrefs.GetString("SecretKey");
            saveFolder = EditorPrefs.GetString("SaveFolder", "Assets");

            texturePresetGUID = EditorPrefs.GetString("scenario/texturePreset");
            texturePreset = GetPreset(texturePresetGUID);

            spritePresetGUID = EditorPrefs.GetString("scenario/spritePreset");
            spritePreset = GetPreset(spritePresetGUID);

            tilePresetGUID = EditorPrefs.GetString("scenario/tilePreset");
            tilePreset = GetPreset(tilePresetGUID);
        }

    }
}