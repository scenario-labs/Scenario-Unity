using System;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public partial class PromptWindowUI
    {
        private void RenderImageSettingsSection(bool shouldAutoGenerateSeed)
        {
            showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Image Settings");

            if (!showSettings) return;

            if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
            {
                switch (DataCache.instance.SelectedModelType)
                {
                    case "sd-xl-composition":
                        DrawWidthValues(allowedSDXLDimensionValues);
                        DrawHeightValues(allowedSDXLDimensionValues);
                        break;

                    case "sd-xl-lora":
                        DrawWidthValues(allowedSDXLDimensionValues);
                        DrawHeightValues(allowedSDXLDimensionValues);
                        break;

                    case "sd-xl":
                        DrawWidthValues(allowedSDXLDimensionValues);
                        DrawHeightValues(allowedSDXLDimensionValues);
                        break;

                    case "sd-1_5":
                        DrawWidthValues(allowed1_5DimensionValues);
                        DrawHeightValues(allowed1_5DimensionValues);
                        break;

                    default:
                        break;
                }
            }
            else
            {
                DrawWidthValues(allowed1_5DimensionValues);
                DrawHeightValues(allowed1_5DimensionValues);
            }

            CustomStyle.Space();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Scheduler: ");
                schedulerIndex = EditorGUILayout.Popup(schedulerIndex, schedulerOptions);
            }
            EditorGUILayout.EndHorizontal();

            CustomStyle.Space();
        
            float labelWidthPercentage = 0.2f;
            float sliderWidthPercentage = 0.78f;

            int labelWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * labelWidthPercentage);
            int sliderWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * sliderWidthPercentage);

            imagesliderIntValue = Mathf.RoundToInt(imagesliderValue);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Images: " + imagesliderIntValue, GUILayout.Width(labelWidth));
                imagesliderValue = GUILayout.HorizontalSlider(imagesliderValue, 1, 16, GUILayout.Width(sliderWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Sampling steps: " + samplesliderValue.ToString("00"), GUILayout.Width(labelWidth));
                samplesliderValue = (int)GUILayout.HorizontalSlider(samplesliderValue, 10, 150, GUILayout.Width(sliderWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Guidance: " + guidancesliderValue.ToString("0.0"), GUILayout.Width(labelWidth));
                guidancesliderValue =
                    Mathf.Round(GUILayout.HorizontalSlider(guidancesliderValue, 0f, 20f, GUILayout.Width(sliderWidth)) *
                                10) / 10f;
            }
            EditorGUILayout.EndHorizontal();

            if (isImageToImage || isControlNet)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("Influence: " + influenceSliderValue.ToString("F0"),"Higher values amplify the weight of the reference image, affecting the final output."), GUILayout.Width(labelWidth));
                    influenceSliderValue = (int)GUILayout.HorizontalSlider(influenceSliderValue, 0, 99, GUILayout.Width(sliderWidth));
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Seed", GUILayout.Width(labelWidth));
                if (shouldAutoGenerateSeed)
                {
                    GUI.enabled = false;
                    seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                    promptWindow.SetSeed(/*seedinputText == "-1" ? null : */seedinputText);
                }
                else
                {
                    GUI.enabled = true;
                    seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                    promptWindow.SetSeed(/*seedinputText == "-1" ? null : */seedinputText);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Draw list of buttons containing Width values for generated images.
        /// </summary>
        /// <param name="_allowedValues"> int array of dimension values </param>
        private void DrawWidthValues(int[] _allowedValues)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CustomStyle.Label("Width: ", width: 45, height: 20);
                int widthIndex = NearestValueIndex(widthSliderValue, _allowedValues);
                widthIndex = GUILayout.SelectionGrid(widthIndex, Array.ConvertAll(_allowedValues, x => x.ToString()),
                    _allowedValues.Length, CustomStyle.GetNormalButtonStyle());
                widthSliderValue = _allowedValues[widthIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw list of buttons containing Height values for generated images.
        /// </summary>
        /// <param name="_allowedValues"> int array of dimension values </param>
        private void DrawHeightValues(int[] _allowedValues)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CustomStyle.Label("Height: ", width: 45, height: 20);
                int heightIndex = NearestValueIndex(heightSliderValue, _allowedValues);
                heightIndex = GUILayout.SelectionGrid(heightIndex, Array.ConvertAll(_allowedValues, x => x.ToString()),
                    _allowedValues.Length, CustomStyle.GetNormalButtonStyle());
                heightSliderValue = _allowedValues[heightIndex];
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}