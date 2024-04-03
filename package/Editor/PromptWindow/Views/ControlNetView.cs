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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enable ControlNet", EditorStyles.label);
            isControlNet = GUILayout.Toggle(isControlNet, "");
            
            promptWindow.ActiveMode.UseControlNet = isControlNet;

            if (isControlNet)
            {
                GUILayout.Label("Advanced Settings", EditorStyles.label);
                isAdvancedSettings = GUILayout.Toggle(isAdvancedSettings, "");
                promptWindow.ActiveMode.UseAdvanceSettings = isAdvancedSettings;
            }

            GUILayout.EndHorizontal();

            if (!isControlNet) return;
        
            CustomStyle.Space(20);

            if (isAdvancedSettings)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Modality :", EditorStyles.label);

                List<string> availableOptions = new List<string> { "None" };

                
                if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
                {
                    switch (DataCache.instance.SelectedModelType)
                    {
                        case "sd-xl-composition":
                            availableOptions.AddRange(dropdownOptions);
                            break;

                        case "sd-xl-lora":
                            availableOptions.AddRange(dropdownOptions);
                            break;

                        case "sd-xl":
                            availableOptions.AddRange(dropdownOptions);
                            break;

                        case "sd-1_5":
                            availableOptions.AddRange(dropdownOptions);
                            availableOptions.AddRange(dropdownOptionsSD15);
                            break;

                        default: 
                            break;

                    }
                }

                //availableOptions.AddRange(dropdownOptions);
                selectedOptionIndex = EditorGUILayout.Popup(selectedOptionIndex, availableOptions.ToArray());
                PromptPusher.Instance.modalitySelected = selectedOptionIndex;

                if (selectedOptionIndex > 0)
                { 
                    GUILayout.Label("Influence :", EditorStyles.label);
                    sliderDisplayedValue = (int)EditorGUILayout.Slider(Mathf.Clamp(sliderDisplayedValue, 0, 100), 0, 100);
                    sliderValue = sliderDisplayedValue / 100.0f;
                    if (sliderValue == 0)
                    {
                        sliderValue = 0.01f;
                    }

                    PromptPusher.Instance.modalityValue = sliderValue;
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
                    PromptPusher.Instance.selectedPreset = presets[selectedIndex].ToLower();
                }
            }
        }
    }
}