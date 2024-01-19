using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class ImageEditorUI
    {
        private Texture2D uploadedImage;
        private Texture2D brushCursor;
        private Color selectedColor = Color.white;
        private Texture2D canvasImage;
        private Texture2D transparentImage;
        private int selectedBrushSize;
        private List<Texture2D> canvasHistory;
        private List<Texture2D> redoHistory;
        private int canvasHistoryIndex;
        private bool newStroke = true;
        public string uploadedImagePath;

        internal ImageEditor imageEditor;
        private float selectedOpacity = 1.0f;

        private enum DrawingMode { Draw, Erase,/* Fill, Picker,*/ Expand, Crop }
        private DrawingMode currentDrawingMode = DrawingMode.Draw;
        private int[] allowedSizes = { 256, 384, 512, 570, 640, 704, 768, 912, 1024 };

        private struct ToolButton
        {
            public string Text;
            public string Tooltip;
            public DrawingMode Mode;
            public Action OnClick;
        }

        private ToolButton[] toolButtons;

        private struct ActionButton
        {
            public string Text;
            public string Tooltip;
            public Action OnClick;
        }

        private ActionButton[] actionButtons;

        public ImageEditorUI(ImageEditor imageEditor)
        {
            this.imageEditor = imageEditor;

            transparentImage = new Texture2D(1, 1);
            selectedBrushSize = 6;
            canvasHistory = new List<Texture2D>();
            canvasHistoryIndex = -1;
            redoHistory = new List<Texture2D>();

            toolButtons = new ToolButton[]
            {
                new ToolButton { Text = "✎", Tooltip = "Draw tool", Mode = DrawingMode.Draw },
                new ToolButton { Text = "✐", Tooltip = "Erase tool", Mode = DrawingMode.Erase },
                /*new ToolButton { Text = "◉", Tooltip = "Fill tool", Mode = DrawingMode.Fill },
            new ToolButton { Text = "✚", Tooltip = "Picker tool", Mode = DrawingMode.Picker },*/
                new ToolButton { Text = "[]", Tooltip = "To expand the image", Mode = DrawingMode.Expand },
                new ToolButton { Text = "-", Tooltip = "To crop the image", Mode = DrawingMode.Crop }
            };

            actionButtons = new ActionButton[]
            {
                /*new ActionButton { Text = "Load mask from file", Tooltip = "Load mask from file", OnClick = LoadMaskFromFile },
            new ActionButton { Text = "Save mask to file", Tooltip = "Save mask to file", OnClick = SaveMaskToFile },
            new ActionButton { Text = "Fill all", Tooltip = "Fill all", OnClick = FillAll },*/
                new ActionButton { Text = "Create Image", Tooltip = "Create a new image", OnClick = CreateImage },
                new ActionButton { Text = "Save Image", Tooltip = "Save image for later use", OnClick = SaveImage },
                new ActionButton { Text = "Clear", Tooltip = "Clear", OnClick = Clear },
                new ActionButton { Text = "Undo", Tooltip = "Undo", OnClick = UndoCanvas },
                new ActionButton { Text = "Redo", Tooltip = "Redo", OnClick = RedoCanvas },
                new ActionButton { Text = "Cancel", Tooltip = "Cancel", OnClick = Cancel },
                new ActionButton { Text = "Use", Tooltip = "Use", OnClick = Use }
            };
        }

        internal void SetImage(Texture2D imageData)
        {
            uploadedImage = imageData;
            uploadedImage.alphaIsTransparency = true;

            canvasImage = new Texture2D(uploadedImage.width, uploadedImage.height, TextureFormat.RGBA32, false, true);
            canvasImage.SetPixels(Enumerable.Repeat(Color.clear, canvasImage.width * canvasImage.height).ToArray());
            canvasImage.Apply();

            canvasHistory.Clear();
            canvasHistoryIndex = -1;
            AddToCanvasHistory();
        }

        public void DrawUI(Rect position)
        {
            DrawBackground(position);

            EditorGUILayout.BeginHorizontal();

            position = DrawLeftSection(position);

            // Flexible space between left and middle section
            GUILayout.FlexibleSpace();

            DrawMiddleSection(position);

            GUILayout.FlexibleSpace();

            DrawRightSection(position);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        private void DrawRightSection(Rect position)
        {
            // Right Section
            float rightSectionWidth = position.width * 0.1f;
            EditorGUILayout.BeginVertical(GUILayout.Width(rightSectionWidth));
            CustomStyle.Space(18);
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            for (int i = 0; i < actionButtons.Length; i++)
            {
                ActionButton button = actionButtons[i];
                if (GUILayout.Button(new GUIContent(button.Text, button.Tooltip), GUILayout.Width(140), GUILayout.Height(40)))
                {
                    button.OnClick?.Invoke();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMiddleSection(Rect position)
        {
            // Middle Section
            float middleSectionWidth = position.width * 0.4f;
            EditorGUILayout.BeginVertical(GUILayout.Width(middleSectionWidth));
            {
                CustomStyle.Space(18);

                if (uploadedImage != null)
                {
                    switch (currentDrawingMode)
                    {
                        case DrawingMode.Draw:
                        case DrawingMode.Erase:
                            DrawCanvasWithImage();
                            break;
                        case DrawingMode.Expand:
                            DrawExpandSection(position);
                            break;
                        case DrawingMode.Crop:
                            DrawCropSection(position);
                            break;
                    }
                }
                else
                {
                    DrawUploadSection();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawUploadSection()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                {
                    HandleImageUpload();
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private void HandleImageUpload()
        {
            Rect dropArea = GUILayoutUtility.GetRect(512f, 256f, GUILayout.ExpandWidth(false));
            GUI.Box(dropArea, "Drop image here or");

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (currentEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        string path = DragAndDrop.paths[0];
                        if (System.IO.File.Exists(path) && (System.IO.Path.GetExtension(path).ToLower() == ".png" ||
                                                            System.IO.Path.GetExtension(path).ToLower() == ".jpg" ||
                                                            System.IO.Path.GetExtension(path).ToLower() == ".jpeg"))
                        {
                            Texture2D tex = new Texture2D(2, 2);
                            byte[] imageData = File.ReadAllBytes(path);
                            tex.LoadImage(imageData);
                            SetImage(tex);
                        }
                    }

                    currentEvent.Use();
                }
            }

            Rect buttonRect = new Rect(dropArea.center.x - 75f, dropArea.center.y - 20f, 150f, 40f);
            if (GUI.Button(buttonRect, "Upload"))
            {
                uploadedImagePath = EditorUtility.OpenFilePanel("Upload image", "", "png,jpeg,jpg");
                if (!string.IsNullOrEmpty(uploadedImagePath))
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes(uploadedImagePath));
                    SetImage(tex);
                }
            }
        }

        private void DrawCropSection(Rect position)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CustomStyle.Space();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                {
                    float centerX = position.width * 0.5f;
                    float centerY = position.height / 2f;
                    float imageWidth = uploadedImage.width;
                    float imageHeight = uploadedImage.height;
                    float buttonSize = 50f;

                    var rect = new Rect(centerX - imageWidth / 2, centerY - imageHeight / 2, imageWidth, imageHeight);
                    GUI.DrawTexture(rect, uploadedImage);

                    rect = new Rect(centerX - imageWidth / 2 - buttonSize - 5, centerY - buttonSize / 2, buttonSize,
                        buttonSize);
                    if (GUI.Button(rect, "-"))
                    {
                        int newWidth = FindPreviousSize((int)imageWidth);
                        uploadedImage = ResizeImage(uploadedImage, newWidth, (int)imageHeight, addLeft: true);
                    }

                    // Right button
                    rect = new Rect(centerX + imageWidth / 2 + 5, centerY - buttonSize / 2, buttonSize, buttonSize);
                    if (GUI.Button(rect, "-"))
                    {
                        int newWidth = FindPreviousSize((int)imageWidth);
                        uploadedImage = ResizeImage(uploadedImage, newWidth, (int)imageHeight);
                    }

                    // Top button
                    rect = new Rect(centerX - buttonSize / 2, centerY - imageHeight / 2 - buttonSize - 5, buttonSize,
                        buttonSize);
                    if (GUI.Button(rect, "-"))
                    {
                        int newHeight = FindPreviousSize((int)imageHeight);
                        uploadedImage = ResizeImage(uploadedImage, (int)imageWidth, newHeight);
                    }

                    // Bottom button
                    rect = new Rect(centerX - buttonSize / 2, centerY + imageHeight / 2 + 5, buttonSize, buttonSize);
                    if (GUI.Button(rect, "-"))
                    {
                        int newHeight = FindPreviousSize((int)imageHeight);
                        uploadedImage = ResizeImage(uploadedImage, (int)imageWidth, newHeight, addBottom: true);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawExpandSection(Rect position)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CustomStyle.Space();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                {
                    float centerX = position.width * 0.5f;
                    float centerY = position.height / 2f;
                    float imageWidth = uploadedImage.width;
                    float imageHeight = uploadedImage.height;
                    float buttonSize = 50f;

                    var rect = new Rect(centerX - imageWidth / 2, centerY - imageHeight / 2, imageWidth, imageHeight);
                    GUI.DrawTexture(rect, uploadedImage);

                    rect = new Rect(centerX - imageWidth / 2 - buttonSize - 5, centerY - buttonSize / 2, buttonSize,
                        buttonSize);
                    if (GUI.Button(rect, "+"))
                    {
                        int newWidth = FindNextSize((int)imageWidth);
                        uploadedImage = ResizeImage(uploadedImage, newWidth, (int)imageHeight, addLeft: true);
                    }

                    // Right button
                    rect = new Rect(centerX + imageWidth / 2 + 5, centerY - buttonSize / 2, buttonSize, buttonSize);
                    if (GUI.Button(rect, "+"))
                    {
                        int newWidth = FindNextSize((int)imageWidth);
                        uploadedImage = ResizeImage(uploadedImage, newWidth, (int)imageHeight);
                    }

                    // Top button
                    rect = new Rect(centerX - buttonSize / 2, centerY - imageHeight / 2 - buttonSize - 5, buttonSize,
                        buttonSize);
                    if (GUI.Button(rect, "+"))
                    {
                        int newHeight = FindNextSize((int)imageHeight);
                        uploadedImage = ResizeImage(uploadedImage, (int)imageWidth, newHeight);
                    }

                    // Bottom button
                    rect = new Rect(centerX - buttonSize / 2, centerY + imageHeight / 2 + 5, buttonSize, buttonSize);
                    if (GUI.Button(rect, "+"))
                    {
                        int newHeight = FindNextSize((int)imageHeight);
                        uploadedImage = ResizeImage(uploadedImage, (int)imageWidth, newHeight, addBottom: true);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCanvasWithImage()
        {
            EditorGUILayout.BeginHorizontal();
            {
                CustomStyle.Space();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                {
                    float maxSize = 1024f;
                    float aspectRatio = (float)uploadedImage.width / (float)uploadedImage.height;
                    float width = Mathf.Min(uploadedImage.width, maxSize);
                    float height = width / aspectRatio;

                    Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false),
                        GUILayout.ExpandHeight(false));

                    GUI.DrawTexture(rect, uploadedImage, ScaleMode.ScaleToFit);

                    if (canvasImage == null || canvasImage.width != uploadedImage.width ||
                        canvasImage.height != uploadedImage.height)
                    {
                        int canvasWidth = Mathf.Min(uploadedImage.width, 1024);
                        int canvasHeight = Mathf.Min(uploadedImage.height, 1024);
                        canvasImage = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false, true);
                        canvasImage.SetPixels(Enumerable.Repeat(Color.clear, canvasImage.width * canvasImage.height)
                            .ToArray());
                        canvasImage.Apply();
                    }

                    GUI.DrawTexture(rect, canvasImage, ScaleMode.ScaleToFit);

                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        if (brushCursor == null || brushCursor.width != selectedBrushSize)
                        {
                            brushCursor = MakeCircularTex(selectedBrushSize, selectedColor);
                        }

                        EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.CustomCursor);
                        Cursor.SetCursor(brushCursor, new Vector2(brushCursor.width / 2, brushCursor.height / 2),
                            CursorMode.Auto);

                        if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                        {
                            Vector2 localMousePosition = Event.current.mousePosition - new Vector2(rect.x, rect.y);
                            Vector2 textureCoords = new Vector2(localMousePosition.x / rect.width,
                                localMousePosition.y / rect.height);

                            int x = (int)(textureCoords.x * uploadedImage.width);
                            int y = (int)((1 - textureCoords.y) * uploadedImage.height);

                            if (currentDrawingMode == DrawingMode.Draw)
                            {
                                DrawOnTexture(canvasImage, new Vector2(x, y), selectedBrushSize, selectedColor,
                                    selectedOpacity);
                            }
                            else if (currentDrawingMode == DrawingMode.Erase)
                            {
                                DrawOnTexture(canvasImage, new Vector2(x, y), selectedBrushSize, new Color(0, 0, 0, 0),
                                    selectedOpacity);
                            }

                            Event.current.Use();
                        }

                        else if (Event.current.type == EventType.MouseUp)
                        {
                            newStroke = true;
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private Rect DrawLeftSection(Rect position)
        {
            // Left Section
            float leftSectionWidth = position.width * 0.1f;
            EditorGUILayout.BeginVertical(GUILayout.Width(leftSectionWidth));
            {
                CustomStyle.Space(18);
                GUILayout.Label("Tools", EditorStyles.boldLabel);

                for (int i = 0; i < toolButtons.Length; i++)
                {
                    ToolButton button = toolButtons[i];
                    if (i % 4 == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                    }

                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.fontSize = 25;
                    if (GUILayout.Button(new GUIContent(button.Text, button.Tooltip), buttonStyle, GUILayout.Width(45),
                            GUILayout.Height(45)))
                    {
                        currentDrawingMode = button.Mode;
                        button.OnClick?.Invoke();
                    }

                    if (i % 4 == 3 || i == toolButtons.Length - 1)
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }

                CustomStyle.Space(10);

                GUILayout.Label("Color", EditorStyles.boldLabel);

                int colorButtonSize = 40;
                int numColumns = 4;

                //Save the original color of the GUI background color
                Color originalColor = GUI.backgroundColor;

                for (int i = 0; i < 35; i++)
                {
                    if (i % numColumns == 0) EditorGUILayout.BeginHorizontal();

                    GUIStyle colorButtonStyle = new GUIStyle(GUI.skin.button);
                    Color color = Color.HSVToRGB((float)i / 35, 1, 1);
                    GUI.backgroundColor = color; //swap the background color to the color we want. so every new GUI component will get this color

                    if (GUILayout.Button("", colorButtonStyle, GUILayout.Width(colorButtonSize),
                            GUILayout.Height(colorButtonSize)))
                    {
                        selectedColor = color;
                    }

                    if (i % numColumns == numColumns - 1 || i == 34) EditorGUILayout.EndHorizontal();
                }

                //GUI will now draw with default color
                GUI.backgroundColor = originalColor;

                CustomStyle.Space(10);

                int[] brushSizes = new[] { 6, 12, 16, 24, 30, 40, 48, 64 };

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Brush Size", EditorStyles.boldLabel, GUILayout.Width(80));
                    GUILayout.Label(selectedBrushSize.ToString(), GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    selectedBrushSize = (int)GUILayout.HorizontalSlider(selectedBrushSize, brushSizes[0],
                        brushSizes[brushSizes.Length - 1], GUILayout.Width(200));
                }
                GUILayout.EndHorizontal();

                CustomStyle.Space(20);

                float[] opacities = new float[] { 0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
                int selectedOpacityIndex = 5;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Opacity", EditorStyles.boldLabel);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    for (int i = 0; i < opacities.Length; i++)
                    {
                        float opacity = opacities[i];
                        GUIStyle opacityButtonStyle = new GUIStyle(GUI.skin.button);
                        opacityButtonStyle.normal.background = MakeOpacityTex(10, 10, opacity);

                        if (GUILayout.Button("", opacityButtonStyle, GUILayout.Width(14), GUILayout.Height(14)))
                        {
                            selectedOpacityIndex = i;
                            selectedOpacity = opacities[selectedOpacityIndex];
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            return position;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeOpacityTex(int width, int height, float opacity)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = new Color(1, 1, 1, opacity);
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeCircularTex(int diameter, Color col)
        {
            if (diameter <= 0) return null;

            int radius = diameter / 2;
            Color[] pix = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int dx = x - radius;
                    int dy = y - radius;
                    if (dx * dx + dy * dy < radius * radius)
                    {
                        pix[y * diameter + x] = col;
                    }
                    else
                    {
                        pix[y * diameter + x] = Color.clear;
                    }
                }
            }
            Texture2D result = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false, true);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeCursorTex(int diameter, Color col)
        {
            if (diameter <= 0) return null;

            int radius = diameter / 2;
            Color[] pix = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int dx = x - radius;
                    int dy = y - radius;
                    if (dx * dx + dy * dy < radius * radius)
                    {
                        pix[y * diameter + x] = col;
                    }
                    else
                    {
                        pix[y * diameter + x] = Color.clear;
                    }
                }
            }
            Texture2D result = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false, true);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawOnTexture(Texture2D tex, Vector2 position, int brushSize, Color color, float opacity)
        {
            if (newStroke && (currentDrawingMode == DrawingMode.Draw || currentDrawingMode == DrawingMode.Erase))
            {
                // Clear the redo history
                if (canvasHistoryIndex < canvasHistory.Count - 1)
                {
                    canvasHistory.RemoveRange(canvasHistoryIndex + 1, canvasHistory.Count - canvasHistoryIndex - 1);
                }

                AddToCanvasHistory();
                newStroke = false;
            }

            int xStart = Mathf.Clamp((int)position.x - brushSize / 2, 0, tex.width);
            int xEnd = Mathf.Clamp((int)position.x + brushSize / 2, 0, tex.width);
            int yStart = Mathf.Clamp((int)position.y - brushSize / 2, 0, tex.height);
            int yEnd = Mathf.Clamp((int)position.y + brushSize / 2, 0, tex.height);

            Color colorWithOpacity = new Color(color.r, color.g, color.b, opacity);

            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    if (Vector2.Distance(new Vector2(x, y), position) <= brushSize / 2)
                    {
                        Color currentColor = tex.GetPixel(x, y);
                        Color blendedColor;

                        if (currentDrawingMode == DrawingMode.Erase)
                        {
                            blendedColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0);
                        }
                        else
                        {
                            blendedColor = Color.Lerp(currentColor, colorWithOpacity, colorWithOpacity.a);
                        }

                        tex.SetPixel(x, y, blendedColor);
                    }
                }
            }

            tex.Apply();
        }

        private int FindNextSize(int currentSize)
        {
            foreach (int size in allowedSizes)
            {
                if (size > currentSize)
                    return size;
            }
            return currentSize;
        }

        private int FindPreviousSize(int currentSize)
        {
            for (int i = allowedSizes.Length - 1; i >= 0; i--)
            {
                if (allowedSizes[i] < currentSize)
                    return allowedSizes[i];
            }
            return currentSize;
        }

        private Texture2D ResizeImage(Texture2D original, int newWidth, int newHeight, bool addBottom = false, bool addLeft = false)
        {
            Texture2D resizedImage = new Texture2D(newWidth, newHeight);
            Color[] originalPixels = original.GetPixels();
            Color[] newPixels = new Color[newWidth * newHeight];
            int xOffset = addLeft ? newWidth - original.width : 0;
            int yOffset = addBottom ? newHeight - original.height : 0;
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    if (x >= xOffset && x < xOffset + original.width && y >= yOffset && y < yOffset + original.height)
                    {
                        newPixels[y * newWidth + x] = originalPixels[(y - yOffset) * original.width + (x - xOffset)];
                    }
                    else
                    {
                        newPixels[y * newWidth + x] = Color.white;
                    }
                }
            }
            resizedImage.SetPixels(newPixels);
            resizedImage.Apply();
            return resizedImage;
        }

        private void CreateTransparentImage(int width, int height)
        {
            transparentImage = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            transparentImage.SetPixels32(pixels);
            transparentImage.Apply();
        }

        private void AddToCanvasHistory()
        {
            canvasHistory.RemoveRange(canvasHistoryIndex + 1, canvasHistory.Count - canvasHistoryIndex - 1);

            Texture2D newHistoryImage = new Texture2D(canvasImage.width, canvasImage.height, TextureFormat.RGBA32, false, true);
            newHistoryImage.SetPixels(canvasImage.GetPixels());
            newHistoryImage.Apply();
            canvasHistory.Add(newHistoryImage);
            canvasHistoryIndex++;

            const int maxHistorySize = 10;
            if (canvasHistory.Count > maxHistorySize)
            {
                canvasHistory.RemoveAt(0);
                canvasHistoryIndex--;
            }
        }

        private void UndoCanvas()
        {
            if (canvasHistoryIndex <= 0) return;

            // Save the current state to the redo stack
            Texture2D redoImage = new Texture2D(canvasImage.width, canvasImage.height, TextureFormat.RGBA32, false, true);
            redoImage.SetPixels(canvasImage.GetPixels());
            redoImage.Apply();
            redoHistory.Add(redoImage);

            // Revert to the previous state
            canvasHistoryIndex--;
            canvasImage.SetPixels(canvasHistory[canvasHistoryIndex].GetPixels());
            canvasImage.Apply();
        }

        private void RedoCanvas()
        {
            if (redoHistory.Count <= 0) return;

            // Save the current state to the undo stack
            Texture2D undoImage = new Texture2D(canvasImage.width, canvasImage.height, TextureFormat.RGBA32, false, true);
            undoImage.SetPixels(canvasImage.GetPixels());
            undoImage.Apply();
            canvasHistory.Add(undoImage);
            canvasHistoryIndex++;

            // Update to the next state
            Texture2D redoImage = redoHistory[redoHistory.Count - 1];
            redoHistory.RemoveAt(redoHistory.Count - 1);
            canvasImage.SetPixels(redoImage.GetPixels());
            canvasImage.Apply();
        }

        // Add the corresponding methods for the action buttons
        private void CreateImage()
        {
            int width = 512;
            int height = 512;
            uploadedImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            uploadedImage.SetPixels(pixels);
            uploadedImage.Apply();
        }

        private void SaveImage()
        {
            string saveImagePath = EditorUtility.SaveFilePanel("Save image", "", "", "png,jpeg,jpg");
            if (!string.IsNullOrEmpty(saveImagePath))
            {
                CommonUtils.SaveTextureAsPNGAtPath(canvasImage, saveImagePath);
            }
        }

        /*private void FillAll()
    {
        // Add logic to fill all
    }*/

        private void Clear()
        {
            uploadedImage = null;
            canvasImage = null;
            canvasHistory.Clear();
            canvasHistoryIndex = -1;
        }

        private void Cancel()
        {
            imageEditor.Close();
        }

        private void Use()
        {
            if (uploadedImage == null || canvasImage == null)
            {
                Debug.Log("MUST HAVE AN UPLOADED IMAGE AND A CANVAS IMAGE FOR MASKING");
                return;
            }

            // Create a new texture that combines the uploaded image and the canvas image
            Texture2D combinedImage = new Texture2D(uploadedImage.width, uploadedImage.height, TextureFormat.RGBA32, false);
            for (int y = 0; y < combinedImage.height; y++)
            {
                for (int x = 0; x < combinedImage.width; x++)
                {
                    Color backgroundColor = uploadedImage.GetPixel(x, y);
                    Color foregroundColor = canvasImage.GetPixel(x, y);
                    Color combinedColor = Color.Lerp(backgroundColor, foregroundColor, foregroundColor.a);
                    combinedImage.SetPixel(x, y, combinedColor);
                }
            }
            combinedImage.Apply();

            PromptWindowUI.imageUpload = combinedImage;
            imageEditor.Close();
        }

    }
}