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

        [MenuItem("Window/Scenario/Editors/Layer Editor", false, 3)]
        public static void ShowWindow()
        {
            GetWindow<LayerEditor>("Layer Editor");
        }

        private void OnEnable()
        {
            InitializeFields();
            rightPanel = new LayerEditorRightPanel(this);
            contextMenuActions = new ContextMenuActions(this);
        }

        private void OnDestroy()
        {
            ClearFields();
        }

        private void OnGUI()
        {
            DrawLayout();
            DrawTogglePanelButton();
        }

        private void DrawLayout()
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
        }

        private void DrawTogglePanelButton()
        {
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 30 };
            float buttonSize = 50;
            float buttonX = isRightPanelOpen ? position.width - buttonSize - (position.width * rightWidthRatio) : position.width - buttonSize;

            if (GUI.Button(new Rect(buttonX, 0, buttonSize, buttonSize), "≡", buttonStyle))
            {
                isRightPanelOpen = !isRightPanelOpen;
                Repaint();
            }
        }

        private void DrawCanvas(float canvasWidth)
        {
            DrawBackground(canvasWidth);
            Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, position.height);
            GUI.Box(canvasRect, GUIContent.none);

            Vector2 canvasContentSize = new Vector2(canvasWidth * zoomFactor, position.height * zoomFactor);
            canvasScrollPosition = GUI.BeginScrollView(canvasRect, canvasScrollPosition, new Rect(Vector2.zero, canvasContentSize));
            {
                DrawCanvasContent(canvasWidth, canvasRect, canvasContentSize);
            }
            GUI.EndScrollView();
        }

        private void DrawBackground(float canvasWidth)
        {
            Color backgroundColor = new Color32(18, 18, 18, 255);
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void DrawCanvasContent(float canvasWidth, Rect canvasRect, Vector2 canvasContentSize)
        {
            GUI.BeginGroup(new Rect(Vector2.zero, canvasContentSize));
            {
                DrawBackgroundImage(canvasWidth);
                HandleDragAndDrop(canvasRect);
            }
            GUI.EndGroup();
        }

        private void DrawBackgroundImage(float canvasWidth)
        {
            if (backgroundImage != null)
            {
                GUI.DrawTexture(new Rect(Vector2.zero, new Vector2(canvasWidth, position.height) * zoomFactor), backgroundImage, ScaleMode.ScaleToFit, true);
            }
        }

        private void HandleDragAndDrop(Rect canvasRect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated && canvasRect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform && canvasRect.Contains(evt.mousePosition))
            {
                AcceptDragAndDrop(evt, canvasRect);
            }
        }

        private void AcceptDragAndDrop(Event evt, Rect canvasRect)
        {
            DragAndDrop.AcceptDrag();
            DragAndDrop.paths = FilterImagePaths(DragAndDrop.paths);

            foreach (string imagePath in DragAndDrop.paths)
            {
                Texture2D uploadedImage = LoadImageFromPath(imagePath);
                if (uploadedImage == null) continue;

                AddImageToCanvas(evt, canvasRect, uploadedImage);
            }

            evt.Use();
        }

        private void AddImageToCanvas(Event evt, Rect canvasRect, Texture2D uploadedImage)
        {
            uploadedImages.Add(uploadedImage);

            Vector2 imageCenter = new Vector2(uploadedImage.width / 2f, uploadedImage.height / 2f);
            Vector2 imagePosition = new Vector2(evt.mousePosition.x - canvasRect.x, evt.mousePosition.y - canvasRect.y) - imageCenter;

            imagePositions.Add(imagePosition);
            isDraggingList.Add(false);
            imageSizes.Add(new Vector2(uploadedImage.width, uploadedImage.height));

            GameObject spriteObj = CreateSpriteObject(uploadedImage);
            spriteObjects.Add(spriteObj);
        }

        private GameObject CreateSpriteObject(Texture2D uploadedImage)
        {
            Rect rect = new Rect(0, 0, uploadedImage.width, uploadedImage.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite sprite = Sprite.Create(uploadedImage, rect, pivot);

            GameObject spriteObj = new GameObject("SpriteObj");
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            spriteObj.transform.position = Vector3.zero;

            return spriteObj;
        }

        private void DrawImageView(Rect canvasRect)
        {
            GUI.BeginGroup(canvasRect);
            Vector2 canvasCenter = canvasRect.center;

            for (int i = 0; i < uploadedImages.Count; i++)
            {
                DrawImage(canvasCenter, i);
            }

            GUI.EndGroup();
        }

        private void DrawImage(Vector2 canvasCenter, int i)
        {
            Texture2D uploadedImage = uploadedImages[i];
            Vector2 imagePosition = imagePositions[i];
            Vector2 imageSize = imageSizes[i];
            bool isDragging = isDraggingList[i];

            Vector2 imageCenterPoint = imagePosition + imageSize / 2f;
            Vector2 transformedCenterPoint = canvasCenter + (imageCenterPoint - canvasCenter) * zoomFactor;
            Vector2 transformedPosition = transformedCenterPoint - imageSize * zoomFactor / 2f;
            Vector2 transformedSize = imageSize * zoomFactor;

            Rect imageRect = new Rect(transformedPosition, transformedSize);
            GUI.DrawTexture(imageRect, uploadedImage);

            HandleImageEvents(i, ref imagePosition, ref imageSize, ref isDragging, imageRect);
        }

        private void HandleImageEvents(int i, ref Vector2 imagePosition, ref Vector2 imageSize, ref bool isDragging, Rect imageRect)
        {
            Event evt = Event.current;
            Vector2 transformedMousePosition = evt.mousePosition / zoomFactor;

            switch (evt.type)
            {
                case EventType.ScrollWheel:
                    HandleScrollWheel(i, ref imageSize, ref imagePosition, ref imageRect);
                    break;
                case EventType.MouseDown when imageRect.Contains(evt.mousePosition):
                    HandleMouseDown(i, ref isDragging, ref imageRect);
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(i, ref imagePosition, ref isDragging, imageRect, transformedMousePosition);
                    break;
                case EventType.MouseUp:
                    HandleMouseUp(ref isDragging);
                    break;
            }

            imagePositions[i] = imagePosition;
            isDraggingList[i] = isDragging;

            HandleIsDragging(i, isDragging, imageRect);
            HandleIsCropping();
        }

        private void HandleMouseDown(int i, ref bool isDragging, ref Rect imageRect)
        {
            double clickTime = EditorApplication.timeSinceStartup;

            if (clickTime - lastClickTime < DoubleClickTimeThreshold)
            {
                HandleDoubleClick(i, ref isDragging, imageRect);
            }
            else
            {
                HandleSingleClick(i, ref isDragging);
            }

            lastClickTime = clickTime;
            Event.current.Use();
        }

        private void HandleDoubleClick(int i, ref bool isDragging, Rect imageRect)
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

        private void HandleSingleClick(int i, ref bool isDragging)
        {
            selectedImageIndex = i;
            isDragging = true;
            isCropping = false;
            isCroppingActive = false;
            cropRect = Rect.zero;
        }

        private void HandleMouseDrag(int i, ref Vector2 imagePosition, ref bool isDragging, Rect imageRect, Vector2 transformedMousePosition)
        {
            if (isDragging && !isCropping)
            {
                DragImage(i, ref imagePosition);
            }
            else if (isCropping)
            {
                OnMouseClickDrag_Crop(i, imageRect, transformedMousePosition);
            }

            Event.current.Use();
        }

        private void DragImage(int i, ref Vector2 imagePosition)
        {
            Vector2 oldPosition = imagePosition;
            Vector2 transformedDelta = Event.current.delta / zoomFactor;
            imagePosition += transformedDelta;
            Vector2 delta = imagePosition - oldPosition;

            GameObject spriteObj = spriteObjects[i];
            spriteObj.transform.position += new Vector3(delta.x, -delta.y, 0) * 0.01f;
        }

        private void HandleMouseUp(ref bool isDragging)
        {
            if (isDragging)
            {
                isDragging = false;
                selectedImageIndex = -1;
            }
            else if (isCropping)
            {
                CropImage(selectedImageIndex, cropRect);
            }

            Event.current.Use();
        }

        private void HandleScrollWheel(int i, ref Vector2 imageSize, ref Vector2 imagePosition, ref Rect imageRect)
        {
            Event evt = Event.current;
            if (evt.control)
            {
                if (imageRect.Contains(evt.mousePosition))
                {
                    float scaleFactor = ScaleImage(ref imageSize, ref imagePosition, evt.delta.y > 0 ? 0.9f : 1.1f);
                    imageSizes[i] = imageSize;
                    imagePositions[i] = imagePosition;
                    ScaleSpriteObject(i, scaleFactor);
                }
                evt.Use();
            }
            else
            {
                ZoomCanvas(evt);
            }
        }

        private void ScaleSpriteObject(int i, float scaleFactor)
        {
            GameObject spriteObj = spriteObjects[i];
            spriteObj.transform.localScale *= scaleFactor;
        }

        private void ZoomCanvas(Event evt)
        {
            Vector2 mousePositionBeforeZoom = (evt.mousePosition + canvasScrollPosition) / zoomFactor;
            zoomFactor -= evt.delta.y * 0.01f;
            zoomFactor = Mathf.Clamp(zoomFactor, 0.1f, 1f);
            Vector2 mousePositionAfterZoom = (evt.mousePosition + canvasScrollPosition) / zoomFactor;
            canvasScrollPosition += (mousePositionAfterZoom - mousePositionBeforeZoom) * zoomFactor;
            evt.Use();
        }

        private static float ScaleImage(ref Vector2 imageSize, ref Vector2 imagePosition, float scaleFactor)
        {
            Vector2 oldCenter = imagePosition + imageSize / 2f;
            imageSize *= scaleFactor;
            imageSize = Vector2.Max(imageSize, new Vector2(10, 10));
            imageSize = Vector2.Min(imageSize, new Vector2(1000, 1000));
            Vector2 newCenter = imagePosition + imageSize / 2f;
            imagePosition += oldCenter - newCenter;
            return scaleFactor;
        }

        private void OnMouseClickDrag_Crop(int index, Rect imageRect, Vector2 transformedMousePosition)
        {
            if (Event.current.button == 0)
            {
                CropRectEdges(transformedMousePosition, imageRect, index);
            }

            ClampCropRect(imageRect);
            Event.current.Use();
        }

        private void CropRectEdges(Vector2 transformedMousePosition, Rect imageRect, int index)
        {
            bool croppingRight = Mathf.Abs(transformedMousePosition.x - cropRect.xMax) < HandleSize;
            bool croppingLeft = Mathf.Abs(transformedMousePosition.x - cropRect.xMin) < HandleSize;
            bool croppingBottom = Mathf.Abs(transformedMousePosition.y - cropRect.yMax) < HandleSize;
            bool croppingTop = Mathf.Abs(transformedMousePosition.y - cropRect.yMin) < HandleSize;

            AdjustCropRectHorizontally(index, croppingRight, croppingLeft);
            AdjustCropRectVertically(index, croppingBottom, croppingTop);
        }

        private void AdjustCropRectHorizontally(int index, bool croppingRight, bool croppingLeft)
        {
            if (croppingRight)
            {
                AdjustCropRectWidth(index, cropRect.width + Event.current.delta.x);
            }
            else if (croppingLeft)
            {
                AdjustCropRectWidth(index, cropRect.width - Event.current.delta.x);
                cropRect.x += Event.current.delta.x;
            }
        }

        private void AdjustCropRectVertically(int index, bool croppingBottom, bool croppingTop)
        {
            if (croppingBottom)
            {
                AdjustCropRectHeight(index, cropRect.height + Event.current.delta.y);
            }
            else if (croppingTop)
            {
                AdjustCropRectHeight(index, cropRect.height - Event.current.delta.y);
                cropRect.y += Event.current.delta.y;
            }
        }

        private void AdjustCropRectWidth(int index, float newWidth)
        {
            int prevWidth = Mathf.RoundToInt(cropRect.width);
            cropRect.width = Mathf.Max(newWidth, 10f);
            DeletePixelsHorizontal(index, prevWidth, Mathf.RoundToInt(cropRect.width));
        }

        private void AdjustCropRectHeight(int index, float newHeight)
        {
            int prevHeight = Mathf.RoundToInt(cropRect.height);
            cropRect.height = Mathf.Max(newHeight, 10f);
            DeletePixelsVertical(index, prevHeight, Mathf.RoundToInt(cropRect.height));
        }

        private void ClampCropRect(Rect imageRect)
        {
            cropRect.width = Mathf.Clamp(cropRect.width, 0f, imageRect.width);
            cropRect.height = Mathf.Clamp(cropRect.height, 0f, imageRect.height);
            cropRect.x = Mathf.Clamp(cropRect.x, imageRect.x, imageRect.xMax - cropRect.width);
            cropRect.y = Mathf.Clamp(cropRect.y, imageRect.y, imageRect.yMax - cropRect.height);
        }

        private void HandleIsCropping()
        {
            if (!isCropping) return;

            DrawCropRect();
        }

        private void DrawCropRect()
        {
            float borderThickness = 1f;
            Color borderColor = Color.white;

            EditorGUI.DrawRect(cropRect, new Color(1, 1, 1, 0.1f));
            DrawCropRectLines(borderThickness, borderColor);
            DrawCropRectBorder(borderThickness, borderColor);
        }

        private void DrawCropRectLines(float borderThickness, Color borderColor)
        {
            float lineHeight = cropRect.height / 3f;
            float columnWidth = cropRect.width / 3f;

            for (int lineIndex = 1; lineIndex <= 2; lineIndex++)
            {
                float lineY = cropRect.y + lineIndex * lineHeight;
                EditorGUI.DrawRect(new Rect(cropRect.x, lineY, cropRect.width, borderThickness), borderColor);
            }

            for (int columnIndex = 1; columnIndex <= 2; columnIndex++)
            {
                float lineX = cropRect.x + columnIndex * columnWidth;
                EditorGUI.DrawRect(new Rect(lineX, cropRect.y, borderThickness, cropRect.height), borderColor);
            }
        }

        private void DrawCropRectBorder(float borderThickness, Color borderColor)
        {
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
                DrawPixelAlignmentLine(i, imageRect);
            }

            if (showHorizontalAlignment && i < uploadedImages.Count - 1 && Mathf.Approximately(imagePositions[i].y, imagePositions[i + 1].y))
            {
                DrawHorizontalAlignmentLine(i, imageRect);
            }
        }

        private void DrawPixelAlignmentLine(int i, Rect imageRect)
        {
            Rect nextImageRect = new Rect(imagePositions[i + 1] * zoomFactor, imageSizes[i + 1] * zoomFactor);
            Rect lineRect = new Rect(imageRect.xMax, imageRect.y, nextImageRect.xMin - imageRect.xMax, imageRect.height);
            if (lineRect.width > 0 && lineRect.height > 0)
            {
                EditorGUI.DrawRect(lineRect, Color.red);
            }
        }

        private void DrawHorizontalAlignmentLine(int i, Rect imageRect)
        {
            Rect nextImageRect = new Rect(imagePositions[i + 1] * zoomFactor, imageSizes[i + 1] * zoomFactor);
            Rect lineRect = new Rect(imageRect.xMin, imageRect.y, Mathf.Max(imageRect.width, nextImageRect.width), 1f);
            if (lineRect.width > 0 && lineRect.height > 0)
            {
                EditorGUI.DrawRect(lineRect, Color.red);
            }
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

            UpdateCroppedImage(index, croppedImage, x, width, y, height);
        }

        private void UpdateCroppedImage(int index, Texture2D croppedImage, int x, int width, int y, int height)
        {
            Vector2 croppedImageCenter = new Vector2(x + width / 2f, y + height / 2f);
            Vector2 originalImageCenter = imagePositions[index] + imageSizes[index] / 2f;
            Vector2 positionOffset = croppedImageCenter - originalImageCenter;

            uploadedImages[index] = croppedImage;
            imagePositions[index] += positionOffset;
            imageSizes[index] = new Vector2(width, height);

            UpdateSpriteObject(index, croppedImage);
        }

        private void UpdateSpriteObject(int index, Texture2D croppedImage)
        {
            Rect spriteRect = new Rect(0, 0, croppedImage.width, croppedImage.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite newSprite = Sprite.Create(croppedImage, spriteRect, pivot);

            GameObject spriteObj = spriteObjects[index];
            SpriteRenderer renderer = spriteObj.GetComponent<SpriteRenderer>();
            renderer.sprite = newSprite;

            spriteObj.transform.position = new Vector3(imagePositions[index].x, imagePositions[index].y, 0);
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

            for (int x = startX; x < endX; x++)
            {
                ClearPixels(image, x, image.height);
            }

            image.Apply();
        }

        private void DeletePixelsVertical(int index, int prevHeight, int newHeight)
        {
            Texture2D image = uploadedImages[index];
            int startY = Mathf.Min(prevHeight, newHeight);
            int endY = Mathf.Max(prevHeight, newHeight);

            for (int y = startY; y < endY; y++)
            {
                ClearPixels(image, image.width, y);
            }

            image.Apply();
        }

        private void ClearPixels(Texture2D image, int x, int y)
        {
            for (int i = 0; i < x; i++)
            {
                image.SetPixel(i, y, Color.clear);
            }
        }

        private void InitializeFields()
        {
            imageStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
            uploadedImages = new List<Texture2D>();
            imagePositions = new List<Vector2>();
            isDraggingList = new List<bool>();
            imageSizes = new List<Vector2>();
            spriteObjects = new List<GameObject>();
        }

        private void ClearFields()
        {
            uploadedImages.Clear();
            imagePositions.Clear();
            isDraggingList.Clear();
            imageSizes.Clear();
            spriteObjects.Clear();
        }
    }
}