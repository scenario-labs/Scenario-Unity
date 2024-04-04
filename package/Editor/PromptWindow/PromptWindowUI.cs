using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public partial class PromptWindowUI
    {
        public static Texture2D imageMask;
    
        internal static Texture2D imageUpload;

        /// <summary>
        /// First dropdown options according to SDXL models
        /// </summary>
        public readonly string[] dropdownOptions =
        {
            "",
            "Character",
            "Landscape",
            "Structure",
            "Pose",
            "Depth",
            "Segmentation",
            "Illusion"
        };

        /// <summary>
        /// Seconds dropdown options according to SD 1.5 models
        /// </summary>
        public readonly string[] dropdownOptionsSD15 =
        {
            "City",
            "Interior",
            "Edges",
            "Scribble",
            "Normal Map",
            "Line Art"
        };

        public string selectedPreset = "";

        /// <summary>
        /// Correspond of the index value selected from the modalities dropdown value
        /// </summary>
        public int selectedOptionIndex = 0;

        /// <summary>
        /// Variable to display in interface on slider element.
        /// </summary>
        public int sliderDisplayedValue = 100;

        /// <summary>
        /// Value from the guidance slider in controlNet options, use to send to generation
        /// </summary>
        public float sliderValue = 0.0f;
    
        internal bool isImageToImage = false;
        internal bool isControlNet = false;
        internal bool isAdvancedSettings = false;
        internal int controlNetModeIndex = 0;
        internal bool isInpainting = false;
        internal string promptinputText = "";
        internal string negativepromptinputText = "";
        internal int widthSliderValue = 1024;
        internal int heightSliderValue = 1024;
        internal float imagesliderValue = 4;
        internal int imagesliderIntValue = 4;
        internal int samplesliderValue = 30;
        internal int influenceSliderValue = 25;
        internal float guidancesliderValue = 7;
        internal string postedModelName = "Choose Model";
        internal string seedinputText = "";
        internal bool isTextToImage = true;
        internal int imageControlTab = 0;

        private int dragFromIndex = -1;
        private int negativeDragFromIndex = -1;
        private string inputText = "";
        private string negativeInputText = "";
        private bool showSettings = true;
        private bool controlNetFoldout = false;
        private Vector2 scrollPosition;
        private Vector2 scrollPosition1 = Vector2.zero;
        private Vector2 scrollPosition2 = Vector2.zero;
        private Vector2 dragStartPos;
        private Vector2 negativeDragStartPos;

        /// <summary>
        /// Reference all dimension values available for SD 1.5 models
        /// </summary>
        private readonly int[] allowed1_5DimensionValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };

        /// <summary>
        /// Reference all dimension values available for SDXL models
        /// </summary>
        private readonly int[] allowedSDXLDimensionValues = { 1024, 1152, 1280, 1376, 1408, 1536, 1824, 2048 };

        internal List<string> tags = new();
        private List<Rect> tagRects = new();
        private List<string> negativeTags = new();
        private List<Rect> negativeTagRects = new();

        private PromptWindow promptWindow;
        private bool shouldAutoGenerateSeed;
        private bool shouldAutoGenerateSeedPrev;

        private PromptPusher pusher = null;

        private ECreationMode selectedMode = ECreationMode.Text_To_Image;

        public string selectedModelName { get; set; } = "Choose Model";

        public PromptWindowUI(PromptWindow promptWindow)
        {
            this.promptWindow = promptWindow;
            if (PromptPusher.Instance != null)
            { 
                pusher = PromptPusher.Instance;
            }
        }

        private void PromptRecv(string str)
        {
            //promptinputText = str;
        }

        private void NegativePromptRecv(string str)
        {
            //negativeInputText = str;
        }

        public void Render(Rect position)
        {
            DrawBackground(position);

            tagRects.Clear();

            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                GUI.FocusControl(null);
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUIStyle.none);

            pusher.modelName = selectedModelName;

            CustomStyle.ButtonSecondary(selectedModelName, 30, Models.ShowWindow);
            CustomStyle.Separator();
            RenderPromptSection();
            CustomStyle.Space();
            RenderNegativePromptSection();
            CustomStyle.Separator();

            bool shouldAutoGenerateSeed = imagesliderValue > 1;
            if (shouldAutoGenerateSeed) { seedinputText = "-1"; }

            CustomStyle.ButtonPrimary("Generate Image", 40, () =>
            {
                pusher.promptInput = SerializeTags(tags);
                pusher.promptNegativeInput = SerializeTags(negativeTags);

                EditorPrefs.SetString("postedModelName", DataCache.instance.SelectedModelId);

                if (shouldAutoGenerateSeed)
                {
                    pusher.GenerateImage(null);
                }
                else
                {
                    string seed = seedinputText;
                    if (seed == "-1") { seed = null; }
                    pusher.GenerateImage(seed);
                }
            });

            CustomStyle.Space();

            RenderImageSettingsSection(shouldAutoGenerateSeed);

            GUI.enabled = true;

            CustomStyle.Space();

            List<string> tabLabels = new List<string>();

            foreach (ECreationMode eMode in Enum.GetValues(typeof(ECreationMode)))
            {
                string eName = eMode.ToString("G").Replace("__", " + ").Replace("_", " ");
                tabLabels.Add(eName);
            }

            selectedMode = (ECreationMode)EditorGUILayout.Popup("Mode: ", imageControlTab, tabLabels.ToArray());
            imageControlTab = (int)selectedMode;
            pusher.ActiveMode(imageControlTab);

            ManageDrawMode();
            
            GUILayout.EndScrollView();
        }

        private static void DrawBackground(Rect position)
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), CustomStyle.GetBackgroundColor());
        }

        /// <summary>
        /// 
        /// </summary>
        private void ManageDrawMode()
        {
            promptWindow.ActiveMode = pusher.GetActiveMode();
            CreationMode activeMode = promptWindow.ActiveMode;

            pusher.imageUpload = imageUpload;
            pusher.maskImage = imageMask;

            switch (activeMode.EMode)
            {
                case ECreationMode.Image_To_Image:
                    DrawHandleImage();

                    GUILayout.BeginHorizontal();

                    if (activeMode.IsControlNet)
                    {
                        if (GUILayout.Button("Create Image"))
                        {
                            ImageEditor.ShowWindow(imageUpload);
                        }

                        if (GUILayout.Button("Add Control"))
                        {
                            CompositionEditor.ShowWindow();
                        }
                    }

                    GUILayout.EndHorizontal();

                    CustomStyle.Space();

                    RenderControlNetFoldout();

                    break;

                case ECreationMode.In_Painting:

                    DrawHandleImage();

                    GUILayout.BeginHorizontal();

                    if (activeMode.IsControlNet)
                    {
                        if (GUILayout.Button("Add Control"))
                        {
                            CompositionEditor.ShowWindow();
                        }
                        if (GUILayout.Button("Add Mask"))
                        {
                            InpaintingEditor.ShowWindow(imageUpload);
                        }
                    }

                    GUILayout.EndHorizontal();

                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void DrawHandleImage()
        {
            CustomStyle.Space();

            Rect dropArea = RenderImageUploadArea();

            if (imageUpload != null)
            {
                dropArea = DrawUploadedImage(dropArea);
            }

            if (imageMask != null)
            {
                GUI.DrawTexture(dropArea, imageMask, ScaleMode.ScaleToFit, true);
            }

            HandleDrag();
        }

        private static void HandleDrag()
        {
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
                        imageUpload = new Texture2D(2, 2);
                        byte[] imageData = File.ReadAllBytes(path);
                        imageUpload.LoadImage(imageData);
                    }

                    currentEvent.Use();
                }
            }
        }

        private Rect DrawUploadedImage(Rect dropArea)
        {
            GUI.DrawTexture(dropArea, imageUpload, ScaleMode.ScaleToFit);

            Rect dropdownButtonRect = new Rect(dropArea.xMax - 90, dropArea.yMax - 25, 30, 30);
            if (imageUpload != null && GUI.Button(dropdownButtonRect, "..."))
            {
                GenericMenu toolsMenu = new GenericMenu();
                toolsMenu.AddItem(new GUIContent("Remove bg"), false, () =>
                {
                    Debug.Log("Remove bg button pressed");
                    BackgroundRemoval.RemoveBackground(imageUpload, bytes =>
                    {
                        imageUpload.LoadImage(bytes);
                    });
                });
            
                toolsMenu.AddItem(new GUIContent("Adjust aspect ratio"), false, () =>
                {
                    if (imageUpload == null) return;

                    int currentWidth = imageUpload.width;
                    int currentHeight = imageUpload.height;

                    int matchingWidth = 0;
                    int matchingHeight = 0;

                    int[] allowedValues = new int[0];
                    if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType))
                    {
                        switch (DataCache.instance.SelectedModelType)
                        {
                            case "sd-xl-composition":
                                allowedValues = allowedSDXLDimensionValues;
                                break;

                            case "sd-xl-lora":
                                allowedValues = allowedSDXLDimensionValues;
                                break;

                            case "sd-xl":
                                allowedValues = allowedSDXLDimensionValues;
                                break;

                            case "sd-1_5":
                                allowedValues = allowed1_5DimensionValues;
                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        allowedValues = allowed1_5DimensionValues;
                    }

                    matchingWidth = GetMatchingValue(currentWidth, allowedValues);
                    matchingHeight = GetMatchingValue(currentHeight, allowedValues);

                    widthSliderValue = matchingWidth != -1 ? matchingWidth : currentWidth;
                    heightSliderValue = matchingHeight != -1 ? matchingHeight : currentHeight;

                    selectedOptionIndex = NearestValueIndex(widthSliderValue, allowedValues);
                });
            
                toolsMenu.DropDown(dropdownButtonRect);
            }

            Rect clearImageButtonRect = new Rect(dropArea.xMax - 50, dropArea.yMax - 25, 30, 30);
            if (imageUpload != null && GUI.Button(clearImageButtonRect, "X"))
            {
                imageUpload = null;
                imageMask = null;
            }

            return dropArea;
        }

        private static Rect RenderImageUploadArea()
        {
            CustomStyle.Label("Upload Image");

            Rect dropArea = GUILayoutUtility.GetRect(0f, 150f, GUILayout.ExpandWidth(true));
            if (imageUpload == null)
            {
                GUI.Box(dropArea, "Drag & Drop an image here");

                Rect buttonRect = new Rect(dropArea.center.x - 50f, dropArea.center.y - 15f, 100f, 30f);
                if (GUI.Button(buttonRect, "Choose Image"))
                {
                    string imagePath = EditorUtility.OpenFilePanel("Choose Image", "", "png,jpg,jpeg");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imageUpload = new Texture2D(2, 2);
                        byte[] imageData = File.ReadAllBytes(imagePath);
                        imageUpload.LoadImage(imageData);
                    }
                }
            }

            return dropArea;
        }
    
        private string SerializeTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "";

            StringBuilder serializedTags = new StringBuilder(tags[0]);
            for (int i = 1; i < tags.Count; i++)
            {
                serializedTags.Append(", ");
                serializedTags.Append(tags[i]);
            }
            return serializedTags.ToString();
        }

        private string TruncateTag(string tag)
        {
            if (tag.Length <= 30)
            {
                return tag;
            }
        
            int lastSpaceIndex = tag.LastIndexOf(' ', 30);
            return lastSpaceIndex == -1 ? tag[..30] : tag[..lastSpaceIndex];
        }

        private int NearestValueIndex(int currentValue, int[] allowedValues)
        {
            int nearestIndex = 0;
            int minDifference = int.MaxValue;

            for (int i = 0; i < allowedValues.Length; i++)
            {
                int difference = Mathf.Abs(currentValue - allowedValues[i]);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private int GetNewIndex(Vector2 currentPos)
        {
            for (int i = 0; i < tagRects.Count; i++)
            {
                if (tagRects[i].Contains(currentPos))
                {
                    return i;
                }
            }

            return -1;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private int GetMatchingValue(int targetValue, int[] values)
        {
            foreach (int value in values)
            {
                if (value == targetValue)
                {
                    return value;
                }
            }

            return -1;
        }
    }
}
