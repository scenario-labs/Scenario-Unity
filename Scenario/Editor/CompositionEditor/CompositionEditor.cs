using UnityEditor;
using UnityEngine;
using System;

public class CompositionEditor : EditorWindow
{
    private static readonly int[] ALLOWED_WIDTH_VALUES = { 512, 576, 640, 688, 704, 768, 912, 1024 };
    private static readonly int[] ALLOWED_HEIGHT_VALUES = { 512, 576, 640, 688, 704, 768, 912, 1024 };
    private static readonly float MINIMUM_WIDTH = 800f;
    private static readonly float MINIMUM_HEIGHT = 750f;

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
        window.minSize = new Vector2(MINIMUM_WIDTH, MINIMUM_HEIGHT);
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
        Color backgroundColor = new Color32(26, 26, 26, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
    }

    private void DrawScreenshotIfAvailable()
    {
        if (screenshot != null)
        {
            Rect textureRect = GUILayoutUtility.GetRect(screenshot.width, screenshot.height, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.DrawTexture(textureRect, screenshot, ScaleMode.ScaleToFit);
        }
    }

    private void DrawControlButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Place Camera"))
        {
            PlaceCamera();
        }

        if (GUILayout.Button("Remove Camera"))
        {
            RemoveCamera();
        }

        if (GUILayout.Button("Screenshot"))
        {
            CaptureScreenshot();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawControls()
    {
        GUILayout.Space(20f);
        distanceOffset = EditorGUILayout.Slider("Distance Offset: ", distanceOffset, 0.1f, 100.0f);
        widthSliderValue = DrawDimensionControl("Width: ", widthSliderValue, ALLOWED_WIDTH_VALUES);
        heightSliderValue = DrawDimensionControl("Height: ", heightSliderValue, ALLOWED_HEIGHT_VALUES);
        GUILayout.Space(20f);
    }

    private int DrawDimensionControl(string label, int currentValue, int[] allowedValues)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
        int index = NearestValueIndex(currentValue, allowedValues);
        index = GUILayout.SelectionGrid(index, Array.ConvertAll(allowedValues, x => x.ToString()), allowedValues.Length);
        EditorGUILayout.EndVertical();
        return allowedValues[index];
    }

    private void DrawUseImageButton()
    {
        EditorGUILayout.BeginVertical();
        GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button);
        generateButtonStyle.normal.background = CreateColorTexture(new Color(0, 0.5333f, 0.8f, 1));
        generateButtonStyle.normal.textColor = Color.white;
        generateButtonStyle.active.background = CreateColorTexture(new Color(0, 0.3f, 0.6f, 1));
        generateButtonStyle.active.textColor = Color.white;

        if (GUILayout.Button("Use Image", generateButtonStyle, GUILayout.Height(40)))
        {
            if (screenshot == null)
            {
                Debug.LogError("Screenshot must be taken before using the image");
                return;
            }
            else
            {
                PromptWindowUI.imageUpload = screenshot;
            }
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10f);
    }

    private void PlaceCamera()
    {
        compositionCamera = new GameObject("CompositionCamera");
        Camera camera = compositionCamera.AddComponent<Camera>();
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView != null)
        {
            Vector3 cameraPosition = sceneView.pivot + sceneView.rotation * Vector3.forward * distanceOffset;
            compositionCamera.transform.position = cameraPosition;
            compositionCamera.transform.rotation = sceneView.rotation;
        }
    }

    private void RemoveCamera()
    {
        if (compositionCamera != null)
        {
            DestroyImmediate(compositionCamera);
            compositionCamera = null;
        }
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