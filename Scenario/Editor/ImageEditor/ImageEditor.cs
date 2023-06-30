using UnityEditor;
using UnityEngine;

public class ImageEditor : EditorWindow
{
    private static ImageEditorUI imageEditorUI;
    private static Texture2D imageTexture;

    private static float minimumWidth = 1775f;

    [MenuItem("Window/Scenario/Image Editor")]
    public static void ShowWindow()
    {
        ImageEditor window = GetWindow<ImageEditor>("Image Editor");
        window.minSize = new Vector2(minimumWidth, window.minSize.y);
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
}