using UnityEditor;
using UnityEngine;
using System;

public class CompositionEditor : EditorWindow
{
    private static readonly int[] AllowedWidthValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };
    private static readonly int[] AllowedHeightValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };
    private static readonly float MinimumWidth = 800f;
    private static readonly float MinimumHeight = 750f;

    private int widthSliderValue = 512;
    private int heightSliderValue = 512;
    private float distanceOffset = 1f;
    private GameObject compositionCamera = null;
    internal RenderTexture renderTexture;
    private Texture2D screenshot;

    [MenuItem("Window/Scenario/Composition Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<CompositionEditor>("Composition Editor");
        window.minSize = new Vector2(MinimumWidth, MinimumHeight);
    }

    private void OnGUI()
    {
        DrawBackground();
        DrawScreenshotIfAvailable();
        DrawControlButtons();
        DrawControls();
        DrawUseImageButton();
    }

    private void DrawBackground()
    {
        Color backgroundColor = CustomStyle.GetBackgroundColor();
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
    }

    private void DrawScreenshotIfAvailable()
    {
        if (screenshot == null) return;
        
        Rect textureRect = GUILayoutUtility.GetRect(screenshot.width, screenshot.height, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.DrawTexture(textureRect, screenshot, ScaleMode.ScaleToFit);
    }

    private void DrawControlButtons()
    {
        EditorGUILayout.BeginHorizontal();
        {
            CustomStyle.ButtonSecondary("Place Camera", 25, PlaceCamera);
            CustomStyle.ButtonSecondary("Remove Camera", 25, RemoveCamera);
            CustomStyle.ButtonSecondary("Take Screenshot", 25, CaptureScreenshot);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawControls()
    {
        CustomStyle.Space();
        distanceOffset = EditorGUILayout.Slider("Distance Offset: ", distanceOffset, 0.1f, 100.0f);
        widthSliderValue = DrawDimensionControl("Width: ", widthSliderValue, AllowedWidthValues);
        heightSliderValue = DrawDimensionControl("Height: ", heightSliderValue, AllowedHeightValues);
        CustomStyle.Space(20f);
    }

    private int DrawDimensionControl(string label, int currentValue, int[] allowedValues)
    {
        int index = 0;
        
        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
            index = NearestValueIndex(currentValue, allowedValues);
            index = GUILayout.SelectionGrid(index, Array.ConvertAll(allowedValues, x => x.ToString()),
                allowedValues.Length);
        }
        EditorGUILayout.EndVertical();
        
        return allowedValues[index];
    }

    private void DrawUseImageButton()
    {
        EditorGUILayout.BeginVertical();
        {
            GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal =
                {
                    background = CreateColorTexture(new Color(0, 0.5333f, 0.8f, 1)),
                    textColor = Color.white
                },
                active =
                {
                    background = CreateColorTexture(new Color(0, 0.3f, 0.6f, 1)),
                    textColor = Color.white
                }
            };

            if (GUILayout.Button("Use Image", generateButtonStyle, GUILayout.Height(40)))
            {
                if (screenshot == null)
                {
                    Debug.LogError("Screenshot must be taken before using the image");
                }
                else
                {
                    PromptWindowUI.imageUpload = screenshot;
                }
            }
        }
        EditorGUILayout.EndVertical();
        CustomStyle.Space(10f);
    }

    private void PlaceCamera()
    {
        compositionCamera = new GameObject("CompositionCamera");
        Camera camera = compositionCamera.AddComponent<Camera>();
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null) return;
        
        Vector3 cameraPosition = sceneView.pivot + sceneView.rotation * Vector3.forward * distanceOffset;
        compositionCamera.transform.position = cameraPosition;
        compositionCamera.transform.rotation = sceneView.rotation;
    }

    private void RemoveCamera()
    {
        if (compositionCamera == null) return;
        
        DestroyImmediate(compositionCamera);
        compositionCamera = null;
    }

    private int NearestValueIndex(int currentValue, int[] allowedValues)
    {
        int nearestIndex = 0;
        int minDifference = int.MaxValue;

        for (int i = 0; i < allowedValues.Length; i++)
        {
            int difference = Mathf.Abs(currentValue - allowedValues[i]);
            if (difference < minDifference)
            {
                minDifference = difference;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void CaptureScreenshot()
    {
        if (compositionCamera == null) return;

        Camera camera = compositionCamera.GetComponent<Camera>();
        RenderTexture rt = new RenderTexture(widthSliderValue, heightSliderValue, 24);
        camera.targetTexture = rt;
        camera.Render();

        Texture2D tempTexture = new Texture2D(widthSliderValue, heightSliderValue, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tempTexture.ReadPixels(new Rect(0, 0, widthSliderValue, heightSliderValue), 0, 0);
        tempTexture.Apply();

        screenshot = new Texture2D(widthSliderValue, heightSliderValue, TextureFormat.RGB24, false);
        screenshot.SetPixels(tempTexture.GetPixels());
        screenshot.Apply();

        camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(tempTexture);

        Repaint();
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}