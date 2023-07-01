using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

public class PromptWindowUI
{

    public PromptWindow promptWindow;

    static internal Texture2D imageUpload;
    static internal Texture2D imageMask;

    internal bool isTextToImage = true;
    internal bool isImageToImage = false;
    internal bool isControlNet = false;
    internal bool showSettings = true;
    internal bool controlNetFoldout = false;
    internal bool isAdvancedSettings = false;
    internal int controlNetModeIndex = 0;
    internal bool isInpainting = false;

    internal string promptinputText = "";
    internal string negativepromptinputText = "";
    internal int widthSliderValue = 512;
    internal int heightSliderValue = 512;
    internal float imagesliderValue = 4;
    internal float samplesliderValue = 50;
    internal float influncesliderValue = 0.25f;
    internal float guidancesliderValue = 7;
    internal string selectedModelName = "Choose Model";
    internal string postedModelName = "Choose Model";
    internal string seedinputText = "";
    public string selectedPreset = "";
    public int selectedOption1Index = 0;
    public int selectedOption2Index = 0;
    public int selectedOption3Index = 0;
    public float sliderValue1 = 0.1f;
    public float sliderValue2 = 0.1f;
    public float sliderValue3 = 0.1f;

    private Vector2 scrollPosition;
    private Vector2 scrollPosition1 = Vector2.zero;
    private Vector2 scrollPosition2 = Vector2.zero;

    public readonly string[] dropdownOptions = { "", "canny", "pose", "depth", "lines", "seg", "scribble", "lineart", "normal-map", "shuffle" };

    private readonly int[] allowedWidthValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };
    private readonly int[] allowedHeightValues = { 512, 576, 640, 688, 704, 768, 912, 1024 };

    private List<string> tags = new List<string>();
    private string inputText = "";
    private int dragFromIndex = -1;
    private Vector2 dragStartPos;
    private List<Rect> tagRects = new List<Rect>();

    private List<string> negativeTags = new List<string>();
    private string negativeInputText = "";
    private int negativeDragFromIndex = -1;
    private Vector2 negativeDragStartPos;
    private List<Rect> negativeTagRects = new List<Rect>();

    public string SelectedModelName
    {
        get
        {
            return selectedModelName;
        }
        set
        {
            selectedModelName = value;
        }
    }

    public PromptWindowUI(PromptWindow promptWindow)
    {
        this.promptWindow = promptWindow;
    }

    public void PromptRecv(string str)
    {
        //promptinputText = str;
    }

    public void NegativePromptRecv(string str)
    {
        //negativeInputText = str;
    }

    public void Render(Rect position)
    {

        Color backgroundColor = new Color32(26, 26, 26, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        tagRects.Clear();

        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            GUI.FocusControl(null);
        }

        GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.textArea);
        wordWrapStyle.wordWrap = true;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUIStyle.none);
        GUILayout.Label("Generate Images", EditorStyles.boldLabel);

        if (GUILayout.Button(selectedModelName, GUILayout.Height(30)))
        {
            Models.ShowWindow();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Prompt");
        
        GUILayout.FlexibleSpace();

        GUIContent plusPrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus").image);
        if (GUILayout.Button(plusPrompt, GUILayout.Width(20), GUILayout.Height(15))) {
            PromptBuilderWindow.isFromNegativePrompt = false;
            PromptBuilderWindow.ShowWindow(PromptRecv, tags);
        }

        /*GUIContent minusPrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Minus").image);
        if (GUILayout.Button(minusPrompt, GUILayout.Width(20), GUILayout.Height(15))) {
        Debug.Log("Button 2 clicked");
        }*/

        GUILayout.EndHorizontal();

        GUILayout.Space(10f);

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle customTagStyle = new GUIStyle(EditorStyles.label);
                customTagStyle.fixedHeight = 25;
                customTagStyle.margin = new RectOffset(0, 5, 0, 5);

                float availableWidth = EditorGUIUtility.currentViewWidth - 20;
                int tagsPerRow = Mathf.FloorToInt(availableWidth / 100);
                int currentTagIndex = 0;

                while (currentTagIndex < tags.Count)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < tagsPerRow && currentTagIndex < tags.Count; i++)
                    {
                        string tag = tags[currentTagIndex];
                        string displayTag = TruncateTag(tag);

                        GUIContent tagContent = new GUIContent(displayTag, tag);
                        Rect tagRect = GUILayoutUtility.GetRect(tagContent, customTagStyle);

                        bool isActiveTag = currentTagIndex == dragFromIndex;
                        GUIStyle tagStyle = isActiveTag ? new GUIStyle(customTagStyle) { normal = { background = MakeTex(2, 2, isActiveTag ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.8f, 0.8f, 0.8f)) } } : customTagStyle;

                        Rect xRect = new Rect(tagRect.xMax - 20, tagRect.y, 20, tagRect.height);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 0 && Event.current.clickCount == 2 && tagRect.Contains(Event.current.mousePosition))
                            {
                                int plusCount = tag.Count(c => c == '+');
                                if (plusCount < 3)
                                {
                                    tags[currentTagIndex] += "+";
                                }
                            }
                            else if (Event.current.button == 1 && tagRect.Contains(Event.current.mousePosition))
                            {
                                if (tag.EndsWith("+"))
                                {
                                    tags[currentTagIndex] = tag.Remove(tag.LastIndexOf('+'));
                                }
                            }
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tagRect.Contains(Event.current.mousePosition))
                        {
                            if (!xRect.Contains(Event.current.mousePosition))
                            {
                                dragFromIndex = currentTagIndex;
                                dragStartPos = Event.current.mousePosition;
                                Event.current.Use();
                            }
                        }

                        if (dragFromIndex >= 0 && Event.current.type == EventType.MouseDrag)
                        {
                            int newIndex = GetNewIndex(Event.current.mousePosition);
                            if (newIndex != -1 && newIndex != dragFromIndex && newIndex < tags.Count)
                            {
                                string tempTag = tags[dragFromIndex];
                                tags.RemoveAt(dragFromIndex);
                                tags.Insert(newIndex, tempTag);
                                dragFromIndex = newIndex;
                            }
                        }

                        if (Event.current.type == EventType.MouseUp)
                        {
                            dragFromIndex = -1;
                        }

                        EditorGUI.LabelField(tagRect, tagContent, tagStyle);

                        if (GUI.Button(xRect, "x"))
                        {
                            tags.RemoveAt(currentTagIndex);
                        }
                        else
                        {
                            currentTagIndex++;
                        }

                        tagRects.Add(tagRect);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            {
                GUI.SetNextControlName("inputTextField");
                inputText = EditorGUILayout.TextField(inputText, GUILayout.ExpandWidth(true), GUILayout.Height(25));

                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "inputTextField")
                {
                    if (!string.IsNullOrWhiteSpace(inputText))
                    {
                        string descriptorName = inputText.Trim();
                        tags.Add(descriptorName);
                        inputText = "";
                        Event.current.Use();
                    }
                    else
                    {
                        EditorGUI.FocusTextInControl("inputTextField");
                        Event.current.Use();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Negative Prompt");

        GUILayout.FlexibleSpace();

        GUIContent plusNegativePrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus").image);
        if (GUILayout.Button(plusNegativePrompt, GUILayout.Width(20), GUILayout.Height(15))) {
            PromptBuilderWindow.isFromNegativePrompt = true;
            PromptBuilderWindow.ShowWindow(NegativePromptRecv, negativeTags);
        }

        /*GUIContent minusNegativePrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Minus").image);
        if (GUILayout.Button(minusNegativePrompt, GUILayout.Width(20), GUILayout.Height(15))) {
        Debug.Log("Button 2 clicked");
        }*/
        GUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle customTagStyle = new GUIStyle(EditorStyles.label);
                customTagStyle.fixedHeight = 25;
                customTagStyle.margin = new RectOffset(0, 5, 0, 5);

                float availableWidth = EditorGUIUtility.currentViewWidth - 20;
                int tagsPerRow = Mathf.FloorToInt(availableWidth / 100);
                int currentTagIndex = 0;

                while (currentTagIndex < negativeTags.Count)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < tagsPerRow && currentTagIndex < negativeTags.Count; i++)
                    {
                        string tag = negativeTags[currentTagIndex];
                        string displayTag = TruncateTag(tag);

                        GUIContent tagContent = new GUIContent(displayTag, tag);
                        Rect tagRect = GUILayoutUtility.GetRect(tagContent, customTagStyle);

                        bool isActiveTag = currentTagIndex == negativeDragFromIndex;
                        GUIStyle tagStyle = isActiveTag ? new GUIStyle(customTagStyle) { normal = { background = MakeTex(2, 2, isActiveTag ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.8f, 0.8f, 0.8f)) } } : customTagStyle;

                        Rect xRect = new Rect(tagRect.xMax - 20, tagRect.y, 20, tagRect.height);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 0 && Event.current.clickCount == 2 && tagRect.Contains(Event.current.mousePosition))
                            {
                                int plusCount = tag.Split('+').Length - 1;
                                if (plusCount < 3)
                                {
                                    negativeTags[currentTagIndex] += "+";
                                }
                            }
                            else if (Event.current.button == 1 && tagRect.Contains(Event.current.mousePosition))
                            {
                                if (tag.EndsWith("+"))
                                {
                                    negativeTags[currentTagIndex] = tag.Remove(tag.LastIndexOf('+'));
                                }
                            }
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tagRect.Contains(Event.current.mousePosition))
                        {
                            if (!xRect.Contains(Event.current.mousePosition))
                            {
                                negativeDragFromIndex = currentTagIndex;
                                dragStartPos = Event.current.mousePosition;
                                Event.current.Use();
                            }
                        }

                        if (negativeDragFromIndex >= 0 && Event.current.type == EventType.MouseDrag)
                        {
                            int newIndex = GetNewIndex(Event.current.mousePosition);
                            if (newIndex != -1 && newIndex != negativeDragFromIndex && newIndex < negativeTags.Count)
                            {
                                string tempTag = negativeTags[negativeDragFromIndex];
                                negativeTags.RemoveAt(negativeDragFromIndex);
                                negativeTags.Insert(newIndex, tempTag);
                                negativeDragFromIndex = newIndex;
                            }
                        }

                        if (Event.current.type == EventType.MouseUp)
                        {
                            negativeDragFromIndex = -1;
                        }

                        EditorGUI.LabelField(tagRect, tagContent, tagStyle);

                        if (GUI.Button(xRect, "x"))
                        {
                            negativeTags.RemoveAt(currentTagIndex);
                        }
                        else
                        {
                            currentTagIndex++;
                        }

                        negativeTagRects.Add(tagRect);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            {
                GUI.SetNextControlName("negativeInputTextField");
                negativeInputText = EditorGUILayout.TextField(negativeInputText, GUILayout.ExpandWidth(true), GUILayout.Height(25));

                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "negativeInputTextField")
                {
                    if (!string.IsNullOrWhiteSpace(negativeInputText))
                    {
                        string descriptorName = negativeInputText.Trim();
                        negativeTags.Add(descriptorName);
                        negativeInputText = "";
                        Event.current.Use();
                    }
                    else
                    {
                        EditorGUI.FocusTextInControl("negativeInputTextField");
                        Event.current.Use();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        bool shouldAutoGenerateSeed = imagesliderValue > 1;
        if (shouldAutoGenerateSeed)
        {
            seedinputText = "-1";
        }

        GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button);
        generateButtonStyle.normal.background = CreateColorTexture(new Color(0, 0.5333f, 0.8f, 1));
        generateButtonStyle.normal.textColor = Color.white;
        generateButtonStyle.active.background = CreateColorTexture(new Color(0, 0.3f, 0.6f, 1));
        generateButtonStyle.active.textColor = Color.white;

        if (GUILayout.Button("Generate Image", generateButtonStyle, GUILayout.Height(40)))
        {
            promptinputText = SerializeTags(tags);
            negativepromptinputText = SerializeTags(negativeTags);

            string selectedModelName = UnityEditor.EditorPrefs.GetString("SelectedModelId");
            string postedModelName = selectedModelName;
            UnityEditor.EditorPrefs.SetString("postedModelName", postedModelName);

            if (shouldAutoGenerateSeed)
            {
                promptWindow.GenerateImage(null);
            }
            else
            {
                string seed = seedinputText;
                if (seed == "-1")
                {
                    seed = null;
                }
                promptWindow.GenerateImage(seed);
            }
        }

        GUILayout.Space(10f);

        showSettings = EditorGUILayout.Foldout(showSettings, "Image Settings");
        if (showSettings) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Width: ", EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
            int widthIndex = NearestValueIndex(widthSliderValue, allowedWidthValues);
            widthIndex = GUILayout.SelectionGrid(widthIndex, Array.ConvertAll(allowedWidthValues, x => x.ToString()), allowedWidthValues.Length);
            widthSliderValue = allowedWidthValues[widthIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Height: ", EditorStyles.label, GUILayout.Width(55), GUILayout.Height(20));
            int heightIndex = NearestValueIndex(heightSliderValue, allowedHeightValues);
            heightIndex = GUILayout.SelectionGrid(heightIndex, Array.ConvertAll(allowedHeightValues, x => x.ToString()), allowedHeightValues.Length);
            heightSliderValue = allowedHeightValues[heightIndex];
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20f);

            float labelWidthPercentage = 0.2f;
            float sliderWidthPercentage = 0.78f;

            int labelWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * labelWidthPercentage);
            int sliderWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * sliderWidthPercentage);

            int imagesliderIntValue = Mathf.RoundToInt(imagesliderValue);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Images: " + imagesliderIntValue, GUILayout.Width(labelWidth));
            imagesliderValue = GUILayout.HorizontalSlider(imagesliderValue, 1, 16, GUILayout.Width(sliderWidth));
            EditorGUILayout.EndHorizontal();

            int samplesliderIntValue = Mathf.RoundToInt(samplesliderValue);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sampling steps: " + samplesliderIntValue, GUILayout.Width(labelWidth));
            samplesliderValue = GUILayout.HorizontalSlider(samplesliderValue, 10, 150, GUILayout.Width(sliderWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Guidance: " + guidancesliderValue.ToString("0.0"), GUILayout.Width(labelWidth));
            guidancesliderValue = Mathf.Round(GUILayout.HorizontalSlider(guidancesliderValue, 0f, 20f, GUILayout.Width(sliderWidth)) * 10) / 10f;
            EditorGUILayout.EndHorizontal();

            if (isImageToImage || isControlNet) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Influence: " + influncesliderValue.ToString("0.00"), GUILayout.Width(labelWidth));
                influncesliderValue = GUILayout.HorizontalSlider(influncesliderValue, 0f, 1f, GUILayout.Width(sliderWidth));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(labelWidth));

            if (shouldAutoGenerateSeed) {
                GUI.enabled = false;
                GUILayout.TextArea("-1", GUILayout.Height(20), GUILayout.Width(sliderWidth));
            } else {
                GUI.enabled = true;
                seedinputText = GUILayout.TextField(seedinputText, GUILayout.Height(20), GUILayout.Width(sliderWidth));
                if (seedinputText == "-1") {
                    promptWindow.SetSeed(null);
                } else {
                    promptWindow.SetSeed(seedinputText);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = true;

        GUILayout.Space(20f);

        GUILayout.Label("Type");

        int selectedTab = isTextToImage ? 0 : (isImageToImage ? 1 : 2);
        string[] tabLabels = { "Text to Image", "Image to Image", "Inpainting" };
        selectedTab = GUILayout.Toolbar(selectedTab, tabLabels);

        if (selectedTab == 0)
        {
            isTextToImage = true;
            isImageToImage = false;
            controlNetFoldout = false;
            isControlNet = false;
            isAdvancedSettings = false;
            isInpainting = false;
        }
        else if (selectedTab == 1)
        {
            isTextToImage = false;
            isImageToImage = true;
            isInpainting = false;
        }
        else if (selectedTab == 2)
        {
            isTextToImage = false;
            isImageToImage = false;
            controlNetFoldout = false;
            isControlNet = false;
            isAdvancedSettings = false;
            isInpainting = true;
        }

        if (isImageToImage || isInpainting)
        {
            GUILayout.Space(10f);

            GUILayout.Label("Upload Image");

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

            if (imageUpload != null)
            {
                GUI.DrawTexture(dropArea, imageUpload, ScaleMode.ScaleToFit);
                
                Rect dropdownButtonRect = new Rect(dropArea.xMax - 90, dropArea.yMax - 25, 30, 30);
                if (imageUpload != null && GUI.Button(dropdownButtonRect, "..."))
                {
                    GenericMenu toolsMenu = new GenericMenu();
                    toolsMenu.AddItem(new GUIContent("Remove bg"), false, () => 
                    {
                        Debug.Log("Remove bg button pressed");
                        promptWindow.RemoveBackground(imageUpload);
                    });
                    toolsMenu.AddItem(new GUIContent("Adjust aspect ratio"), false, () =>
                    {
                        if (imageUpload != null)
                        {
                            int currentWidth = imageUpload.width;
                            int currentHeight = imageUpload.height;

                            int matchingWidth = GetMatchingValue(currentWidth, allowedWidthValues);
                            int matchingHeight = GetMatchingValue(currentHeight, allowedHeightValues);

                            widthSliderValue = matchingWidth != -1 ? matchingWidth : currentWidth;
                            heightSliderValue = matchingHeight != -1 ? matchingHeight : currentHeight;

                            selectedOption1Index = NearestValueIndex(widthSliderValue, allowedWidthValues);
                            selectedOption2Index = NearestValueIndex(heightSliderValue, allowedHeightValues);
                        }
                    });
                    toolsMenu.DropDown(dropdownButtonRect);
                }

                Rect clearImageButtonRect = new Rect(dropArea.xMax - 50, dropArea.yMax - 25, 30, 30);
                if (imageUpload != null && GUI.Button(clearImageButtonRect, "X"))
                {
                    imageUpload = null;
                    imageMask = null;
                }
            }

            if (imageMask != null)
            {
                GUI.DrawTexture(dropArea, imageMask, ScaleMode.ScaleToFit, true);
            }

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

            GUILayout.BeginHorizontal();

            if (isControlNet || isImageToImage)
            {
                if (GUILayout.Button("Create Image"))
                {
                    ImageEditor.ShowWindow(imageUpload);
                }
            }

            if (isControlNet)
            {
                if (GUILayout.Button("Add Control"))
                {
                  CompositionEditor.ShowWindow();
                }
            }

            if (isInpainting)
            {
                if (GUILayout.Button("Add Mask"))
                {
                    InpaintingEditor.ShowWindow(imageUpload);
                }
            }

            /*if (GUILayout.Button("Clear Image"))
            {
                imageUpload = null;
                imageMask = null;
            }*/

            GUILayout.EndHorizontal();

            if (isImageToImage)
            {
                GUILayout.Space(10f);

                controlNetFoldout = EditorGUILayout.Foldout(controlNetFoldout, "ControlNet Options");

                if (controlNetFoldout)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enable ControlNet", EditorStyles.label);
                    isControlNet = GUILayout.Toggle(isControlNet, "");

                    if (isControlNet)
                    {
                        GUILayout.Label("Advanced Settings", EditorStyles.label);
                        isAdvancedSettings = GUILayout.Toggle(isAdvancedSettings, "");
                    }

                    GUILayout.EndHorizontal();

                    if (isControlNet)
                    {
                        GUILayout.Space(20);

                        if (isAdvancedSettings)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Model 1", EditorStyles.label);

                            List<string> availableOptions1 = new List<string> { "None" };
                            availableOptions1.AddRange(dropdownOptions);
                            selectedOption1Index = EditorGUILayout.Popup(selectedOption1Index, availableOptions1.ToArray());

                            GUILayout.Label("Slider 1", EditorStyles.label);
                            sliderValue1 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue1, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
                            GUILayout.EndHorizontal();

                            if (selectedOption1Index > 0)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Model 2", EditorStyles.label);

                                List<string> availableOptions2 = new List<string> { "None" };
                                availableOptions2.AddRange(dropdownOptions);
                                availableOptions2.RemoveAt(selectedOption1Index);
                                selectedOption2Index = EditorGUILayout.Popup(selectedOption2Index, availableOptions2.ToArray());

                                GUILayout.Label("Slider 2", EditorStyles.label);
                                sliderValue2 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue2, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
                                GUILayout.EndHorizontal();
                            }

                            if (selectedOption2Index > 0)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Model 3", EditorStyles.label);

                                List<string> availableOptions3 = new List<string> { "None" };
                                availableOptions3.AddRange(dropdownOptions);
                                int option1Index = Array.IndexOf(dropdownOptions, availableOptions1[selectedOption1Index]);
                                int option2Index = Array.IndexOf(dropdownOptions, dropdownOptions[selectedOption2Index]);

                                availableOptions3.RemoveAt(option1Index + 1);
                                availableOptions3.RemoveAt(option2Index);

                                selectedOption3Index = EditorGUILayout.Popup(selectedOption3Index, availableOptions3.ToArray());

                                GUILayout.Label("Slider 3", EditorStyles.label);
                                sliderValue3 = Mathf.Round(EditorGUILayout.Slider(Mathf.Clamp(sliderValue3, 0.1f, 1.0f), 0.1f, 1.0f) * 100) / 100;
                                GUILayout.EndHorizontal();
                            }
                        }
                        else
                        {
                            GUILayout.Label("Presets:", EditorStyles.boldLabel);

                            string[] presets = { "Character", "Landscape", "City", "Interior" };

                            int selectedIndex = Array.IndexOf(presets, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selectedPreset));

                            selectedIndex = GUILayout.SelectionGrid(selectedIndex, presets, presets.Length);

                            if (selectedIndex >= 0 && selectedIndex < presets.Length)
                            {
                                selectedPreset = presets[selectedIndex].ToLower();
                            }
                        }
                    }
                }
            }
        }

        GUILayout.EndScrollView();
    }

    private string SerializeTags(List<string> tags)
    {
        string values = "";
        foreach (var tag in tags)
        {
            values += tag + ", ";
        }
        return values;
    }

    private string TruncateTag(string tag)
    {
        if (tag.Length <= 30)
        {
            return tag;
        }
        
        int lastSpaceIndex = tag.LastIndexOf(' ', 30);
        return lastSpaceIndex == -1 ? tag.Substring(0, 30) : tag.Substring(0, lastSpaceIndex);
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

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pixels);
        result.Apply();
        return result;
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