
using Needle.Deeplink;
using Scenario.Editor;
using UnityEditor;
using UnityEngine;

namespace Scenario
{ 
    /// <summary>
    /// Manage the reception of deeplink information.
    /// </summary>
    public class DeepLinkManager : EditorWindow
    {
        #region Public Fields
        #endregion

        #region Private Fields

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
#if UNITY_EDITOR

        /// <summary>
        /// Allow to download an image sended by the webApp.
        /// </summary>
        /// <param name="_link"></param>
        /// <returns></returns>
        [DeepLink]
        private static bool DeepLinkDownloadImage(string _link)
        {
            Debug.Log("[Deep Link] Received deeplink: " + _link);
            string url = _link.Replace("com.unity3d.kharma://", "");
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                url = url.Replace("https//", "https://");
            }
            CommonUtils.FetchTextureFromURL(url, response =>
            {
                CommonUtils.SaveTextureAsPNG(response, importPreset: PluginSettings.TexturePreset);
            });

            return true;
        }

    #endif
        #endregion
    }
}