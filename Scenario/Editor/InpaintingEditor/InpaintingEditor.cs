using UnityEditor;
using UnityEngine;

public class InpaintingEditor : EditorWindow
{
    private static InpaintingEditorUI inpaintingEditorUI;
    private static Texture2D inpaintingTexture;

    private static float minimumWidth = 1775f;

    [MenuItem("Window/Scenario/Inpainting Editor")]
    public static void ShowInpaintingWindow()
    {
        EditorWindow.GetWindow(typeof(InpaintingEditor), false, "Inpainting Editor");
        InpaintingEditor window = GetWindow<InpaintingEditor>("Inpainting Editor");
        window.minSize = new Vector2(minimumWidth, window.minSize.y);
    }

    public static void ShowInpaintingWindow(Texture2D texture2D)
    {
        EditorWindow.GetWindow(typeof(InpaintingEditor), false, "Inpainting Editor");
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