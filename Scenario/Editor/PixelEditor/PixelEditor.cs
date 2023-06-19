using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using RestSharp;

public class PixelEditor : EditorWindow
{
    private PixelEditorUI pixelEditorUI = new PixelEditorUI();

    private static float minimumWidth = 1650f;

    [MenuItem("Window/Scenario/Pixel Editor")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(PixelEditor), false, "Pixel Editor") as PixelEditor;
        if (window.pixelEditorUI != null)
        {
            window.pixelEditorUI.removeNoise = false;
            window.pixelEditorUI.removeBackground = false;
        }

        window.minSize = new Vector2(minimumWidth, window.minSize.y);
    }

    private void OnGUI()
    {
        pixelEditorUI.OnGUI(this.position);
    }

    private void OnDestroy()
    {
        PixelEditorUI.currentImage = null;
    }
}