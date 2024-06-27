using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class LayerEditor : EditorWindow
    {
        [SerializeField] private float rightWidthRatio = 0.1f;
        [SerializeField] private float leftWidthRatio = 0.9f;

        public List<Texture2D> uploadedImages = new();
        public List<Vector2> imagePositions = new();
        public List<bool> isDraggingList = new();
        public List<Vector2> imageSizes = new();
        public List<GameObject> spriteObjects = new();

        private GUIStyle imageStyle;

        public int selectedLayerIndex = -1;
        private int selectedImageIndex = -1;
        private const float HandleSize = 5f;

        private bool isRightPanelOpen = true;
        private bool showPixelAlignment = true;
        private bool showHorizontalAlignment = true;
        private Vector2 canvasScrollPosition;
        private bool isCropping = false;
        private Rect cropRect;
        private bool isCroppingActive = false;

        private double lastClickTime = 0;
        private const double DoubleClickTimeThreshold = 0.3;

        public Texture2D backgroundImage;

        private float zoomFactor = 1f;

        private LayerEditorRightPanel rightPanel;
        private ContextMenuActions contextMenuActions;

        [MenuItem("Scenario/Editors/Layer Editor", false, 3)]
        public static void ShowWindow()
        {
            GetWindow<LayerEditor>("Layer Editor");
        }

        private void OnEnable()
        {
            imageStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            };

            uploadedImages = new List<Texture2D>();
            imagePositions = new List<Vector2>();
            isDraggingList = new List<bool>();
            imageSizes = new List<Vector2>();
            spriteObjects = new List<GameObject>();

            rightPanel = new LayerEditorRightPanel(this);
            contextMenuActions = new ContextMenuActions(this);
        }

        private void OnDestroy()
        {
            uploadedImages.Clear();
            imagePositions.Clear();
            isDraggingList.Clear();
            imageSizes.Clear();
            spriteObjects.Clear();
        }

        private void OnGUI()
        {
            float totalWidth = position.width;
            float leftWidth = isRightPanelOpen ? totalWidth * leftWidthRatio : totalWidth;
            float rightWidth = isRightPanelOpen ? totalWidth * rightWidthRatio : 0f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
                {
                    DrawCanvas(leftWidth);
                }
                EditorGUILayout.EndVertical();

                if (isRightPanelOpen)
                {
                    rightPanel.DrawRightPanel(rightWidth);
                }
            }
            EditorGUILayout.EndHorizontal();

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 30
            };

            float buttonSize = 50;
            float buttonX = isRightPanelOpen ? position.width - buttonSize - rightWidth : position.width - buttonSize;
            if (GUI.Button(new Rect(buttonX, 0, buttonSize, buttonSize), "≡", buttonStyle))
            {
                isRightPanelOpen = !isRightPanelOpen;
                Repaint();
            }
        }


        private void DrawCanvas(float canvasWidth)
        {
            Color backgroundColor = new Color32(18, 18, 18, 255);
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

            Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, position.height);
            GUI.Box(canvasRect, GUIContent.none);

            Vector2 canvasContentSize = new Vector2(canvasWidth * zoomFactor, position.height * zoomFactor);

            canvasScrollPosition = GUI.BeginScrollView(canvasRect, canvasScrollPosition, new Rect(Vector2.zero, canvasContentSize));
            {
                canvasRect = DrawImageList(canvasWidth, canvasRect, canvasContentSize);
                DrawImageView(canvasRect);
            }
            GUI.EndScrollView();
        }

        private Rect DrawImageList(float canvasWidth, Rect canvasRect, Vector2 canvasContentSize)
        {
            GUI.BeginGroup(new Rect(Vector2.zero, canvasContentSize));
            {
                DrawBackgroundImage(canvasWidth);

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
                        if (uploadedImage == null) continue;
                    
                        uploadedImages.Add(uploadedImage);

                        // Drop the image at mouse position
                        Vector2 imageCenter = new Vector2(uploadedImage.width / 2f, uploadedImage.height / 2f);
                        Vector2 imagePosition = new Vector2(
                            Event.current.mousePosition.x - canvasRect.x,
                            Event.current.mousePosition.y - canvasRect.y
                        );

                        imagePositions.Add( imagePosition - imageCenter);
                        isDraggingList.Add(false);
                        imageSizes.Add(new Vector2(uploadedImage.width, uploadedImage.height));

                        Rect rect = new Rect(0, 0, uploadedImage.width, uploadedImage.height);
                        Vector2 pivot = new Vector2(0.5f, 0.5f);
                        Sprite sprite = Sprite.Create(uploadedImage, rect, pivot);

                        GameObject spriteObj = new GameObject("SpriteObj");
                        SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;

                        spriteObj.transform.position = new Vector3(0, 0, 0);
                        spriteObjects.Add(spriteObj);
                    }

                    Event.current.Use();
                }
            }
            GUI.EndGroup();
            return canvasRect;
        }

        private void DrawBackgroundImage(float canvasWidth)
        {
            if (backgroundImage == null) return;
        
            GUI.DrawTexture(new Rect(Vector2.zero, new Vector2(canvasWidth, position.height) * zoomFactor),
                backgroundImage, ScaleMode.ScaleToFit, true);
        }

        private void DrawImageView(Rect canvasRect)
        {
            GUI.BeginGroup(canvasRect);

            // Calculate the center of the canvas
            Vector2 canvasCenter = canvasRect.center;

            for (int i = 0; i < uploadedImages.Count; i++)
            {
                DrawImage(canvasCenter, i);
            }

            GUI.EndGroup();
        }

        private void DrawImage(Vector2 canvasCenter, int i)
        {
            int index = i;

            Texture2D uploadedImage = uploadedImages[i];
            Vector2 imagePosition = imagePositions[i];
            Vector2 imageSize = imageSizes[i];
            bool isDragging = isDraggingList[i];

            float imageSizeXHalf = (imageSize.x / 2.0f);
            float imageSizeYHalf = (imageSize.y / 2.0f);
            var halfSize = new Vector2(imageSizeXHalf, imageSizeYHalf);

            // Calculate the transformed position with the center as anchor
            Vector2 transformedPosition = canvasCenter + (imagePosition - halfSize) * zoomFactor;
            Vector2 transformedSize = imageSize * zoomFactor;

            Rect imageRect = new Rect(transformedPosition, transformedSize);
            GUI.DrawTexture(imageRect, uploadedImage);

            HandleImageEvents(i, index, ref imagePosition, ref imageSize, ref isDragging, imageRect);
        }

        private void HandleImageEvents(int i, int index, ref Vector2 imagePosition, ref Vector2 imageSize, ref bool isDragging, Rect imageRect)
        {
            Vector2 transformedMousePosition = Event.current.mousePosition / zoomFactor;

            if (Event.current.type == EventType.ScrollWheel)
            {
                HandleScrollWheel(i, ref imageSize, ref imageRect);
            }

            if (Event.current.type == EventType.MouseDown && imageRect.Contains(Event.current.mousePosition))
            {
                switch (Event.current.button)
                {
                    case 0:
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
                        break;
                    }
                    case 1:
                        CreateContextMenu(index);
                        break;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && isDragging && !isCropping)
            {
                Vector2 oldPosition = imagePosition;
                Vector2 transformedDelta = Event.current.delta / zoomFactor;
                Vector2 newPosition = imagePosition + transformedDelta;
           
                imagePosition = newPosition;
                Vector2 delta = newPosition - oldPosition;

                float scaleFactor = 0.01f;
                Vector3 sceneDelta = new Vector3(delta.x, -delta.y, 0) * scaleFactor;

                GameObject spriteObj = spriteObjects[i];
                spriteObj.transform.position += sceneDelta;

                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && isCropping)
            {
                OnMouseClickDrag_Crop(index, imageRect, transformedMousePosition);
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
                Event.current.Use();
            }

            imagePositions[i] = imagePosition;
            isDraggingList[i] = isDragging;

            HandleIsDragging(i, isDragging, imageRect);
            HandleIsCropping();
        }

        private void OnMouseClickDrag_Crop(int index, Rect imageRect, Vector2 transformedMousePosition)
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

            cropRect.width = Mathf.Clamp(cropRect.width, 0f, imageRect.width);
            cropRect.height = Mathf.Clamp(cropRect.height, 0f, imageRect.height);
            cropRect.x = Mathf.Clamp(cropRect.x, imageRect.x, imageRect.xMax - cropRect.width);
            cropRect.y = Mathf.Clamp(cropRect.y, imageRect.y, imageRect.yMax - cropRect.height);

            Event.current.Use();
        }

        private void HandleIsCropping()
        {
            if (!isCropping) return;
        
            float borderThickness = 1f;
            Color borderColor = Color.white;

            EditorGUI.DrawRect(cropRect, new Color(1, 1, 1, 0.1f));

            float lineHeight = cropRect.height / 3f;
            float columnWidth = cropRect.width / 3f;

            for (int lineIndex = 1; lineIndex <= 2; lineIndex++)
            {
                float lineY = cropRect.y + lineIndex * lineHeight;
                Rect lineRect = new Rect(cropRect.x, lineY, cropRect.width, borderThickness);
                EditorGUI.DrawRect(lineRect, borderColor);
            }

            for (int columnIndex = 1; columnIndex <= 2; columnIndex++)
            {
                float lineX = cropRect.x + columnIndex * columnWidth;
                Rect lineRect = new Rect(lineX, cropRect.y, borderThickness, cropRect.height);
                EditorGUI.DrawRect(lineRect, borderColor);
            }

            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y - borderThickness, cropRect.width + 2 * borderThickness, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y + cropRect.height, cropRect.width + 2 * borderThickness, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x - borderThickness, cropRect.y, borderThickness, cropRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(cropRect.x + cropRect.width, cropRect.y, borderThickness, cropRect.height), borderColor);
        }

        private void HandleIsDragging(int i, bool isDragging, Rect imageRect)
        {
            if (!isDragging) return;
        
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

        private void HandleScrollWheel(int i, ref Vector2 imageSize, ref Rect imageRect)
        {
            if (Event.current.control)
            {
                if (imageRect.Contains(Event.current.mousePosition))
                {
                    float scaleFactor = ScaleImage(ref imageSize);
                    imageSizes[i] = imageSize;

                    // Scale the corresponding GameObject as well
                    GameObject spriteObj = spriteObjects[i];
                    spriteObj.transform.localScale *= scaleFactor;
                }
                Event.current.Use();
            }
            else
            {
                Vector2 mousePositionBeforeZoom = (Event.current.mousePosition + canvasScrollPosition) / zoomFactor;
                zoomFactor -= Event.current.delta.y * 0.01f;
                zoomFactor = Mathf.Clamp(zoomFactor, 0.1f, 1f);
                Vector2 mousePositionAfterZoom = (Event.current.mousePosition + canvasScrollPosition) / zoomFactor;
                canvasScrollPosition += (mousePositionAfterZoom - mousePositionBeforeZoom) * zoomFactor;
                Event.current.Use();
            }
        }

        private static float ScaleImage(ref Vector2 imageSize)
        {
            float scaleFactor = Event.current.delta.y > 0 ? 0.9f : 1.1f;
            imageSize *= scaleFactor;
            imageSize = Vector2.Max(imageSize, new Vector2(10, 10));
            imageSize = Vector2.Min(imageSize, new Vector2(1000, 1000));
            return scaleFactor;
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
            return extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp";
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

        private void CropImage(int index, Rect rectCrop)
        {
            Texture2D originalImage = uploadedImages[index];

            int x = Mathf.RoundToInt(rectCrop.x - imagePositions[index].x);
            int y = Mathf.RoundToInt((imagePositions[index].y + imageSizes[index].y) - (rectCrop.y + rectCrop.height));
            int width = Mathf.RoundToInt(rectCrop.width);
            int height = Mathf.RoundToInt(rectCrop.height);

            x = Mathf.Clamp(x, 0, originalImage.width);
            y = Mathf.Clamp(y, 0, originalImage.height);
            width = Mathf.Clamp(width, 0, originalImage.width - x);
            height = Mathf.Clamp(height, 0, originalImage.height - y);

            Texture2D croppedImage = new Texture2D(width, height);
            Color[] pixels = originalImage.GetPixels(x, y, width, height);
            croppedImage.SetPixels(pixels);
            croppedImage.Apply();

            uploadedImages[index] = croppedImage;
            imagePositions[index] = new Vector2(imagePositions[index].x + x, imagePositions[index].y + imageSizes[index].y - (y + height));
            imageSizes[index] = new Vector2(width, height);

            Rect spriteRect = new Rect(0, 0, croppedImage.width, croppedImage.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite newSprite = Sprite.Create(croppedImage, spriteRect, pivot);

            GameObject spriteObj = spriteObjects[index];
            SpriteRenderer renderer = spriteObj.GetComponent<SpriteRenderer>();
            renderer.sprite = newSprite;
        }
    }
}