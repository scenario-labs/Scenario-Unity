using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CustomStyle
{
    public static GUIStyle GetNormalButtonStyle()
    {
        return new GUIStyle(GUI.skin.button) { };
    }
    
    public static GUIStyle GetTertiaryButtonStyle()
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            normal =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.4f, 0.9f, 0.86f)),
                textColor = Color.white
            },
            active =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.4f, 0.9f, 0.86f)),
                textColor = Color.white
            }
        };

        return style;
    }
    
    public static Color GetBackgroundColor()
    {
        return new Color(0.24f, 0.24f, 0.24f);
    }
    
    //new Color(0.23f, 0.89f, 0.45f)

    public static bool Foldout(bool value, string text)
    {
        // Create a GUIStyle to customize the foldout appearance.
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold, // Make the text bold.
            fontSize = 14, // Set the font size.
            normal =
            {
                textColor = Color.cyan // Change the text color.
            },
            padding = new RectOffset(15, 0, 0, 0) // Indent the text.
        };
        
        return EditorGUILayout.Foldout(value, text, foldoutStyle);
    }
    
    public static void Separator()
    {
        EditorGUILayout.Separator();
    }
    
    public static void Space(float space = 10)
    {
        EditorGUILayout.Space(space);
    }
    
    public static void Label(string text, int fontSize = 12,
        TextAnchor alignment = TextAnchor.MiddleLeft,
        float width = 0,
        float height = 0,
        bool bold = false,
        params GUILayoutOption[] layoutOptions)
    {
        var style = new GUIStyle((bold)?EditorStyles.boldLabel:EditorStyles.label)
        {
            normal =
            {
                textColor = Color.white
            },
            fontSize = fontSize,
            alignment = alignment,
            fixedWidth = width,
            fixedHeight = height,
        };
        
        GUILayout.Label(text, style, layoutOptions);
    }

    public static void ButtonPrimary(string text, float height, Action action)
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            normal =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.44f, 0.35f, 0.89f)),
                textColor = Color.white
            },
            active =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.33f, 0.34f, 1f)),
                textColor = Color.white
            },
            hover = 
            {
                background = CommonUtils.CreateColorTexture(new Color(0.47f, 0.5f, 1f)),
                textColor = Color.white
            }
        };

        if (GUILayout.Button(text, style, GUILayout.Height(height)))
        {
           action?.Invoke();
        }
    }
    
    public static void ButtonSecondary(string text, float height, Action action)
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            border = new RectOffset(),
            normal =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.09f, 0.75f, 0.92f)),
                textColor = Color.white
            },
            active =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.22f, 1f, 0.99f)),
                textColor = Color.white
            },
            hover =
            {
                background = CommonUtils.CreateColorTexture(new Color(0.41f, 1f, 0.99f)),
                textColor = Color.white
            }
        };

        if (GUILayout.Button(text, style, GUILayout.Height(height)))
        {
            action?.Invoke();
        }
    }
    
    public static void ButtonTertiary(string text, float height, Action action)
    {
        var style = GetTertiaryButtonStyle();

        if (GUILayout.Button(text, style, GUILayout.Height(height)))
        {
            action?.Invoke();
        }
    }
}