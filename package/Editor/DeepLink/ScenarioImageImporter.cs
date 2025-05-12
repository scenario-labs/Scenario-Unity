#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System;

public static class ScenarioImageImporter
{
    private const string SCHEME = "com.scenarioinc.scenario";

    [MenuItem("Scenario/Import Image from URL")]
    public static void ImportFromURL()
    {
        string url = EditorUtility.OpenFilePanel("Paste Image URL", "", "");
        if (string.IsNullOrEmpty(url)) return;

        ProcessImageUrl(url);
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
                ProcessImageUrl(imageUrl);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to process deep link: {e.Message}");
        }
    }

    public static void ProcessImageUrl(string url)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAndSave(url));
    }

    private static IEnumerator DownloadAndSave(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image download failed: " + www.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(www);
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, "ScenarioImports", "downloadedImage.png");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();
        Debug.Log("Image imported to Assets/ScenarioImports/downloadedImage.png");
    }
}
#endif 