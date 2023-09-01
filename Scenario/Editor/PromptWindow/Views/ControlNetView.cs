using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

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
        
        GUILayout.Space(20);

        if (isAdvancedSettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Model 1", EditorStyles.label);

            List<string> availableOptions1 = new List<string> { "None" };
            availableOptions1.AddRange(dropdownOptions);
            selectedOption1Index = EditorGUILayout.Popup(selectedOption1Index, availableOptions1.ToArray());

            GUILayout.Label("Slider 1", EditorStyles.label);
            sliderValue1 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue1, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
            GUILayout.EndHorizontal();

            if (selectedOption1Index > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Model 2", EditorStyles.label);

                List<string> availableOptions2 = new List<string> { "None" };
                availableOptions2.AddRange(dropdownOptions);
                availableOptions2.RemoveAt(selectedOption1Index);
                selectedOption2Index = EditorGUILayout.Popup(selectedOption2Index, availableOptions2.ToArray());

                GUILayout.Label("Slider 2", EditorStyles.label);
                sliderValue2 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue2, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
                GUILayout.EndHorizontal();
            }

            if (selectedOption2Index > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Model 3", EditorStyles.label);

                List<string> availableOptions3 = new List<string> { "None" };
                availableOptions3.AddRange(dropdownOptions);
                int option1Index = Array.IndexOf(dropdownOptions, availableOptions1[selectedOption1Index]);
                int option2Index = Array.IndexOf(dropdownOptions, dropdownOptions[selectedOption2Index]);

                availableOptions3.RemoveAt(option1Index + 1);
                availableOptions3.RemoveAt(option2Index);

                selectedOption3Index = EditorGUILayout.Popup(selectedOption3Index, availableOptions3.ToArray());

                GUILayout.Label("Slider 3", EditorStyles.label);
                sliderValue3 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue3, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
                GUILayout.EndHorizontal();
            }
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