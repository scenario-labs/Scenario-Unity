using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;

public class ScenarioPackageInstaller : EditorWindow
{
    [MenuItem("Window/Scenario2D Package Installer")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScenarioPackageInstaller>("Scenario2D Package Installer");

        var settings = LoadSettings();

        if (settings.FirstLoad == 0)
        {
            settings.PackageSetupComplete_0 = 1;
            settings.PackageSetupComplete_1 = 1;
            settings.PackageSetupComplete_2 = 1;
            settings.PackageSetupComplete_3 = 1;
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

            int setupComplete = (i == 0) ? settings.PackageSetupComplete_0 : settings.PackageSetupComplete_1;
            int installComplete = (i == 0) ? settings.PackageInstallComplete_0 :
                                (i == 1) ? settings.PackageInstallComplete_1 :
                                (i == 2) ? settings.PackageInstallComplete_2 :
                                            settings.PackageInstallComplete_3;

            EditorGUI.BeginDisabledGroup(setupComplete == 1);
            if (GUILayout.Button("Setup", GUILayout.Width(100)))
            {
                Debug.Log("Setup button clicked for package: " + packages[i].name);
                if (i == 0)
                    settings.PackageSetupComplete_0 = 1;
                else
                    settings.PackageSetupComplete_1 = 1;

                SaveSettings(settings);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(installComplete == 1);
            if (GUILayout.Button("Install", GUILayout.Width(100)))
            {
                Debug.Log("Install button clicked for package: " + packages[i].name);
                AddPackage(packages[i].gitUrl, i);
                if (i == 0)
                    settings.PackageInstallComplete_0 = 1;
                else if (i == 1)
                    settings.PackageInstallComplete_1 = 1;
                else if (i == 2)
                    settings.PackageInstallComplete_2 = 1;
                else
                    settings.PackageInstallComplete_3 = 1;

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
        public int PackageSetupComplete_0;
        public int PackageSetupComplete_1;
        public int PackageSetupComplete_2;
        public int PackageSetupComplete_3;
        public int PackageInstallComplete_0;
        public int PackageInstallComplete_1;
        public int PackageInstallComplete_2;
        public int PackageInstallComplete_3;
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