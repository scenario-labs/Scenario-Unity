using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MyEditorWindow : EditorWindow
{
    [SerializeField] private float rightWidthRatio = 0.1f;
    [SerializeField] private float leftWidthRatio = 0.9f;

    private List<Texture2D> uploadedImages = new List<Texture2D>();
    private List<Vector2> imagePositions = new List<Vector2>();
    private List<bool> isDraggingList = new List<bool>();
    private List<Vector2> imageSizes = new List<Vector2>();

    private GUIStyle imageStyle;

    private int selectedLayerIndex = -1;

    private bool showPixelAlignment = true;
    private bool showHorizontalAlignment = true;

    [MenuItem("Window/My Editor Window")]
    public static void ShowWindow()
    {
        GetWindow<MyEditorWindow>("My Window");
    }

    private void OnEnable()
    {
        imageStyle = new GUIStyle();
        imageStyle.alignment = TextAnchor.MiddleCenter;
        imageSizes = new List<Vector2>();
    }

    private void OnGUI()
    {
        float totalWidth = position.width;
        float leftWidth = totalWidth * leftWidthRatio;
        float rightWidth = totalWidth * rightWidthRatio;
        float middleWidth = totalWidth - leftWidth - rightWidth;

        // Left section
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
        DrawCanvas(leftWidth);
        EditorGUILayout.EndVertical();

        // Right section
        EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));

        for (int i = 0; i < uploadedImages.Count; i++)
        {
            Texture2D uploadedImage = uploadedImages[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(uploadedImage, GUILayout.MinWidth(50), GUILayout.MinHeight(50));

            if (selectedLayerIndex == i)
            {
                GUI.backgroundColor = Color.yellow;
            }
            if (GUILayout.Button($"Layer {i + 1}", GUILayout.MinWidth(100)))
            {
                selectedLayerIndex = i;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCanvas(float canvasWidth)
    {
        Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, position.height);
        GUI.Box(canvasRect, GUIContent.none);

        if (Event.current.type == EventType.DragUpdated && canvasRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            Event.current.Use();
        }
        else if (Event.current.type == EventType.DragPerform && canvasRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            DragAndDrop.paths = FilterImagePaths(DragAndDrop.paths);
            foreach (string imagePath in DragAndDrop.paths)
            {
                Texture2D uploadedImage = LoadImageFromPath(imagePath);
                if (uploadedImage != null)
                {
                    uploadedImages.Add(uploadedImage);
                    imagePositions.Add(Event.current.mousePosition - canvasRect.position);
                    isDraggingList.Add(false);
                    imageSizes.Add(new Vector2(uploadedImage.width, uploadedImage.height));
                }
            }
            Event.current.Use();
        }

        for (int i = 0; i < uploadedImages.Count; i++)
        {
            int index = i;

            Texture2D uploadedImage = uploadedImages[i];
            Vector2 imagePosition = imagePositions[i];

            if (i < imageSizes.Count)
            {
                Vector2 imageSize = imageSizes[i];
                bool isDragging = isDraggingList[i];

                Rect imageRect = new Rect(canvasRect.position + imagePosition, imageSize);

                if (selectedLayerIndex == i)
                {
                    EditorGUI.DrawRect(imageRect, Color.white);
                }

                GUI.DrawTexture(imageRect, uploadedImage);

                EditorGUIUtility.AddCursorRect(canvasRect, MouseCursor.MoveArrow);

                if (Event.current.control && Event.current.type == EventType.ScrollWheel)
                {
                    if (imageRect.Contains(Event.current.mousePosition))
                    {
                        float scaleFactor = Event.current.delta.y > 0 ? 0.9f : 1.1f;
                        imageSize *= scaleFactor;
                        imageSize = Vector2.Max(imageSize, new Vector2(10, 10));
                        imageSize = Vector2.Min(imageSize, new Vector2(1000, 1000));

                        imageSizes[i] = imageSize;
                        Event.current.Use();
                    }
                }

                if (Event.current.type == EventType.MouseDown && imageRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        isDragging = true;
                        selectedLayerIndex = i;
                        Event.current.Use();
                    }
                    else if (Event.current.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Move Up"), false, () => MoveLayerUp(index));
                        menu.AddItem(new GUIContent("Move Down"), false, () => MoveLayerDown(index));
                        menu.AddItem(new GUIContent("Clone"), false, () => CloneLayer(index));
                        menu.AddItem(new GUIContent("Delete"), false, () => DeleteLayer(index));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Flip/Horizontal Flip"), false, () => FlipImageHorizontal(index));
                        menu.AddItem(new GUIContent("Flip/Vertical Flip"), false, () => FlipImageVertical(index));

                        menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDrag && isDragging)
                {
                    Vector2 newPosition = imagePosition + Event.current.delta;
                    newPosition.x = Mathf.Clamp(newPosition.x, 0f, canvasRect.width - imageSize.x);
                    newPosition.y = Mathf.Clamp(newPosition.y, 0f, canvasRect.height - imageSize.y);
                    imagePosition = newPosition;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && isDragging)
                {
                    isDragging = false;
                    selectedLayerIndex = -1;
                    Event.current.Use();
                }

                imagePositions[i] = imagePosition;
                isDraggingList[i] = isDragging;

                if (isDragging)
                {
                    if (showPixelAlignment && i < uploadedImages.Count - 1)
                    {
                        Rect nextImageRect = new Rect(canvasRect.position + imagePositions[i + 1], imageSizes[i + 1]);
                        Rect lineRect = new Rect(imageRect.xMax, imageRect.y, nextImageRect.xMin - imageRect.xMax, imageRect.height);
                        if (lineRect.width > 0 && lineRect.height > 0)
                        {
                            EditorGUI.DrawRect(lineRect, Color.red);
                        }
                    }

                    if (showHorizontalAlignment && i < uploadedImages.Count - 1 && Mathf.Approximately(imagePositions[i].y, imagePositions[i + 1].y))
                    {
                        Rect nextImageRect = new Rect(canvasRect.position + imagePositions[i + 1], imageSizes[i + 1]);
                        Rect lineRect = new Rect(imageRect.xMin, imageRect.y, Mathf.Max(imageRect.width, nextImageRect.width), 1f);
                        if (lineRect.width > 0 && lineRect.height > 0)
                        {
                            EditorGUI.DrawRect(lineRect, Color.red);
                        }
                    }
                }
            }
        }
    }


    private Texture2D LoadImageFromPath(string path)
    {
        Texture2D image = new Texture2D(2, 2);
        byte[] imageData = System.IO.File.ReadAllBytes(path);
        image.LoadImage(imageData);
        return image;
    }

    private string[] FilterImagePaths(string[] paths)
    {
        List<string> filteredPaths = new List<string>();
        foreach (string path in paths)
        {
            if (IsImagePath(path))
            {
                filteredPaths.Add(path);
            }
        }
        return filteredPaths.ToArray();
    }

    private bool IsImagePath(string path)
    {
        string extension = System.IO.Path.GetExtension(path).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp";
    }

    private void MoveLayerUp(int index)
    {
        if (index >= 0 && index < uploadedImages.Count - 1)
        {
            Texture2D image = uploadedImages[index];
            Vector2 position = imagePositions[index];
            bool isDragging = isDraggingList[index];
            Vector2 size = imageSizes[index];

            uploadedImages.RemoveAt(index);
            imagePositions.RemoveAt(index);
            isDraggingList.RemoveAt(index);
            imageSizes.RemoveAt(index);

            uploadedImages.Insert(index + 1, image);
            imagePositions.Insert(index + 1, position);
            isDraggingList.Insert(index + 1, isDragging);
            imageSizes.Insert(index + 1, size);

            selectedLayerIndex = index + 1;
        }

        Repaint();
    }

    private void MoveLayerDown(int index)
    {
        if (index > 0 && index < uploadedImages.Count)
        {
            Texture2D image = uploadedImages[index];
            Vector2 position = imagePositions[index];
            bool isDragging = isDraggingList[index];
            Vector2 size = imageSizes[index];

            uploadedImages.RemoveAt(index);
            imagePositions.RemoveAt(index);
            isDraggingList.RemoveAt(index);
            imageSizes.RemoveAt(index);

            uploadedImages.Insert(index - 1, image);
            imagePositions.Insert(index - 1, position);
            isDraggingList.Insert(index - 1, isDragging);
            imageSizes.Insert(index - 1, size);

            selectedLayerIndex = index - 1;
        }

        Repaint();
    }

    private void CloneLayer(int index)
    {
        if (index >= 0 && index < uploadedImages.Count)
        {
            Texture2D originalImage = uploadedImages[index];
            Texture2D clonedImage = new Texture2D(originalImage.width, originalImage.height);
            clonedImage.SetPixels(originalImage.GetPixels());
            clonedImage.Apply();

            uploadedImages.Insert(index + 1, clonedImage);
            imagePositions.Insert(index + 1, imagePositions[index]);
            isDraggingList.Insert(index + 1, false);
            imageSizes.Insert(index + 1, imageSizes[index]);
        }
    }

    private void DeleteLayer(int index)
    {
        if (index >= 0 && index < uploadedImages.Count)
        {
            uploadedImages.RemoveAt(index);
            imagePositions.RemoveAt(index);
            isDraggingList.RemoveAt(index);
            imageSizes.RemoveAt(index);

            if (selectedLayerIndex == index)
            {
                selectedLayerIndex = -1;
            }
            else if (selectedLayerIndex > index)
            {
                selectedLayerIndex--;
            }
        }
    }

    private void FlipImageHorizontal(int index)
    {
        if (index >= 0 && index < uploadedImages.Count)
        {
            Texture2D originalImage = uploadedImages[index];
            Texture2D flippedImage = new Texture2D(originalImage.width, originalImage.height);

            Color[] originalPixels = originalImage.GetPixels();
            Color[] flippedPixels = new Color[originalPixels.Length];

            int width = originalImage.width;
            int height = originalImage.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flippedPixels[y * width + x] = originalPixels[y * width + (width - x - 1)];
                }
            }

            flippedImage.SetPixels(flippedPixels);
            flippedImage.Apply();

            uploadedImages[index] = flippedImage;
        }
    }

    private void FlipImageVertical(int index)
    {
        if (index >= 0 && index < uploadedImages.Count)
        {
            Texture2D originalImage = uploadedImages[index];
            Texture2D flippedImage = new Texture2D(originalImage.width, originalImage.height);

            Color[] originalPixels = originalImage.GetPixels();
            Color[] flippedPixels = new Color[originalPixels.Length];

            int width = originalImage.width;
            int height = originalImage.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flippedPixels[y * width + x] = originalPixels[(height - y - 1) * width + x];
                }
            }

            flippedImage.SetPixels(flippedPixels);
            flippedImage.Apply();

            uploadedImages[index] = flippedImage;
        }
    }
}