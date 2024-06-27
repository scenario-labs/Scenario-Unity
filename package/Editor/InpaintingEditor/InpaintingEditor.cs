using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class InpaintingEditor : EditorWindow
    {
        private static readonly float MinimumWidth = 1775f;
        private static InpaintingEditorUI inpaintingEditorUI;
        private static Texture2D inpaintingTexture;

        [MenuItem("Scenario/Editors/Inpainting Editor", false, 2)]
        public static void ShowWindow()
        {
            InpaintingEditor window = GetWindow<InpaintingEditor>("Inpainting Editor");
            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
        }

        public static void ShowWindow(Texture2D texture2D)
        {
            InpaintingEditor window = GetWindow<InpaintingEditor>("Inpainting Editor");
            inpaintingTexture = texture2D;
            if (inpaintingTexture != null)
            {
                inpaintingEditorUI.SetImage(inpaintingTexture);
            }
        }

        private void OnEnable()
        {
            inpaintingEditorUI = new InpaintingEditorUI(this);
        }

        private void OnGUI()
        {
            inpaintingEditorUI.DrawUI(this.position);
        }
    }
}