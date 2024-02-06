using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class ImageEditor : EditorWindow
    {
        private static ImageEditorUI imageEditorUI;
        private static Texture2D imageTexture;

        private static float minimumWidth = 1775f;

        [MenuItem("Window/Scenario/Image Editor")]
        public static void ShowWindow()
        {
            ImageEditor window = GetWindow<ImageEditor>("Image Editor");
            window.minSize = new Vector2(GetScreenWidth() * 0.8f, window.minSize.y);
        }

        public static void ShowWindow(Texture2D texture2D)
        {
            EditorWindow.GetWindow(typeof(ImageEditor), false, "Image Editor");
            imageTexture = texture2D;
            if (imageTexture != null)
            {
                imageEditorUI.SetImage(imageTexture);
            }
        }

        private void OnEnable()
        {
            imageEditorUI = new ImageEditorUI(this);
        }

        private void OnGUI()
        {
            imageEditorUI.DrawUI(this.position);
        }

        private static int GetScreenWidth()
        {
#if UNITY_EDITOR_OSX
            return Screen.resolutions[Screen.resolutions.Length - 1].width / 2;
#else
            return Screen.currentResolution.width;
#endif
        }
    }
}