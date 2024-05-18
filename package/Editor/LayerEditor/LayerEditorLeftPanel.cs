using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class LayerEditorRightPanel
    {
        private LayerEditor editor;
        private ContextMenuActions contextMenuActions;

        public LayerEditorRightPanel(LayerEditor editor)
        {
            this.editor = editor;
            this.contextMenuActions = new ContextMenuActions(editor);
        }

        public void DrawRightPanel(float rightWidth)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));

            for (int i = 0; i < editor.uploadedImages.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box"); // Use "box" style to create a bordered box for each layer

                // Visibility icon (eyeball)
                bool visibility = GUILayout.Toggle(true, EditorGUIUtility.IconContent("d_VisibilityOn"), "Button", GUILayout.Width(20), GUILayout.Height(20));

                // Display Layer Name as Label
                GUILayout.Label($"Layer_{i}", GUILayout.Width(100));

                // Up hollow caret button
                if (GUILayout.Button("△", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    // Handle move layer up action
                    MoveLayerUp(i);
                }

                // Down hollow caret button
                if (GUILayout.Button("▽", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    // Handle move layer down action
                    MoveLayerDown(i);
                }

                // Delete button with a hollow X icon
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    // Handle delete action
                    contextMenuActions.DeleteLayer(i);
                }

                EditorGUILayout.EndHorizontal();

                // Highlight selected layer
                if (editor.selectedLayerIndex == i)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                GUI.backgroundColor = Color.white;
            }

            // Add new layer and autotile layer buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Layer"))
            {
                // Handle adding a new layer
                AddNewLayer();
            }
            if (GUILayout.Button("+ Autotile Layer"))
            {
                // Handle adding a new autotile layer
                AddNewAutotileLayer();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void MoveLayerUp(int index)
        {
            if (index > 0)
            {
                var temp = editor.uploadedImages[index - 1];
                editor.uploadedImages[index - 1] = editor.uploadedImages[index];
                editor.uploadedImages[index] = temp;

                // Swap other associated properties
                SwapLayerProperties(index, index - 1);
            }
        }

        private void MoveLayerDown(int index)
        {
            if (index < editor.uploadedImages.Count - 1)
            {
                var temp = editor.uploadedImages[index + 1];
                editor.uploadedImages[index + 1] = editor.uploadedImages[index];
                editor.uploadedImages[index] = temp;

                // Swap other associated properties
                SwapLayerProperties(index, index + 1);
            }
        }

        private void SwapLayerProperties(int index1, int index2)
        {
            var tempPosition = editor.imagePositions[index1];
            editor.imagePositions[index1] = editor.imagePositions[index2];
            editor.imagePositions[index2] = tempPosition;

            var tempDragging = editor.isDraggingList[index1];
            editor.isDraggingList[index1] = editor.isDraggingList[index2];
            editor.isDraggingList[index2] = tempDragging;

            var tempSize = editor.imageSizes[index1];
            editor.imageSizes[index1] = editor.imageSizes[index2];
            editor.imageSizes[index2] = tempSize;

            var tempSprite = editor.spriteObjects[index1];
            editor.spriteObjects[index1] = editor.spriteObjects[index2];
            editor.spriteObjects[index2] = tempSprite;
        }

        private void AddNewLayer()
        {
            // Logic to add a new layer
        }

        private void AddNewAutotileLayer()
        {
            // Logic to add a new autotile layer
        }
    }
}
