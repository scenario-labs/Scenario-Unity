using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using RestSharp;

public class UpscaleEditor : EditorWindow
{
    private UpscaleEditorUI upscaleEditorUI = new UpscaleEditorUI();

    private static float minimumWidth = 1650f;

    [MenuItem("Window/Scenario/Upscale Editor")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(UpscaleEditor), false, "Upscale Editor") as UpscaleEditor;
        if (window.upscaleEditorUI != null)
        {
            /*window.upscaleEditorUI.removeNoise = false;
            window.upscaleEditorUI.removeBackground = false;*/
        }

        window.minSize = new Vector2(minimumWidth, window.minSize.y);
    }

    private void OnGUI()
    {
        upscaleEditorUI.OnGUI(this.position);
    }

    private void OnDestroy()
    {
        UpscaleEditorUI.currentImage = null;
    }
}