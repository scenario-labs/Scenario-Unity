using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateGenerator : EditorWindow
{
    [MenuItem("Window/Scenario/Create Generator")]
    public static void ShowWindow()
    {
        GetWindow<CreateGenerator>("Create Generator");
    }

    private string generatorName = "";
    private int uploadedImages = 0;

    private List<Texture2D> uploadedImageTextures = new List<Texture2D>();

    private void OnGUI()
    {
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        Event currentEvent = Event.current;
        Rect dropArea = new Rect(20, 90, 200, 50);

        EditorGUILayout.LabelField("1 / 3 - Add Training Images", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Generator Name", EditorStyles.boldLabel);
        generatorName = EditorGUILayout.TextField("", generatorName, GUILayout.ExpandWidth(true));
        EditorGUILayout.HelpBox("The name will be used to identify your generator", MessageType.Info);

        if (dropArea.Contains(currentEvent.mousePosition))
        {
            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }
                    break;

                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        foreach (Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is Texture2D texture)
                            {
                                uploadedImages++;
                                uploadedImageTextures.Insert(0, texture);
                            }
                        }
                    }
                    break;
            }
        }

        EditorGUILayout.LabelField("Upload Progress", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(uploadedImages + " / 100");

        Rect progressBarRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressBarRect, uploadedImages / 100f, uploadedImages + " / 100");
        EditorGUILayout.HelpBox("Upload 5 to 100 images - learn more about curating a good training dataset here", MessageType.Info);

        // Display the uploaded images
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < uploadedImageTextures.Count; i++)
        {
            uploadedImageTextures[i] = (Texture2D)EditorGUILayout.ObjectField(uploadedImageTextures[i], typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));
        }

        // Display the uploaded images
        const int imagesPerRow = 7;
        for (int i = 0; i < uploadedImageTextures.Count; i += imagesPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = i; j < i + imagesPerRow && j < uploadedImageTextures.Count; j++)
            {
                uploadedImageTextures[j] = (Texture2D)EditorGUILayout.ObjectField(uploadedImageTextures[j], typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // Add Images button with the same size as uploaded images
        if (GUILayout.Button("+ Add Images\n(Select or Drag & Drop)", GUILayout.Width(64), GUILayout.Height(64)))
        {
            string[] filePaths = EditorUtility.OpenFilePanel("Select Images", "", "jpg,jpeg,png,gif").Split('\n');
            foreach (string absoluteFilePath in filePaths)
            {
                if (!string.IsNullOrEmpty(absoluteFilePath))
                {
                    string relativeFilePath = absoluteFilePath.Replace(Application.dataPath, "Assets");
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativeFilePath);
                    if (texture != null)
                    {
                        uploadedImages++;
                        uploadedImageTextures.Insert(0, texture);
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = uploadedImages >= 5;
        if (GUILayout.Button("Next"))
        {
        }
        GUI.enabled = true;
    }
}
