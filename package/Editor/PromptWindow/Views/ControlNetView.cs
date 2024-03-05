using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public partial class PromptWindowUI
    {
        private void RenderControlNetFoldout()
        {
            if (!controlNetFoldout) return;
        
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enable ControlNet", EditorStyles.label);
            isControlNet = GUILayout.Toggle(isControlNet, "");

            if (isControlNet)
            {
                GUILayout.Label("Advanced Settings", EditorStyles.label);
                isAdvancedSettings = GUILayout.Toggle(isAdvancedSettings, "");
            }

            GUILayout.EndHorizontal();

            if (!isControlNet) return;
        
            CustomStyle.Space(20);

            if (isAdvancedSettings)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Modality :", EditorStyles.label);

                List<string> availableOptions1 = new List<string> { "None" };
                availableOptions1.AddRange(dropdownOptions);
                selectedOption1Index = EditorGUILayout.Popup(selectedOption1Index, availableOptions1.ToArray());

                if (selectedOption1Index > 0)
                { 
                    GUILayout.Label("Guidance :", EditorStyles.label);
                    sliderDisplayedValue = (int)EditorGUILayout.Slider(Mathf.Clamp(sliderDisplayedValue, 0, 100), 0, 100);
                    sliderValue = sliderDisplayedValue / 100.0f;
                    if (sliderValue == 0)
                    {
                        sliderValue = 0.1f;
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Presets:", EditorStyles.boldLabel);
                string[] presets = { "Character", "Landscape", "City", "Interior" };

                int selectedIndex = Array.IndexOf(presets, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selectedPreset));
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, presets, presets.Length);
                if (selectedIndex >= 0 && selectedIndex < presets.Length)
                {
                    selectedPreset = presets[selectedIndex].ToLower();
                }
            }
        }
    }
}