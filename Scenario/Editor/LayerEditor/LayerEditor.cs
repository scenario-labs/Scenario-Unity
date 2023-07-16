using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using RestSharp;
using Newtonsoft.Json;
using UnityEditor.EditorTools;
using Unity.EditorCoroutines.Editor;

public class LayerEditor : EditorWindow
{
    [SerializeField] private float rightWidthRatio = 0.1f; 
    [SerializeField] private float leftWidthRatio = 0.9f;

    private List<Texture2D> uploadedImages = new List<Texture2D>();
    private List<Vector2> imagePositions = new List<Vector2>();
    private List<bool> isDraggingList = new List<bool>();
    private List<Vector2> imageSizes = new List<Vector2>();

    private GUIStyle imageStyle;

    private int selectedLayerIndex = -1;
    private int selectedImageIndex = -1;
    private const float HandleSize = 5f;

    private bool showPixelAlignment = true;
    private bool showHorizontalAlignment = true;
    private Vector2 canvasScrollPosition;
    private bool isCropping = false; 
    private Rect cropRect;
    private bool isCroppingActive = false;

    private double lastClickTime = 0;
    private const double DoubleClickTimeThreshold = 0.3;

    private Texture2D backgroundImage;

    private float zoomFactor = 1f;

    // Create an instance of ContextMenuActions
    private ContextMenuActions contextMenuActions;

    // Create public properties to access private fields
    public List<Texture2D> UploadedImages => uploadedImages;
    public List<Vector2> ImagePositions => imagePositions;
    public List<bool> IsDraggingList => isDraggingList;
    public List<Vector2> ImageSizes => imageSizes;
    public int SelectedLayerIndex 
    {
        get => selectedLayerIndex;
        set
        {
            if (value >= -1 && value < UploadedImages.Count)
            {
                selectedLayerIndex = value;
            }
        }
    }
    public Texture2D BackgroundImage  
    {
        get => backgroundImage;
        set
        {
            backgroundImage = value;
        }
    }

    [MenuItem("Window/Layer Editor")]
    public static void ShowWindow()
    {
        GetWindow<LayerEditor>("Layer Editor");
    }

    private void OnEnable()
    {
        imageStyle = new GUIStyle();
        imageStyle.alignment = TextAnchor.MiddleCenter;
        imageSizes = new List<Vector2>();
        
        // Instantiate the ContextMenuActions class
        contextMenuActions = new ContextMenuActions(this);
    }

    private void OnGUI()
    {
        float totalWidth = position.width;
        float leftWidth = totalWidth * leftWidthRatio;
        float rightWidth = totalWidth * rightWidthRatio;
        float middleWidth = totalWidth - leftWidth - rightWidth;

        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
        DrawCanvas(leftWidth);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));
        
        for (int i = 0; i < UploadedImages.Count; i++)
        {
            Texture2D uploadedImage = UploadedImages[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(uploadedImage, GUILayout.MinWidth(50), GUILayout.MinHeight(50));
            
            if (SelectedLayerIndex == i) 
            {
                GUI.backgroundColor = Color.yellow;
            }
            if (GUILayout.Button($"Layer {i + 1}", GUILayout.MinWidth(100))) 
            {
                SelectedLayerIndex = i;
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

        // Calculate the size of the zoomed-in canvas
        Vector2 canvasContentSize = new Vector2(canvasWidth * zoomFactor, position.height * zoomFactor);

        // Begin a scroll view with scrollbars based on the canvas size
        canvasScrollPosition = GUI.BeginScrollView(canvasRect, canvasScrollPosition, new Rect(Vector2.zero, canvasContentSize));
        
        // Draw the canvas content
        GUI.BeginGroup(new Rect(Vector2.zero, canvasContentSize));
        
        if (backgroundImage != null)
        {
            GUI.DrawTexture(new Rect(Vector2.zero, new Vector2(canvasWidth, position.height) * zoomFactor), backgroundImage, ScaleMode.ScaleToFit, true);
        }

        if (Event.current.type == EventType.DragUpdated && canvasRect.Contains(Event.current.mousePosition)) 
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            Event.current.Use();
        }
        else if (Event.current.type == EventType.DragPerform && canvasRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            DragAndDrop.paths = FilterImagePaths(DragAndDrop.paths);
            Vector2 canvasCenter = new Vector2(canvasRect.width / 2f, canvasRect.height / 2f);
            foreach (string imagePath in DragAndDrop.paths)
            {
                Texture2D uploadedImage = LoadImageFromPath(imagePath);
                if (uploadedImage != null)
                {
                    uploadedImages.Add(uploadedImage);
                    imagePositions.Add(canvasCenter - new Vector2(uploadedImage.width / 2f, uploadedImage.height / 2f));
                    isDraggingList.Add(false);
                    imageSizes.Add(new Vector2(uploadedImage.width, uploadedImage.height)); 
                }
            }
            Event.current.Use();
        }

        GUI.EndGroup();

        GUI.BeginGroup(canvasRect);
        
        for (int i = 0; i < uploadedImages.Count; i++)
        {
            int index = i;
            
            Texture2D uploadedImage = uploadedImages[i];
            Vector2 imagePosition = imagePositions[i];
            Vector2 imageSize = imageSizes[i]; 
            bool isDragging = isDraggingList[i];

            Vector2 transformedPosition = imagePosition * zoomFactor;
            Vector2 transformedSize = imageSize * zoomFactor;

            Rect imageRect = new Rect(transformedPosition, transformedSize);

            GUI.DrawTexture(imageRect, uploadedImage);

            Vector2 transformedMousePosition = Event.current.mousePosition / zoomFactor;

            if (Event.current.control && Event.current.type == EventType.ScrollWheel && imageRect.Contains(Event.current.mousePosition))
            {
                float scaleFactor = Event.current.delta.y > 0 ? 0.9f : 1.1f;
                imageSize *= scaleFactor;
                imageSize = Vector2.Max(imageSize, new Vector2(10, 10));
                imageSize = Vector2.Min(imageSize, new Vector2(1000, 1000));
                imageSizes[i] = imageSize;
                Event.current.Use();
            }

            if (!Event.current.control && Event.current.type == EventType.ScrollWheel)
            {
                zoomFactor -= Event.current.delta.y * 0.01f;
                zoomFactor = Mathf.Clamp(zoomFactor, 0.1f, 10f);
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && imageRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    double clickTime = EditorApplication.timeSinceStartup;
                    if (clickTime - lastClickTime < DoubleClickTimeThreshold)
                    {
                        if (selectedImageIndex == i && isCropping && isCroppingActive)
                        {
                            CropImage(i, cropRect);
                            isCroppingActive = false;
                        }
                        else 
                        {
                            selectedImageIndex = i;
                            isDragging = false;
                            isCropping = true;
                            isCroppingActive = true;
                            cropRect = imageRect;
                        }
                    }
                    else
                    {
                        selectedImageIndex = i;
                        isDragging = true;
                        isCropping = false;
                        isCroppingActive = false;
                        cropRect = Rect.zero;
                    }

                    lastClickTime = clickTime;
                    Event.current.Use();
                }
                else if (Event.current.button == 1)
                {
                    CreateContextMenu(index);
                }
            }
            else if (Event.current.type == EventType.MouseDrag && isDragging && !isCropping)
            {
                Vector2 transformedDelta = Event.current.delta / zoomFactor;
                
                Vector2 newPosition = imagePosition + transformedDelta;
                newPosition.x = Mathf.Clamp(newPosition.x, 0f, (canvasRect.width / zoomFactor) - imageSize.x);
                newPosition.y = Mathf.Clamp(newPosition.y, 0f, (canvasRect.height / zoomFactor) - imageSize.y);
                imagePosition = newPosition;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && isCropping)
            {
                if (Event.current.button == 0)
                {
                    bool croppingRight = Mathf.Abs(transformedMousePosition.x - cropRect.xMax) < HandleSize;
                    bool croppingLeft = Mathf.Abs(transformedMousePosition.x - cropRect.xMin) < HandleSize;
                    bool croppingBottom = Mathf.Abs(transformedMousePosition.y - cropRect.yMax) < HandleSize;
                    bool croppingTop = Mathf.Abs(transformedMousePosition.y - cropRect.yMin) < HandleSize;

                    if (croppingRight)
                    {
                        int prevWidth = Mathf.RoundToInt(cropRect.width);
                        cropRect.width += Event.current.delta.x;
                        cropRect.width = Mathf.Max(cropRect.width, 10f);
                        int newWidth = Mathf.RoundToInt(cropRect.width);
                        DeletePixelsHorizontal(index, prevWidth, newWidth);
                    }
                    else if (croppingLeft)
                    {
                        int prevWidth = Mathf.RoundToInt(cropRect.width);
                        cropRect.x += Event.current.delta.x;
                        cropRect.width -= Event.current.delta.x;
                        cropRect.width = Mathf.Max(cropRect.width, 10f);
                        int newWidth = Mathf.RoundToInt(cropRect.width);
                        DeletePixelsHorizontal(index, newWidth, prevWidth);
                    }

                    if (croppingBottom)
                    {
                        int prevHeight = Mathf.RoundToInt(cropRect.height);
                        cropRect.height += Event.current.delta.y;
                        cropRect.height = Mathf.Max(cropRect.height, 10f);
                        int newHeight = Mathf.RoundToInt(cropRect.height);
                        DeletePixelsVertical(index, prevHeight, newHeight);
                    }
                    else if (croppingTop)
                    {
                        int prevHeight = Mathf.RoundToInt(cropRect.height);
                        cropRect.y += Event.current.delta.y;
                        cropRect.height -= Event.current.delta.y;
                        cropRect.height = Mathf.Max(cropRect.height, 10f);
                        int newHeight = Mathf.RoundToInt(cropRect.height);
                        DeletePixelsVertical(index, newHeight, prevHeight);
                    }
                }
                else if (Event.current.button == 2)
                {
                    cropRect.position += Event.current.delta;   
                }

                cropRect.width = Mathf.Clamp(cropRect.width, 0f, imageRect.width);
                cropRect.height = Mathf.Clamp(cropRect.height, 0f, imageRect.height);
                cropRect.x = Mathf.Clamp(cropRect.x, imageRect.x, imageRect.xMax - cropRect.width);
                cropRect.y = Mathf.Clamp(cropRect.y, imageRect.y, imageRect.yMax - cropRect.height);
                
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
                selectedImageIndex = -1;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp && isCroppingActive)
            {
                CropImage(i, cropRect);
                isCroppingActive = false;
                Event.current.Use();
            }

            imagePositions[i] = imagePosition;
            isDraggingList[i] = isDragging;

            if (isDragging)
            {
                if (showPixelAlignment && i < uploadedImages.Count - 1)
                {
                    Rect nextImageRect = new Rect(imagePositions[i + 1] * zoomFactor, imageSizes[i + 1] * zoomFactor);
                    Rect lineRect = new Rect(imageRect.xMax, imageRect.y, nextImageRect.xMin - imageRect.xMax, imageRect.height);
                    if (lineRect.width > 0 && lineRect.height > 0)
                    {
                        EditorGUI.DrawRect(lineRect, Color.red);
                    }
                }

                if (showHorizontalAlignment && i < uploadedImages.Count - 1 && Mathf.Approximately(imagePositions[i].y, imagePositions[i + 1].y))
                {
                    Rect nextImageRect = new Rect(imagePositions[i + 1] * zoomFactor, imageSizes[i + 1] * zoomFactor);
                    Rect lineRect = new Rect(imageRect.xMin, imageRect.y, Mathf.Max(imageRect.width, nextImageRect.width), 1f);
                    if (lineRect.width > 0 && lineRect.height > 0)
                    {
                        EditorGUI.DrawRect(lineRect, Color.red);
                    }
                }
            }

            if (isCropping)
        {
            float borderThickness = 1f;
            Color borderColor = Color.red;

            EditorGUI.DrawRect(cropRect, new Color(1, 1, 1, 0.1f));

            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y - borderThickness, cropRect.width + 2 * borderThickness, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y + cropRect.height, cropRect.width + 2 * borderThickness, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y, borderThickness, cropRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x + cropRect.width, cropRect.y, borderThickness, cropRect.height), borderColor);
            
            // Add handles at the corners
            float handleSize = 10f;
            EditorGUIUtility.AddCursorRect(new Rect(cropRect.x - handleSize / 2, cropRect.y - handleSize / 2, handleSize, handleSize), MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(new Rect(cropRect.xMax - handleSize / 2, cropRect.y - handleSize / 2, handleSize, handleSize), MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(new Rect(cropRect.x - handleSize / 2, cropRect.yMax - handleSize / 2, handleSize, handleSize), MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(new Rect(cropRect.xMax - handleSize / 2, cropRect.yMax - handleSize / 2, handleSize, handleSize), MouseCursor.ResizeUpLeft);
        }
        }

        GUI.EndGroup();
        GUI.EndScrollView();
    }

    private void CreateContextMenu(int index)
    {
        contextMenuActions.CreateContextMenu(index); 
    }

    private Texture2D LoadImageFromPath(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        byte[] imageData = File.ReadAllBytes(path);
        texture.LoadImage(imageData);
        return texture;
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
        string extension = Path.GetExtension(path).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp";
    }

    private void DeletePixelsHorizontal(int index, int prevWidth, int newWidth)
    {
        Texture2D image = uploadedImages[index];
        int startX = Mathf.Min(prevWidth, newWidth);
        int endX = Mathf.Max(prevWidth, newWidth);

        if (newWidth < prevWidth)
        {
            for (int x = startX; x < endX; x++)
            {
                for (int y = 0; y < image.height; y++)
                {
                    image.SetPixel(x, y, Color.clear);
                }
            }
        }
        else
        {
            for (int x = startX; x < endX; x++)
            {
                for (int y = 0; y < image.height; y++)
                {
                    int reversedX = image.width - 1 - x;
                    image.SetPixel(reversedX, y, Color.clear);
                }
            }
        }

        image.Apply();
    }

    private void DeletePixelsVertical(int index, int prevHeight, int newHeight)
    {
        Texture2D image = uploadedImages[index];
        int startY = Mathf.Min(prevHeight, newHeight);
        int endY = Mathf.Max(prevHeight, newHeight);

        if (newHeight < prevHeight) 
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    int reversedY = image.height - 1 - y;
                    image.SetPixel(x, reversedY, Color.clear);
                }
            }
        }
        else
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    image.SetPixel(x, y, Color.clear);
                }
            }
        }

        image.Apply();
    }

    private void CropImage(int index, Rect cropRect)
    {
        Texture2D originalImage = uploadedImages[index];
        int x = Mathf.RoundToInt(cropRect.x - imagePositions[index].x);
        int y = Mathf.RoundToInt(cropRect.y - imagePositions[index].y);
        int width = Mathf.RoundToInt(cropRect.width);
        int height = Mathf.RoundToInt(cropRect.height);

        x = Mathf.Clamp(x, 0, originalImage.width);
        y = Mathf.Clamp(y, 0, originalImage.height);
        width = Mathf.Clamp(width, 0, originalImage.width - x);
        height = Mathf.Clamp(height, 0, originalImage.height - y);

        Texture2D croppedImage = new Texture2D(width, height);
        Color[] pixels = originalImage.GetPixels(x, y, width, height);
        croppedImage.SetPixels(pixels); 
        croppedImage.Apply();

        uploadedImages[index] = croppedImage;
        imagePositions[index] = new Vector2(imagePositions[index].x + x, imagePositions[index].y + y);
        imageSizes[index] = new Vector2(width, height);
    }
}