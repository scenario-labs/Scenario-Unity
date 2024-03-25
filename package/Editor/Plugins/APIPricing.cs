using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class APIPricingWindow : EditorWindow
    {
        [MenuItem("Window/Scenario/API Pricing", false, 101)]
        public static void ShowWindow()
        {
            Application.OpenURL("https://www.scenario.com/api-pricing");
        }
    }
}