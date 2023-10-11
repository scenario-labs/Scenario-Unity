using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class LayerEditorRightPanel
    {
        private LayerEditor editor;
    
        public LayerEditorRightPanel(LayerEditor editor)
        {
            this.editor = editor;
        }

        public void DrawRightPanel(float rightWidth)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));
        
            for (int i = 0; i < editor.uploadedImages.Count; i++)
            {
                Texture2D uploadedImage = editor.uploadedImages[i];
                EditorGUILayout.BeginHorizontal();
                {
                    // Display Thumbnail Image
                    GUILayout.Label(uploadedImage, GUILayout.Width(50), GUILayout.Height(50));

                    EditorGUILayout.BeginVertical();
                    {
                        // Display Image Name as Label
                        GUILayout.Label($"Layer {i + 1}");

                        // Add any additional information or options related to the image layer here
                        // For example:
                        // GUILayout.Label($"Width: {uploadedImage.width}");
                        // GUILayout.Label($"Height: {uploadedImage.height}");
                        // ... additional properties ...
                    }
                    EditorGUILayout.EndVertical();

                    if (editor.selectedLayerIndex == i)
                    {
                        GUI.backgroundColor = Color.yellow;
                    }

                    if (GUILayout.Button($"Select", GUILayout.MinWidth(80)))
                    {
                        editor.selectedLayerIndex = i;
                    }

                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }
        
            EditorGUILayout.EndVertical();
        }
    }
}
