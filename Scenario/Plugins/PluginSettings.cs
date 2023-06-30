using System;
using UnityEditor;
using UnityEngine;

public class PluginSettings : EditorWindow
{
    private string apiKey;
    private string secretKey;
    private string saveFolder;
    private int imageFormatIndex;
    public static string ApiUrl => "https://api.cloud.scenario.com/v1";
    public static string ApiKey => EditorPrefs.GetString("ApiKey");
    public static string SecretKey => EditorPrefs.GetString("SecretKey");

    private readonly string[] imageFormats = { "JPEG", "PNG" };
    private readonly string[] imageFormatExtensions = { "jpeg", "png" };

    private static string version = "Scenario Beta Version 0.1.1";

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

    private void OnGUI()
    {
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), backgroundColor);
        
        GUILayout.Space(10);

        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        secretKey = EditorGUILayout.PasswordField("Secret Key", secretKey);

        GUILayout.Space(10);

        GUILayout.Label("Image Settings", EditorStyles.boldLabel);

        imageFormatIndex = EditorGUILayout.Popup("Image Format", imageFormatIndex, imageFormats);

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

        GUILayout.FlexibleSpace(); // This will push the version text to the bottom
        GUILayout.Label(version, EditorStyles.boldLabel); // Add this line to display the version at the bottom
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void SaveSettings()
    {
        EditorPrefs.SetString("ApiKey", apiKey);
        EditorPrefs.SetString("SecretKey", secretKey);
        EditorPrefs.SetString("SaveFolder", saveFolder);
        EditorPrefs.SetString("ImageFormat", imageFormatExtensions[imageFormatIndex]);

        PlayerPrefs.SetString("EncodedAuth", EncodedAuth);
    }

    private void LoadSettings()
    {
        apiKey = EditorPrefs.GetString("ApiKey");
        secretKey = EditorPrefs.GetString("SecretKey");
        saveFolder = EditorPrefs.GetString("SaveFolder", "Assets");

        string imageFormat = EditorPrefs.GetString("ImageFormat", "jpeg");
        imageFormatIndex = System.Array.IndexOf(imageFormatExtensions, imageFormat);
        if (imageFormatIndex < 0)
        {
            imageFormatIndex = 0;
        }
    }
}
