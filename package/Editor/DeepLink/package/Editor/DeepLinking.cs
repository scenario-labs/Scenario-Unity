#if UNITY_2019_1_OR_NEWER

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Needle.Deeplink
{
    /// <summary>
    /// Allows to intercept web deep links going to Unity.
    /// </summary>
    /// <returns>
    /// Return true if the method matched, return false if you want other handlers to still process it.
    /// </returns>
    /// <remarks>
    /// Methods marked with this attribute must return bool and take a single string as input.
    /// You can use regex expressions directly with the RegexFilter property; you'll receive the first group capture (if any) as input in that case.
    /// Regular AssetStore deep links have the form: com.unity3d.kharma:content/160139. These are always left through.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DeepLinkAttribute : Attribute
    {
        public string RegexFilter { get; set; } = null;
        public RegexOptions RegexOptions { get; set; } = RegexOptions.None;
        public int Priority { get; set; } = -100000;
    }
    
    internal class DeepLinking
    {
        [InitializeOnLoadMethod]
        static void InitializePatch()
        {
            var type = typeof(UnityEditor.PackageManager.UI.Window).Assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
            var method = type?.GetMethod("OpenURL", (BindingFlags)(-1));
            if (method == null) return;

            try
            {
                var h = new Harmony(nameof(DeepLinking));
                h.Patch(method, new HarmonyMethod(typeof(DeepLinking), nameof(Prefix)));
            }
            catch (Exception)
            {
                // ignore
            }
        }
        
#if !BUILTIN_UPM_DEEPLINKS
        [DeepLink(RegexFilter = @"com.unity3d.kharma:upmpackage/(.*)")]
        // ReSharper disable once UnusedMember.Local
        private static bool InstallPackage(string url)
        {
            if (EditorUtility.DisplayDialog("Install Package", "This will install " + url + ".", "Install", "Cancel"))
                InstallPackageByIdentifier(url);
            return true;
        }
        
        private static async void InstallPackageByIdentifier(string identifier)
        {
            Debug.Log("[Deep Link] Installing package: " + identifier);
            var request = Client.Add(identifier);
            while (!request.IsCompleted)
                await Task.Delay(100);
            if (request.Status != StatusCode.Success)
                Debug.LogWarning("Package " + identifier + " couldn't be installed.");
        }
#endif

        // Some more samples.
        
        // [DeepLink]
        // private static bool LogDeepLink(string url)
        // {
        //     Debug.Log("[Deep Link] Received: " + url);
        //     return false;
        // }
        
        // [DeepLink(RegexFilter = @"com.unity3d.kharma:custom\/(.*)")]
        // private static bool LogCustomData(string data)
        // {
        //     Debug.Log("[Deep Link] Data: " + data);
        //     return true;
        // }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedParameter.Local
        private static bool Prefix(object __instance, string url)
        {
            // check for regular AssetStore URLs and prevent those from being processed
            if (Regex.IsMatch(url, @"com.unity3d.kharma:content\/(.*)"))
                return true;

            var methods = TypeCache.GetMethodsWithAttribute<DeepLinkAttribute>()
                .OrderByDescending(x => x.GetCustomAttribute<DeepLinkAttribute>().Priority)
                .ToList();
            
            // check if any listener wants to catch this
            foreach (var method in methods)
            {
                if (method.ReturnType != typeof(bool)) {
                    Debug.LogWarning("Method " + method.Name + " is marked as DeepLink but does not return bool.");
                    continue;
                }

                if (!method.IsStatic) {
                    Debug.LogWarning("Method " + method.Name + " is marked as DeepLink but is not static.");
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || (parameters.Length == 1 && parameters[0].ParameterType != typeof(string))) {
                    Debug.LogWarning("Method " + method.Name + " is marked as DeepLink but must have exactly one string parameter.");
                    continue;
                }

                var attr = method.GetCustomAttribute<DeepLinkAttribute>();
                if (!string.IsNullOrEmpty(attr.RegexFilter))
                {
                    var match = Regex.Match(url, attr.RegexFilter, attr.RegexOptions);
                    if(!match.Success)
                        continue;
                    if(match.Groups.Count > 1)
                        url = match.Groups[1].Value;
                } 

                var hasMatched = (bool) method.Invoke(null, new object[] { url });
                if (hasMatched)
                    return false;
            }
            
            // if this is not an AssetStore link, we should not open PackMan for it even if no handler is attached
            if(methods.Count > 0)
                Debug.LogWarning(("[Deep Link] No method found to handle deep link request, will fall back to Unity's handling of it: " + url));
            return true;
        }
    }
}

#endif