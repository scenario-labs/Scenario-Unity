using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

public class ScenarioPackageInstallerTwo : EditorWindow
{
    [MenuItem("Window/Scenario Package Installer 2")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScenarioPackageInstallerTwo>("Scenario Package Installer Two");

        var settings = LoadSettings();

        if (settings.FirstLoad == 0)
        {
            settings.PackageSetupComplete = new int[] { 0 };
            settings.PackageInstallComplete = new int[] { 0 };
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
            name = "package.openupm.com",
            url = "https://package.openupm.com",
            scopes = new string[] {"com.adrenak.restsharp.unity"},
            gitUrl = "com.adrenak.restsharp.unity@1.1.0"
        }
    };

    private void OnGUI()
    {
        var settings = LoadSettings();

        for (int i = 0; i < packages.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(packages[i].gitUrl, GUILayout.Width(200));
            EditorGUILayout.LabelField(packages[i].url, GUILayout.Width(200));

            EditorGUI.BeginDisabledGroup(settings.PackageSetupComplete[i] == 1);
            if (GUILayout.Button("Setup", GUILayout.Width(100)))
            {
                Debug.Log("Setup button clicked for package: " + packages[i].name);
                settings.PackageSetupComplete[i] = 1;
                SaveSettings(settings);

                AddScopedRegistry(packages[i].name, packages[i].url, packages[i].scopes);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(settings.PackageInstallComplete[i] == 1);
            if (GUILayout.Button("Install", GUILayout.Width(100)))
            {
                Debug.Log("Install button clicked for package: " + packages[i].name);
                AddPackage(packages[i].gitUrl);
                settings.PackageInstallComplete[i] = 1;
                SaveSettings(settings);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }
    }

    private void AddPackage(string gitUrl)
    {
        UnityEditor.PackageManager.Client.Add(gitUrl);
        Debug.Log("Package installation requested: " + gitUrl);
    }

    private static void AddScopedRegistry(string name, string url, string[] scopes)
    {
        var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

        if (File.Exists(manifestPath))
        {
            var manifestJSON = File.ReadAllText(manifestPath);
            var manifest = JObject.Parse(manifestJSON);

            var registries = (JArray)manifest["scopedRegistries"];
            if (registries != null && registries.Any(r => (string)r["url"] == url))
                return;

            var registry = new JObject
            {
                ["name"] = name,
                ["url"] = url,
                ["scopes"] = new JArray(scopes)
            };

            if (registries == null)
            {
                manifest["scopedRegistries"] = new JArray(registry);
            }
            else
            {
                registries.Add(registry);
            }

            File.WriteAllText(manifestPath, manifest.ToString());
        }
    }

    [System.Serializable]
    public struct Settings
    {
        public int FirstLoad;
        public int[] PackageSetupComplete;
        public int[] PackageInstallComplete;
    }

    private static string settingsPath => Path.Combine(Application.dataPath, "settings_two.json");

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