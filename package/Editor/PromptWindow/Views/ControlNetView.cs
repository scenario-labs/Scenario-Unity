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

            promptWindow.ActiveMode.UseControlNet = true;

            if (promptWindow.ActiveMode.UseControlNet)
            {
                GUILayout.Label("Advanced Settings", EditorStyles.label);

                bool isFluxModel = false;

                if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
                {
                    switch (DataCache.instance.SelectedModelType)
                    {
                        case string modelType when modelType.StartsWith("flux", StringComparison.OrdinalIgnoreCase):
                            isAdvancedSettings = true;
                            isFluxModel = true;
                            break;
                    }
                }

                if (isFluxModel)
                {
                    GUI.enabled = false;
                    GUILayout.Toggle(isAdvancedSettings, "");
                    GUI.enabled = true;
                }
                else
                {
                    isAdvancedSettings = GUILayout.Toggle(isAdvancedSettings, "");
                }

                promptWindow.ActiveMode.UseAdvanceSettings = isAdvancedSettings;
            }

            GUILayout.EndHorizontal();

            if (!promptWindow.ActiveMode.UseControlNet) return;

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
                        case string modelType when modelType.StartsWith("sd-xl", StringComparison.OrdinalIgnoreCase):
                            availableOptions.AddRange(dropdownOptions);
                            break;
                        case string modelType when modelType.StartsWith("flux", StringComparison.OrdinalIgnoreCase):
                            availableOptions.AddRange(dropdownOptionsflux);
                            break;
                        case "sd-1_5":
                            availableOptions.AddRange(dropdownOptions);
                            availableOptions.AddRange(dropdownOptionsSD15);
                            break;
                    }
                }

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
                string[] presetsToShow = new string[0];

                if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
                {
                    switch (DataCache.instance.SelectedModelType)
                    {
                        case string modelType when modelType.StartsWith("sd-xl", StringComparison.OrdinalIgnoreCase):
                            presetsToShow = new string[] { "Character", "Landscape" };
                            break;
                        case string modelType when modelType.StartsWith("flux", StringComparison.OrdinalIgnoreCase):
                            presetsToShow = new string[0];
                            break;
                        case "sd-1_5":
                            presetsToShow = new string[] { "Character", "Landscape", "City", "Interior" };
                            break;
                        default:
                            presetsToShow = new string[] { "Character", "Landscape", "City", "Interior" };
                            break;
                    }
                }
                else
                {
                    presetsToShow = new string[] { "Character", "Landscape", "City", "Interior" };
                }

                int selectedIndex = -1;
                if (!string.IsNullOrEmpty(selectedPreset))
                {
                    selectedIndex = Array.IndexOf(presetsToShow, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selectedPreset));
                }
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, presetsToShow, presetsToShow.Length);

                if (selectedIndex >= 0 && selectedIndex < presetsToShow.Length)
                {
                    selectedPreset = presetsToShow[selectedIndex].ToLower().Replace(" ", "");
                    PromptPusher.Instance.selectedPreset = selectedPreset;
                }
            }
        }
    }
}