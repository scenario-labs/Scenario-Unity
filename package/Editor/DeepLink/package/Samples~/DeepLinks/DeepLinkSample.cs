using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Needle.Deeplink.Samples
{
    public class DeepLinkSample : MonoBehaviour
    {
#if UNITY_EDITOR
        [DeepLink]
        private static bool LogAllDeepLinkRequestsAndPassThrough(string url)
        {
            Debug.Log("[Deep Link] Received deeplink: " + url);
            return false;
        }

        [DeepLink(RegexFilter = @"com.unity3d.kharma:custom\/(.*)")]
        private static bool CaptureCustomData(string data)
        {
            Debug.Log("[Deep Link] Custom Data received: " + data);
            return true;
        }
        
        [DeepLink(RegexFilter = @"com.unity3d.kharma:open-scene\/(.*)")]
        private static bool OpenScene(string sceneName)
        {
            Debug.Log("[Deep Link] Opening Scene: " + sceneName);
            
            // look for scene
            foreach (var guid in AssetDatabase.FindAssets("t:scene " + sceneName))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                return true;
            }
            
            Debug.LogWarning("Received open-scene deeplink but no scene with this name was found: " + sceneName);
            return true;
        }
        
        [DeepLink(RegexFilter = @"com.unity3d.kharma:selected-sample\/(.*)")]
        private static bool CallMethodOnGameObject(string data)
        {
            var objects = GameObject.FindObjectsOfType<DeepLinkSample>();
            foreach (var obj in objects)
            {
                if (data.StartsWith(obj.name))
                {
                    EditorGUIUtility.PingObject(obj);
                    obj.DeepLinkCalled(data);
                    return true;
                }
            }
            Debug.LogWarning("Received selected-sample deeplink but no object with the specified name found in the current active scenes: " + data);
            return true;
        }
#endif
        
        // callback on the actual object
        private void DeepLinkCalled(string data)
        {
            Debug.Log("[deep Link] Received: " + data + " on object " + name, this);
        }
    }
}
