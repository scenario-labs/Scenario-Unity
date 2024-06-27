using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class PixelEditor : EditorWindow
    {
        private static readonly float MinimumWidth = 1650f;
        private PixelEditorUI pixelEditorUI = new();

        [MenuItem("Scenario/Editors/Pixel Editor", false, 4)]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(PixelEditor), false, "Pixel Editor") as PixelEditor;
            if (window.pixelEditorUI != null)
            {
                window.pixelEditorUI.removeNoise = false;
                window.pixelEditorUI.removeBackground = false;
            }

            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
        }

        public static void ShowWindow(Texture2D selectedTexture, ImageDataStorage.ImageData imageData)
        {
            PixelEditorUI.currentImage = selectedTexture;
            PixelEditorUI.imageData = imageData;
            ShowWindow();
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
}