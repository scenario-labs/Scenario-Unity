using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;

public class Scenario3DPackageInstaller : EditorWindow
{
    [MenuItem("Window/Scenario3D Package Installer")]
    public static void ShowWindow()
    {
        var window = GetWindow<Scenario3DPackageInstaller>("Scenario3D Package Installer");

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
            name = "2D Tilemap",
            url = "optional",
            scopes = new string[] {""},
            gitUrl = "com.unity.2d.tilemap"
        },
        new PackageData
        {
            name = "2D Tilemap extras",
            url = "optional",
            scopes = new string[] {""},
            gitUrl = "com.unity.2d.tilemap.extras"
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
        UnityEditor.PackageManager.Client.Add(gitUrl);
        Debug.Log("Package installation requested: " + gitUrl);
    }

    [System.Serializable]
    public struct Settings
    {
        public int FirstLoad;
        public int[] PackageSetupComplete;
        public int[] PackageInstallComplete;
    }

    private static string settingsPath => Path.Combine(Application.dataPath, "settings_3d.json");

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
