using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using System.Collections;

public class ScenarioPackageInstaller : EditorWindow
{
    [MenuItem("Window/Scenario2D Package Installer")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScenarioPackageInstaller>("Scenario2D Package Installer");

        var settings = LoadSettings();

        if (settings.FirstLoad == 0)
        {
            settings.PackageSetupComplete = new int[4] { 1, 1, 1, 1 };
            settings.PackageInstallComplete = new int[4] { 0, 0, 0, 0 };
            settings.FirstLoad = 1;
            SaveSettings(settings);
        }
    }

    public struct PackageData
    {
        public string name;
        public string url;
        public string[] scopes;
        public string gitUrl;
    }

    public List<PackageData> packages = new List<PackageData>
    {
        new PackageData
        {
            name = "newtonsoft-json",
            url = "",
            scopes = new string[] {""},
            gitUrl = "com.unity.nuget.newtonsoft-json@3.2.1"
        },
        new PackageData
        {
            name = "Unitask",
            url = "",
            scopes = new string[] {""},
            gitUrl = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.1.0"
        },
        new PackageData
        {
            name = "Editor Coroutines",
            url = "",
            scopes = new string[] {""},
            gitUrl = "com.unity.editorcoroutines"
        }
    };

    private void OnGUI()
    {
        var settings = LoadSettings();

        for (int i = 0; i < packages.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(packages[i].name, GUILayout.Width(200));

            EditorGUI.BeginDisabledGroup(settings.PackageSetupComplete[i] == 1);
            if (GUILayout.Button("Setup", GUILayout.Width(100)))
            {
                Debug.Log("Setup button clicked for package: " + packages[i].name);
                settings.PackageSetupComplete[i] = 1;
                SaveSettings(settings);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(settings.PackageInstallComplete[i] == 1);
            if (GUILayout.Button("Install", GUILayout.Width(100)))
            {
                Debug.Log("Install button clicked for package: " + packages[i].name);
                AddPackage(packages[i].gitUrl, i);
                settings.PackageInstallComplete[i] = 1;
                SaveSettings(settings);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void AddPackage(string gitUrl, int packageIndex)
    {
        var request = UnityEditor.PackageManager.Client.Add(gitUrl);
        EditorCoroutineUtility.StartCoroutineOwnerless(CheckAddRequest(request, packageIndex));
    }

    private IEnumerator CheckAddRequest(AddRequest request, int packageIndex)
    {
        while (!request.IsCompleted)
        {
            yield return null;
        }

        if (request.Status == StatusCode.Success)
        {
            Debug.Log("Package installed successfully: " + request.Result.packageId);
        }
        else if (request.Status >= StatusCode.Failure)
        {
            Debug.LogError("Failed to install package: " + request.Error.message);
        }
    }

    [System.Serializable]
    public struct Settings
    {
        public int FirstLoad;
        public int[] PackageSetupComplete;
        public int[] PackageInstallComplete;
    }

    private static string settingsPath => Path.Combine(Application.dataPath, "settings.json");

    public static Settings LoadSettings()
    {
        if (!File.Exists(settingsPath))
            return new Settings();

        var settingsJson = File.ReadAllText(settingsPath);
        return JsonUtility.FromJson<Settings>(settingsJson);
    }

    public static void SaveSettings(Settings settings)
    {
        var settingsJson = JsonUtility.ToJson(settings);
        File.WriteAllText(settingsPath, settingsJson);
    }
}