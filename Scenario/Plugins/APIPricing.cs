using UnityEngine;
using UnityEditor;

public class APIPricingWindow : EditorWindow
{
    private string[] headers = { "Steps", "512x512", "512x576", "512x640", "512x688", "512x704", "512x768", "512x912", "512x1024" };
    private string[,] data = { 
        { "30", "$0.010", "$0.011", "$0.013", "$0.013", "$0.014", "$0.015", "$0.018", "$0.020" },
        { "50", "$0.017", "$0.019", "$0.021", "$0.022", "$0.023", "$0.025", "$0.030", "$0.033" },
        { "100", "$0.033", "$0.038", "$0.042", "$0.045", "$0.046", "$0.050", "$0.059", "$0.067" },
        { "150", "$0.050", "$0.056", "$0.063", "$0.067", "$0.069", "$0.075", "$0.089", "$0.100" }
    };
    private string[,] additionalCosts = {
        { "Background removal", "$0.100 per image" },
        { "Upscaling", "$0.050 per image" },
        { "Magic Upscaling", "$0.050 per image" },
        { "Pixelate", "$0.025 per image" }
    };
    private string[,] additionalDetails = {
        { "Composition Control Multiplier", "1.2" },
        { "Generator training", "$5.00 per training" }
    };

    [MenuItem("Window/Scenario/API Pricing")]
    public static void ShowWindow()
    {
        GetWindow<APIPricingWindow>("API Pricing");
    }

    void OnGUI()
    {
        DrawInfo();
        DrawTable();
        DrawAdditionalCosts();
        DrawAdditionalDetails();
    }

    void DrawInfo()
    {
        GUILayout.Label("The Scenario API", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The Scenario API is a versatile tool for image generation and training generators. It's highly adaptable, making it easy to incorporate into games or third-party software. The flexible pricing structure operates on a pay-per-use system, with a monthly billing cycle and cost management options.", MessageType.Info);
        GUILayout.Label("Pricing Structure", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The API pricing is designed for flexibility and affordability, and begins at a simple rate of 1 cent per image. Costs vary based on image resolution or the use of advanced features. Please refer to the tables below for detailed pricing per image, based on different features and configurations:", MessageType.Info);
    }

    void DrawTable()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();
        foreach (string header in headers)
        {
            GUILayout.Label(header, EditorStyles.boldLabel);
        }
        EditorGUILayout.EndHorizontal();
        for (int i = 0; i < data.GetLength(0); i++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < data.GetLength(1); j++)
            {
                GUILayout.Label(data[i, j], GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    void DrawAdditionalCosts()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Additional costs (cost per image)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        for (int i = 0; i < additionalCosts.GetLength(0); i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(additionalCosts[i, 0], GUILayout.Width(150));
            GUILayout.Label(additionalCosts[i, 1]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    void DrawAdditionalDetails()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Additional Details", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        for (int i = 0; i < additionalDetails.GetLength(0); i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(additionalDetails[i, 0], GUILayout.Width(200));
            GUILayout.Label(additionalDetails[i, 1]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
}