using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class APIPricingWindow : EditorWindow
    {
        [MenuItem("Scenario/API Pricing", false, 101)]
        public static void ShowWindow()
        {
            Application.OpenURL("https://docs.scenario.com/page/api-pricing");
        }
    }
}