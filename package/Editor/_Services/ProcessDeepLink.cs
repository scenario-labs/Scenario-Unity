using Needle.Deeplink;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ProcessDeepLinkMngr
{
    [DeepLink]
    private static bool LogAllDeepLinkRequestsAndPassThrough(string url)
    {
        Debug.Log("[Deep Link] Received deeplink: " + url);
        return false;
    }

    [DeepLink]
    private static bool DownloadImage(string url)
    {
        Debug.Log("[Deep Link] Received deeplink: " + url);
        string json = url.Replace("com.unity3d.kharma:", "");
        ScenarioDeepLink sdl = Newtonsoft.Json.JsonConvert.DeserializeObject< ScenarioDeepLink > (json);
        Debug.Log(sdl.link);
        return true;
    }
}

class ScenarioDeepLink
{
    public string link;
}

