using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using RestSharp;
using Newtonsoft.Json;
using UnityEditor.EditorTools;
using Unity.EditorCoroutines.Editor;

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
    private int selectedImageIndex = -1;
    private const float ResizeHandleSize = 5f;


    private bool showPixelAlignment = true;
    private bool showHorizontalAlignment = true;

    private bool isCropping = false;
    private Rect cropRect;

    private double lastClickTime = 0;
    private const double DoubleClickTimeThreshold = 0.3;

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

                if (i == selectedImageIndex && isCropping)
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
                        double clickTime = EditorApplication.timeSinceStartup;
                        if (clickTime - lastClickTime < DoubleClickTimeThreshold)
                        {
                            // Double-click event
                            if (selectedImageIndex == i && isCropping)
                            {
                                CropImage(i, cropRect);
                            }
                            else
                            {
                                selectedImageIndex = i;
                                isDragging = false;
                                isCropping = true;
                                cropRect = imageRect;
                            }
                        }
                        else
                        {
                            // Single-click event
                            selectedImageIndex = i;
                            isDragging = true;
                            isCropping = false;
                            cropRect = Rect.zero;
                        }

                        lastClickTime = clickTime;
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
                        menu.AddItem(new GUIContent("Remove/Background"), false, () => RemoveBackground(index));

                        menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDrag && isDragging && !isCropping)
                {
                    Vector2 newPosition = imagePosition + Event.current.delta;
                    newPosition.x = Mathf.Clamp(newPosition.x, 0f, canvasRect.width - imageSize.x);
                    newPosition.y = Mathf.Clamp(newPosition.y, 0f, canvasRect.height - imageSize.y);
                    imagePosition = newPosition;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && selectedImageIndex == i)
                {
                    if (Event.current.button == 0)
                    {
                        isCropping = true;
                        cropRect = new Rect(imageRect);
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDrag && isCropping)
                {
                    if (Event.current.button == 0)
                    {
                        // Determine which border is being dragged
                        bool resizeRight = Mathf.Abs(Event.current.mousePosition.x - cropRect.xMax) < ResizeHandleSize;
                        bool resizeLeft = Mathf.Abs(Event.current.mousePosition.x - cropRect.xMin) < ResizeHandleSize;
                        bool resizeBottom = Mathf.Abs(Event.current.mousePosition.y - cropRect.yMax) < ResizeHandleSize;
                        bool resizeTop = Mathf.Abs(Event.current.mousePosition.y - cropRect.yMin) < ResizeHandleSize;

                        if (resizeRight)
                        {
                            // Resize right border
                            cropRect.width += Event.current.delta.x;
                            cropRect.width = Mathf.Max(cropRect.width, 10f);
                        }
                        else if (resizeLeft)
                        {
                            // Resize left border
                            cropRect.x += Event.current.delta.x;
                            cropRect.width -= Event.current.delta.x;
                            cropRect.width = Mathf.Max(cropRect.width, 10f);
                        }

                        if (resizeBottom)
                        {
                            // Resize bottom border
                            cropRect.height += Event.current.delta.y;
                            cropRect.height = Mathf.Max(cropRect.height, 10f);
                        }
                        else if (resizeTop)
                        {
                            // Resize top border
                            cropRect.y += Event.current.delta.y;
                            cropRect.height -= Event.current.delta.y;
                            cropRect.height = Mathf.Max(cropRect.height, 10f);
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        // Move the cropping rectangle
                        cropRect.position += Event.current.delta;
                    }

                    // Clamp the cropping rectangle within the image boundaries
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
                else if (Event.current.type == EventType.MouseUp && isCropping)
                {
                    CropImage(i, cropRect);
                    isCropping = false;
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

                if (isCropping)
                {
                    float borderThickness = 10f; // Adjust this to change the thickness of the border
                    Color borderColor = Color.red; // Adjust this to change the color of the border

                    // Draw the cropping rectangle with a semi-transparent white color
                    EditorGUI.DrawRect(cropRect, new Color(1, 1, 1, 0.1f));

                    // Draw the border rectangles
                    EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y - borderThickness, cropRect.width + 2 * borderThickness, borderThickness), borderColor); // Top border
                    EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y + cropRect.height, cropRect.width + 2 * borderThickness, borderThickness), borderColor); // Bottom border
                    EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y, borderThickness, cropRect.height), borderColor); // Left border
                    EditorGUI.DrawRect(new Rect(cropRect.x + cropRect.width, cropRect.y, borderThickness, cropRect.height), borderColor); // Right border
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
            try
            {
                uploadedImages.RemoveAt(index);
                imagePositions.RemoveAt(index);
                isDraggingList.RemoveAt(index);
                imageSizes.RemoveAt(index);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error deleting layer: " + ex.Message);
                return;
            }

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

    internal void RemoveBackground(int index)
    {
        if (index >= 0 && index < uploadedImages.Count)
        {
            Texture2D texture2D = uploadedImages[index];
            var imgBytes = texture2D.EncodeToPNG();
            string base64String = Convert.ToBase64String(imgBytes);
            string dataUrl = $"data:image/png;base64,{base64String}";
            EditorCoroutineUtility.StartCoroutineOwnerless(PutRemoveBackground(dataUrl, index));
        }
    }

    IEnumerator PutRemoveBackground(string dataUrl, int index)
    {
        string name = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";

        string url = $"{PluginSettings.ApiUrl}/images/erase-background";

        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest(Method.PUT);

        string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{name}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";
        Debug.Log(param);

        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");
        request.AddParameter("application/json", param, ParameterType.RequestBody);

        yield return client.ExecuteAsync(request, response =>
        {
            if (response.ErrorException != null)
            {
                Debug.Log($"Error: {response.ErrorException.Message}");
            }
            else
            {
                try
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                    string imageUrl = jsonResponse.asset.url;

                    EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImageIntoMemory(imageUrl, index));
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred while processing the response: " + ex.Message);
                }
            }
        });
    }

    IEnumerator DownloadImageIntoMemory(string imageUrl, int index)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                uploadedImages[index] = texture;
            }
        }
    }

    private void CropImage(int index, Rect cropRect)
    {
        Texture2D originalImage = uploadedImages[index];
        int x = Mathf.RoundToInt(cropRect.x - imagePositions[index].x);
        int y = Mathf.RoundToInt(cropRect.y - imagePositions[index].y);
        int width = Mathf.RoundToInt(cropRect.width);
        int height = Mathf.RoundToInt(cropRect.height);

        Texture2D croppedImage = new Texture2D(width, height);
        Color[] pixels = originalImage.GetPixels(x, y, width, height);
        croppedImage.SetPixels(pixels);
        croppedImage.Apply();

        uploadedImages[index] = croppedImage;
        imageSizes[index] = new Vector2(width, height);
    }
}