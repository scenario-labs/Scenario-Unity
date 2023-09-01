using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class PromptWindowUI
{
     private void RenderImageSettingsSection(bool shouldAutoGenerateSeed)
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "Image Settings");
        
        if (!showSettings) return;
        
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Width: ", EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
            int widthIndex = NearestValueIndex(widthSliderValue, allowedWidthValues);
            widthIndex = GUILayout.SelectionGrid(widthIndex, Array.ConvertAll(allowedWidthValues, x => x.ToString()),
                allowedWidthValues.Length);
            widthSliderValue = allowedWidthValues[widthIndex];
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Height: ", EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
            int heightIndex = NearestValueIndex(heightSliderValue, allowedHeightValues);
            heightIndex = GUILayout.SelectionGrid(heightIndex, Array.ConvertAll(allowedHeightValues, x => x.ToString()),
                allowedHeightValues.Length);
            heightSliderValue = allowedHeightValues[heightIndex];
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20f);

        float labelWidthPercentage = 0.2f;
        float sliderWidthPercentage = 0.78f;

        int labelWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * labelWidthPercentage);
        int sliderWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * sliderWidthPercentage);

        int imagesliderIntValue = Mathf.RoundToInt(imagesliderValue);
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label("Images: " + imagesliderIntValue, GUILayout.Width(labelWidth));
            imagesliderValue = GUILayout.HorizontalSlider(imagesliderValue, 1, 16, GUILayout.Width(sliderWidth));
        }
        EditorGUILayout.EndHorizontal();

        int samplesliderIntValue = Mathf.RoundToInt(samplesliderValue);
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label("Sampling steps: " + samplesliderIntValue, GUILayout.Width(labelWidth));
            samplesliderValue = GUILayout.HorizontalSlider(samplesliderValue, 10, 150, GUILayout.Width(sliderWidth));
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
                GUILayout.Label("Influence: " + influncesliderValue.ToString("0.00"), GUILayout.Width(labelWidth));
                influncesliderValue =
                    GUILayout.HorizontalSlider(influncesliderValue, 0f, 1f, GUILayout.Width(sliderWidth));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label("Seed", GUILayout.Width(labelWidth));
            if (shouldAutoGenerateSeed)
            {
                GUI.enabled = false;
                GUILayout.TextArea("-1", GUILayout.Height(20), GUILayout.Width(sliderWidth));
            }
            else
            {
                GUI.enabled = true;
                seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                promptWindow.SetSeed(seedinputText == "-1" ? null : seedinputText);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}