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

        /// <summary>
        /// Reference all dimension values available for SD 1.5 models
        /// </summary>
        public readonly int[] allowed1_5DimensionValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };

        /// <summary>
        /// Reference all dimension values available for SDXL models
        /// </summary>
        public readonly int[] allowedSDXLDimensionValues = { 1024, 1152, 1280, 1376, 1408, 1536, 1824, 2048 };

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
        private PromptPusher pusher = null;

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
                pusher = PromptPusher.Instance;
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

        public int NearestValueIndex(int currentValue, int[] allowedValues)
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

        #endregion

        #region Private Methods

        private static void DrawBackground(Rect position)
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), CustomStyle.GetBackgroundColor());
        }

        /// <summary>
        /// WARNING --> may have to remove activeMode.IsControlNet if it useless
        /// Manage all display available depending on active creation mode
        /// </summary>
        private void ManageDrawMode()
        {
            promptWindow.ActiveMode = pusher.GetActiveMode();
            CreationMode activeMode = promptWindow.ActiveMode;

            pusher.imageUpload = dropImageView.ImageUpload;
            pusher.maskImage = dropImageView.ImageMask;

            pusher.additionalImageUpload = dropAdditionalImageView.ImageUpload;

            switch (activeMode.EMode)
            {
                case ECreationMode.Image_To_Image:

                    dropImageView.DrawHandleImage();

                    CustomStyle.Space();

                    break;

                case ECreationMode.In_Painting:

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

                case ECreationMode.Control_Net:

                    dropImageView.DrawHandleImage();

                    CustomStyle.Space();

                    RenderControlNetFoldout();

                    break;

                case ECreationMode.Ip_Adapter:

                    dropImageView.DrawHandleImage();

                    CustomStyle.Space(); 

                    DrawAdditionalModality("Ip Adapter Scale");

                    break;

                case ECreationMode.Reference_Only:
                    dropImageView.DrawHandleImage();

                    CustomStyle.Space();

                    DrawAdditionalModality("Style Fidelity");

                    GUILayout.BeginHorizontal();
                    { 
                        GUILayout.Label("Reference Attn: ", EditorStyles.label);
                        if (activeMode.AdditionalSettings.ContainsKey("Reference Attn"))
                        {
                            bool refAtt = GUILayout.Toggle(activeMode.AdditionalSettings["Reference Attn"], "");
                            activeMode.AdditionalSettings["Reference Attn"] = refAtt;
                        }
                        else
                        {
                            bool refAtt = GUILayout.Toggle(true, "");
                            activeMode.AdditionalSettings.Add("Reference Attn", refAtt);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Reference AdaIN: ", EditorStyles.label);
                        if (activeMode.AdditionalSettings.ContainsKey("Reference AdaIN"))
                        {
                            bool refAd = GUILayout.Toggle(activeMode.AdditionalSettings["Reference AdaIN"], "");
                            activeMode.AdditionalSettings["Reference AdaIN"] = refAd;
                        }
                        else
                        {
                            bool refAd = GUILayout.Toggle(false, "");
                            activeMode.AdditionalSettings.Add("Reference AdaIN", refAd);
                        }
                    }
                    GUILayout.EndHorizontal();

                    break;

                case ECreationMode.Image_To_Image__Control_Net:

                    dropImageView.DrawHandleImage("(Image to image)");

                    CustomStyle.Space();

                    dropAdditionalImageView.DrawHandleImage("(Control Net)");

                    CustomStyle.Space();

                    RenderControlNetFoldout();

                    break;

                case ECreationMode.Image_To_Image__Ip_Adapter:

                    dropImageView.DrawHandleImage("(Image to image)");

                    CustomStyle.Space();

                    dropAdditionalImageView.DrawHandleImage("(Ip Adapter)");

                    CustomStyle.Space();

                    DrawAdditionalModality("Ip Adapter Scale");

                    break;

                case ECreationMode.Control_Net__Ip_Adapter:

                    dropImageView.DrawHandleImage("(Control Net)");

                    CustomStyle.Space();

                    RenderControlNetFoldout();

                    CustomStyle.Space();

                    dropAdditionalImageView.DrawHandleImage("(Ip Adapter)");

                    CustomStyle.Space();

                    DrawAdditionalModality("Ip Adapter Scale");

                    break;
            }

            pusher.UpdateActiveMode(activeMode);
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

                pusher.additionalModalityValue = additionalModalityValue;
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
