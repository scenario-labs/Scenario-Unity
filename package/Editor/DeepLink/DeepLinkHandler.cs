#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

[InitializeOnLoad]
public class DeepLinkHandler
{
    private const string SCHEME = "com.scenarioinc.scenario";
    private static bool isInitialized = false;

    static DeepLinkHandler()
    {
        if (!isInitialized)
        {
            EditorApplication.update += OnEditorUpdate;
            isInitialized = true;
        }
    }

    private static void OnEditorUpdate()
    {
        // Check for command line arguments that might contain our deep link
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.StartsWith(SCHEME + "://"))
            {
                HandleDeepLink(arg);
                break;
            }
        }
    }

    public static void HandleDeepLink(string deepLinkUrl)
    {
        if (string.IsNullOrEmpty(deepLinkUrl)) return;

        try
        {
            Uri uri = new Uri(deepLinkUrl);
            if (uri.Scheme == SCHEME)
            {
                // Extract the URL from the deep link
                string imageUrl = uri.Host + uri.PathAndQuery;
                ScenarioImageImporter.ProcessImageUrl(imageUrl);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to process deep link: {e.Message}");
        }
    }
}
#endif 