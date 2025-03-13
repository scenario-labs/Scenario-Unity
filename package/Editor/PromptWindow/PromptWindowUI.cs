using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public partial class PromptWindowUI
    {
        #region Public Fields

        public string selectedModelName { get; set; } = "Choose Model";

        public int WidthSliderValue { get { return widthSliderValue; } set { widthSliderValue = value; } }
        public int HeightSliderValue { get { return heightSliderValue; } set { heightSliderValue = value; } }

        public static Texture2D imageMask;

        public readonly string[] dropdownOptionsflux =
        {
            "",
            "Structure",
            "Pose",
            "Depth",
            "Tile",
            "Blur",
            "Gray",
            "low-quality"
        };

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

        public static bool isSizeSliderInitialized = false;

        /// <summary>
        /// Reference all dimension values available for SD 1.5 models
        /// </summary>
        public readonly Vector2Int[] allowed1_5DimensionValues = {
            new Vector2Int(728, 312),  // 21:9
            new Vector2Int(672, 384),  // 16:9
            new Vector2Int(608, 416),  // 3:2
            new Vector2Int(552, 416),  // 4:3
            new Vector2Int(544, 448),  // 5:4
            new Vector2Int(512, 512),  // 1:1
            new Vector2Int(448, 544),  // 4:5
            new Vector2Int(416, 552),  // 3:4
            new Vector2Int(416, 608),  // 2:3
            new Vector2Int(384, 672),  // 9:16
            new Vector2Int(312, 728)   // 9:21
        };

        /// <summary>
        /// Reference all dimension values available for SDXL models
        /// </summary>
        public readonly Vector2Int[] allowedSDXLDimensionValues = {
            new Vector2Int(1456, 624), // 21:9
            new Vector2Int(1344, 768), // 16:9
            new Vector2Int(1216, 832), // 3:2
            new Vector2Int(1104, 832), // 4:3
            new Vector2Int(1088, 896), // 5:4
            new Vector2Int(1024, 1024),// 1:1
            new Vector2Int(896, 1088), // 4:5
            new Vector2Int(832, 1104), // 3:4
            new Vector2Int(832, 1216), // 2:3
            new Vector2Int(768, 1344), // 9:16
            new Vector2Int(624, 1456)  // 9:21
        };

        /// <summary>
        /// Reference all dimension values available for FLUX models
        /// </summary>
        public readonly Vector2Int[] allowedFLUXPRODimensionValues = {
            new Vector2Int(3136, 1344), // 21:9
            new Vector2Int(2752, 1536), // 16:9
            new Vector2Int(2496, 1664), // 3:2
            new Vector2Int(2368, 1792), // 4:3
            new Vector2Int(2304, 1856), // 5:4
            new Vector2Int(2048, 2048),// 1:1
            new Vector2Int(1856, 2304), // 4:5
            new Vector2Int(1792, 2368), // 3:4
            new Vector2Int(1664, 2496), // 2:3
            new Vector2Int(1536, 2752), // 9:16
            new Vector2Int(1344, 3136)  // 9:21
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

        /// <summary>
        /// Specific additional modality value of the slider
        /// </summary>
        public float additionalModalitySliderValue = 50.0f;

        /// <summary>
        /// Specific additional modality value
        /// </summary>
        public float additionalModalityValue = 0.5f;

        #endregion

        #region Private Fields

        internal static Texture2D imageUpload;
        internal static Texture2D additionalImageUpload;

        internal bool isImageToImage = false;
        internal bool isControlNet = false;
        internal bool isAdvancedSettings = false;
        internal int controlNetModeIndex = 0;
        internal bool isInpainting = false;
        internal string promptinputText = "";
        internal string negativepromptinputText = "";
        internal int widthSliderValue = 1024;
        internal int heightSliderValue = 1024;
        /// <summary>
        /// Default slide value for the size slider
        /// </summary>
        internal float sizeSliderValue = 7;
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
        private Vector2 scrollPosition = new Vector2();
        private Vector2 dragStartPos = new Vector2();
        private Vector2 negativeDragStartPos = new Vector2();

        internal List<string> tags = new();
        private List<Rect> tagRects = new();
        private List<string> negativeTags = new();
        private List<Rect> negativeTagRects = new();

        private PromptWindow promptWindow;

        private bool shouldAutoGenerateSeed;
        private bool shouldAutoGenerateSeedPrev;

        /// <summary>
        /// Get a reference of the prompt pusher
        /// </summary>
        private PromptPusher promptPusher = null;

        /// <summary>
        /// Get a reference to necessary image display editor
        /// </summary>
        private DropImageView dropImageView = null;
        
        /// <summary>
        /// Get a reference to additional Image display editor
        /// </summary>
        private DropImageView dropAdditionalImageView = null;

        /// <summary>
        /// Default selected creation mode.
        /// </summary>
        private ECreationMode selectedMode = ECreationMode.Text_To_Image;

        #endregion

        #region Public Methods

        public PromptWindowUI(PromptWindow promptWindow)
        {
            this.promptWindow = promptWindow;

            if (PromptPusher.Instance != null)
            {
                promptPusher = PromptPusher.Instance;
            }

            if (dropImageView == null)
            {
                dropImageView = new DropImageView(this);
            }

            if (dropAdditionalImageView == null)
            {
                dropAdditionalImageView = new DropImageView(this);
            }
        }

        public int NearestValueIndex(int targetValue, Vector2Int[] values) // Updated for Vector2Int[]
        {
            int nearestIndex = -1;
            float minDifference = float.MaxValue;

            for (int i = 0; i < values.Length; i++)
            {
                float difference = Mathf.Abs(values[i].x - targetValue); // Compare with Vector2Int.x (width)
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestIndex = i;
                }
            }
            return nearestIndex;
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

            promptPusher.modelName = selectedModelName;
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            { 
                CustomStyle.ButtonSecondary(selectedModelName, 30, Models.ShowWindow);
                CustomStyle.Separator();
                RenderPromptSection();
                if (!DataCache.instance.SelectedModelType.StartsWith("flux", StringComparison.OrdinalIgnoreCase))
                {
                    CustomStyle.Space();
                    RenderNegativePromptSection();
                }
                CustomStyle.Separator();
            

                bool shouldAutoGenerateSeed = imagesliderValue > 1;
                if (shouldAutoGenerateSeed) { seedinputText = "-1"; }

                CustomStyle.ButtonPrimary("Generate Image", 40, 0, () =>
                {
                    promptPusher.promptInput = SerializeTags(tags);
                    promptPusher.promptNegativeInput = SerializeTags(negativeTags);

                    EditorPrefs.SetString("postedModelName", DataCache.instance.SelectedModelId);

                    if (shouldAutoGenerateSeed)
                    {
                        promptPusher.GenerateImage(null);
                    }
                    else
                    {
                        string seed = seedinputText;
                        if (seed == "-1") { seed = null; }
                        promptPusher.GenerateImage(seed);
                    }
                });

                CustomStyle.Space();

                RenderImageSettingsSection(shouldAutoGenerateSeed);

                GUI.enabled = true;

                CustomStyle.Space();

                List<string> tabLabels = new List<string>();
                List<ECreationMode> availableModes = new List<ECreationMode>();

                foreach (ECreationMode eMode in Enum.GetValues(typeof(ECreationMode)))
                {
                    if (!string.IsNullOrEmpty(DataCache.instance.SelectedModelType) &&
                        DataCache.instance.SelectedModelType.StartsWith("flux", StringComparison.OrdinalIgnoreCase))
                    {
                        if (DataCache.instance.SelectedModelType.Equals("flux1.1-pro", StringComparison.OrdinalIgnoreCase) ||
                                DataCache.instance.SelectedModelType.Equals("flux.1-pro", StringComparison.OrdinalIgnoreCase))
                        {
                            if (eMode == ECreationMode.Text_To_Image)
                            {
                                availableModes.Add(eMode);
                                string eName = eMode.ToString("G").Replace("__", " + ").Replace("_", " ");
                                tabLabels.Add(eName);
                            }
                            continue; 
                        }
                        else if (DataCache.instance.SelectedModelType.Contains("flux.1.1-pro-ultra", StringComparison.OrdinalIgnoreCase))
                        {
                            if (eMode == ECreationMode.Text_To_Image || eMode == ECreationMode.IP_Adapter)
                            {
                                availableModes.Add(eMode);
                                string eName = eMode.ToString("G").Replace("__", " + ").Replace("_", " ");
                                tabLabels.Add(eName);
                            }
                            continue; 
                        }
                        else
                        {
                            availableModes.Add(eMode);
                            string eName = eMode.ToString("G").Replace("__", " + ").Replace("_", " ");
                            tabLabels.Add(eName);
                        }
                    }
                    else
                    {
                        availableModes.Add(eMode);
                        string eName = eMode.ToString("G").Replace("__", " + ").Replace("_", " ");
                        tabLabels.Add(eName);
                    }
                }


                int selectedIndex = availableModes.IndexOf(selectedMode);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.inspectorFullWidthMargins);
                {
                    GUILayout.Label("Mode: ", GUILayout.Width(labelWidth));
                    selectedIndex = EditorGUILayout.Popup(selectedIndex, tabLabels.ToArray(), GUILayout.Width(sliderWidth));
                    selectedMode = availableModes[selectedIndex];
                }
                EditorGUILayout.EndHorizontal();

                imageControlTab = (int)selectedMode;
                promptPusher.ActiveMode(imageControlTab);


                ManageDrawMode();
            }
            EditorGUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Set the image directly inside the drop content
        /// </summary>
        /// <param name="_newImage"></param>
        public void SetDropImage(Texture2D _newImage)
        { 
            dropImageView.ImageUpload = _newImage;
            promptPusher.imageUpload = dropImageView.ImageUpload;
        }

        /// <summary>
        /// Set the image directly inside the additional drop content
        /// </summary>
        /// <param name="_newImage"></param>
        public void SetAdditionalDropImage(Texture2D _newImage)
        {
            dropAdditionalImageView.ImageUpload = _newImage;
            promptPusher.imageUpload = dropAdditionalImageView.ImageUpload;
        }

        /// <summary>
        /// Set the image directly inside the drop mask content
        /// </summary>
        /// <param name="_newImage"></param>
        public void SetDropMaskImage(Texture2D _newImage)
        {
            dropImageView.ImageMask = _newImage;
            promptPusher.maskImage = dropImageView.ImageMask;
        }

        #endregion

        #region Private Methods

        private static void DrawBackground(Rect position)
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), CustomStyle.GetBackgroundColor());
        }

        public void UpdateSliderValuesForModel()
        {
            if (DataCache.instance.SelectedModelType == "flux.1.1-pro-ultra")
            {
                imagesliderValue = 1;
                samplesliderValue = 0;
                guidancesliderValue = 0;
            }
            else if (DataCache.instance.SelectedModelType == "flux1.1-pro")
            {
                imagesliderValue = 1;
                samplesliderValue = 0;
                guidancesliderValue = 0;
            }
            else if (DataCache.instance.SelectedModelType == "flux.1-pro")
            {
                imagesliderValue = 1;
                samplesliderValue = 25;
                guidancesliderValue = 3;
            }
            else if (DataCache.instance.SelectedModelId == "flux.1-schnell")
            {
                imagesliderValue = 4;
                samplesliderValue = 4;
                guidancesliderValue = 3.5f;
            }
            else if (DataCache.instance.SelectedModelType.StartsWith("flux"))
            {
                imagesliderValue = 4;
                samplesliderValue = 28;
                guidancesliderValue = 3.5f;
            }
            else if (DataCache.instance.SelectedModelType.StartsWith("sd-xl"))
            {
                imagesliderValue = 4;
                samplesliderValue = 30;
                guidancesliderValue = 6;
            }
            else if (DataCache.instance.SelectedModelType.StartsWith("sd-1.5"))
            {
                imagesliderValue = 4;
                samplesliderValue = 30;
                guidancesliderValue = 7;
            }
        }

        /// <summary>
        /// WARNING --> may have to remove activeMode.IsControlNet if it useless
        /// Manage all display available depending on active creation mode
        /// </summary>
        private void ManageDrawMode()
        {
            promptWindow.ActiveMode = promptPusher.GetActiveMode();
            CreationMode activeMode = promptWindow.ActiveMode;

            promptPusher.imageUpload = dropImageView.ImageUpload;
            promptPusher.maskImage = dropImageView.ImageMask;

            promptPusher.additionalImageUpload = dropAdditionalImageView.ImageUpload;

            switch (activeMode.EMode)
            {
                case ECreationMode.Text_To_Image:
                    // No specific UI for Text_To_Image mode
                    break;

                case ECreationMode.Image_To_Image:
                    dropImageView.DrawHandleImage();
                    CustomStyle.Space();
                    break;

                case ECreationMode.Inpaint:
                    dropImageView.DrawHandleImage();
                    GUILayout.BeginHorizontal();
                    if (activeMode.IsControlNet)
                    {
                        if (GUILayout.Button("Add Mask"))
                        {
                            InpaintingEditor.ShowWindow(dropImageView.ImageUpload);
                        }
                    }
                    GUILayout.EndHorizontal();
                    break;

                case ECreationMode.ControlNet:
                    dropImageView.DrawHandleImage();
                    CustomStyle.Space();
                    RenderControlNetFoldout();
                    break;

                case ECreationMode.IP_Adapter:
                    dropImageView.DrawHandleImage();
                    CustomStyle.Space();
                    DrawAdditionalModality("IP Adapter Scale");
                    break;

                case ECreationMode.Texture:
                    // No specific UI for Texture mode, similar to Text_To_Image
                    break;

                case ECreationMode.Image_To_Image__ControlNet:
                    dropImageView.DrawHandleImage("(Image to image)");
                    CustomStyle.Space();
                    dropAdditionalImageView.DrawHandleImage("(ControlNet)");
                    CustomStyle.Space();
                    RenderControlNetFoldout();
                    break;

                case ECreationMode.Image_To_Image__IP_Adapter:
                    dropImageView.DrawHandleImage("(Image to image)");
                    CustomStyle.Space();
                    dropAdditionalImageView.DrawHandleImage("(IP Adapter)");
                    CustomStyle.Space();
                    DrawAdditionalModality("IP Adapter Scale");
                    break;

                case ECreationMode.ControlNet__Inpaint:
                    dropImageView.DrawHandleImage("(ControlNet)");
                    CustomStyle.Space();
                    RenderControlNetFoldout();
                    CustomStyle.Space();
                    if (GUILayout.Button("Add Mask"))
                    {
                        InpaintingEditor.ShowWindow(dropImageView.ImageUpload);
                    }
                    break;

                case ECreationMode.ControlNet__IP_Adapter:
                    dropImageView.DrawHandleImage("(ControlNet)");
                    CustomStyle.Space();
                    RenderControlNetFoldout();
                    CustomStyle.Space();
                    dropAdditionalImageView.DrawHandleImage("(IP Adapter)");
                    CustomStyle.Space();
                    DrawAdditionalModality("IP Adapter Scale");
                    break;

                case ECreationMode.ControlNet__Texture:
                    dropImageView.DrawHandleImage();
                    CustomStyle.Space();
                    RenderControlNetFoldout();
                    CustomStyle.Space();
                    // Implement UI for ControlNet__Texture
                    break;

            }

            promptPusher.UpdateActiveMode(activeMode);
        }

        /// <summary>
        /// Draw display for additional modality
        /// </summary>
        /// <param name="_additionalName"> Name of the additional variables. </param>
        private void DrawAdditionalModality(string _additionalName)
        {
            GUILayout.BeginHorizontal();
            { 
                GUILayout.Label($"{_additionalName} :", EditorStyles.label);
                additionalModalitySliderValue = (int)EditorGUILayout.Slider(Mathf.Clamp(additionalModalitySliderValue, 0, 100), 0, 100);
                additionalModalityValue = additionalModalitySliderValue / 100.0f;
                if (additionalModalityValue == 0)
                {
                    additionalModalityValue = 0.01f;
                }

                promptPusher.additionalModalityValue = additionalModalityValue;
            }
            GUILayout.EndHorizontal();
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

        private void PromptRecv(string str)
        {
            //promptinputText = str;
        }

        private void NegativePromptRecv(string str)
        {
            //negativeInputText = str;
        }

        #endregion
    }
}
