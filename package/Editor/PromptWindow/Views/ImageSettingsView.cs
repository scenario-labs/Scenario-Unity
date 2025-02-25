using System;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public partial class PromptWindowUI
    {
        /// <summary>
        /// Fix default value to the label width
        /// </summary>
        private int labelWidth = 0;

        /// <summary>
        /// Fix default value to the slider width
        /// </summary>
        private int sliderWidth = 0;

        private void RenderImageSettingsSection(bool shouldAutoGenerateSeed)
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            {
                float labelWidthPercentage = 0.35f;
                float sliderWidthPercentage = 0.85f;

                labelWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * labelWidthPercentage);
                sliderWidth = Mathf.RoundToInt((EditorGUIUtility.currentViewWidth - labelWidth) * sliderWidthPercentage);

                showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Image Settings");
                if (!showSettings)
                {
                    EditorGUILayout.EndVertical();
                    return;
                }

                float maxImages = 16f;
                float maxSteps = 150f;
                float maxGuidance = 20f;

                if (DataCache.instance.SelectedModelType == "flux.1.1-pro-ultra")
                {
                    maxImages = 1f;
                    maxSteps = 0f;
                    maxGuidance = 0f;
                }
                else if (DataCache.instance.SelectedModelType == "flux.1.1-pro")
                {
                    maxImages = 1f;
                    maxSteps = 0f;
                    maxGuidance = 0f;
                }
                else if (DataCache.instance.SelectedModelType == "flux.1-pro")
                {
                    maxImages = 1f;
                    maxSteps = 50f;
                }
                else if (DataCache.instance.SelectedModelType.StartsWith("flux."))
                {
                    maxImages = 8f;
                    maxSteps = 50f;
                }
                else if (DataCache.instance.SelectedModelId == "flux.1-schnell")
                {
                    maxImages = 4f;
                    maxSteps = 10f;
                }

                imagesliderValue = Mathf.Max(1, Mathf.Min(imagesliderValue, (int)maxImages));
                samplesliderValue = Mathf.Max(1, Mathf.Min(samplesliderValue, (int)maxSteps));
                guidancesliderValue = Mathf.Max(0, Mathf.Min(guidancesliderValue, (int)maxGuidance));

                if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
                {
                    switch (DataCache.instance.SelectedModelType)
                    {
                        case string modelType when modelType.StartsWith("sd-xl"):
                            DrawSliderSizeValue(allowedSDXLDimensionValues);
                            break;

                        case "sd-1_5":
                            DrawSliderSizeValue(allowed1_5DimensionValues);
                            break;

                        case string modelType when modelType.StartsWith("flux."):
                            DrawSliderSizeValue(allowedSDXLDimensionValues);
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    DrawSliderSizeValue(allowed1_5DimensionValues);
                }

                CustomStyle.Space();

                if (!DataCache.instance.SelectedModelType.StartsWith("flux."))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Scheduler: ", GUILayout.Width(labelWidth));
                    promptPusher.schedulerSelected = EditorGUILayout.Popup(promptPusher.schedulerSelected, promptPusher.SchedulerOptions, GUILayout.Width(sliderWidth));
                    EditorGUILayout.EndHorizontal();
                }

                imagesliderIntValue = Mathf.RoundToInt(imagesliderValue);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Images: " + imagesliderIntValue, GUILayout.Width(labelWidth));
                imagesliderValue = GUILayout.HorizontalSlider(imagesliderValue, 1, maxImages, GUILayout.Width(sliderWidth));
                promptPusher.numberOfImages = Mathf.RoundToInt(imagesliderValue);
                EditorGUILayout.EndHorizontal();

                if (maxSteps > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Sampling steps: " + samplesliderValue.ToString("00"), GUILayout.Width(labelWidth));
                    samplesliderValue = (int)GUILayout.HorizontalSlider(samplesliderValue, 1, maxSteps, GUILayout.Width(sliderWidth));
                    promptPusher.samplesStep = samplesliderValue;
                    EditorGUILayout.EndHorizontal();
                }

                if (maxGuidance > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Guidance: " + guidancesliderValue.ToString("0.0"), GUILayout.Width(labelWidth));
                    guidancesliderValue = Mathf.Round(GUILayout.HorizontalSlider(guidancesliderValue, 0f, maxGuidance, GUILayout.Width(sliderWidth)) * 10) / 10f;
                    promptPusher.guidance = guidancesliderValue;
                    EditorGUILayout.EndHorizontal();
                }

                if (promptWindow.ActiveMode != null)
                {
                    if (promptWindow.ActiveMode.EMode == ECreationMode.Image_To_Image || promptWindow.ActiveMode.IsControlNet)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Influence: " + influenceSliderValue.ToString("F0"), "Higher values amplify the weight of the reference image, affecting the final output."), GUILayout.Width(labelWidth));
                        influenceSliderValue = (int)GUILayout.HorizontalSlider(influenceSliderValue, 0, 99, GUILayout.Width(sliderWidth));
                        promptPusher.influenceOption = influenceSliderValue;
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Seed", GUILayout.Width(labelWidth));
                if (shouldAutoGenerateSeed)
                {
                    GUI.enabled = false;
                    seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                    promptWindow.SetSeed(seedinputText);
                }
                else
                {
                    GUI.enabled = true;
                    seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                    promptWindow.SetSeed(seedinputText);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Draw a slider to select the width and height of the image, still according to the Scenario's web app
        /// </summary>
        /// <param name="_allowedValues"> Reference Array </param>
        private void DrawSliderSizeValue(int[] _allowedValues)
        {
            CustomStyle.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Size: " + promptPusher.width + " x " + promptPusher.height, GUILayout.Width(labelWidth));
                sizeSliderValue = GUILayout.HorizontalSlider(sizeSliderValue, 1, (_allowedValues.Length * 2) - 1, GUILayout.Width(sliderWidth));
                sizeSliderValue = Mathf.Round(sizeSliderValue);
                int correspondingValue = 0;

                if (sizeSliderValue >= _allowedValues.Length)
                {
                    correspondingValue = (int)sizeSliderValue - _allowedValues.Length;
                    heightSliderValue = _allowedValues[correspondingValue];
                    widthSliderValue = _allowedValues[0];
                    promptPusher.height = heightSliderValue;
                    promptPusher.width = widthSliderValue;
                }
                else if (sizeSliderValue < _allowedValues.Length)
                {
                    correspondingValue = _allowedValues.Length - (int)sizeSliderValue;
                    widthSliderValue = _allowedValues[correspondingValue];
                    heightSliderValue = _allowedValues[0];
                    promptPusher.width = widthSliderValue;
                    promptPusher.height = heightSliderValue;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
