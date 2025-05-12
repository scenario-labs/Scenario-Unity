#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class DeepLinkRegistration
{
    [MenuItem("Scenario/Register Deep Link Handler")]
    public static void RegisterDeepLinkHandler()
    {
        #if UNITY_EDITOR_OSX
        string infoPlistPath = Path.Combine(Application.dataPath, "..", "Info.plist");
        string sourcePlistPath = Path.Combine(Application.dataPath, "Editor/DeepLink/Info.plist");

        if (File.Exists(sourcePlistPath))
        {
            File.Copy(sourcePlistPath, infoPlistPath, true);
            Debug.Log("Deep link handler registered successfully!");
        }
        else
        {
            Debug.LogError("Could not find Info.plist template!");
        }
        #else
        Debug.LogWarning("Deep link registration is currently only supported on macOS.");
        #endif
    }
}
#endif 