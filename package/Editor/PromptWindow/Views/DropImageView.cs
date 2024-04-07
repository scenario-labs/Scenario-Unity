using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class DropImageView
    {

        #region Public Fields

        public Texture2D ImageUpload { get { return imageUpload; } }
        public Texture2D ImageMask { get { return imageMask; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Get a reference of the prompt window display
        /// </summary>
        private PromptWindowUI promptWindowUI = null;
        
        /// <summary>
        /// Reference to the image uploaded
        /// </summary>
        private Texture2D imageUpload = null;
        
        /// <summary>
        /// Reference to mask uploaded
        /// </summary>
        private Texture2D imageMask = null;

        #endregion

        #region Public Methods

        public DropImageView(PromptWindowUI _attachedUI)
        { 
            promptWindowUI = _attachedUI;
        }

        /// <summary>
        /// Draw display to the drag & drop area of the image
        /// </summary>
        public void DrawHandleImage()
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the drag & drop action
        /// </summary>
        private void HandleDrag()
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

        /// <summary>
        /// Display the image uploaded 
        /// </summary>
        /// <returns></returns>
        private Rect RenderImageUploadArea()
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

        /// <summary>
        /// Draw image uploaded and also additional options.
        /// </summary>
        /// <param name="dropArea"></param>
        /// <returns></returns>
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
                                allowedValues = promptWindowUI.allowedSDXLDimensionValues;
                                break;

                            case "sd-xl-lora":
                                allowedValues = promptWindowUI.allowedSDXLDimensionValues;
                                break;

                            case "sd-xl":
                                allowedValues = promptWindowUI.allowedSDXLDimensionValues;
                                break;

                            case "sd-1_5":
                                allowedValues = promptWindowUI.allowed1_5DimensionValues;
                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        allowedValues = promptWindowUI.allowed1_5DimensionValues;
                    }

                    matchingWidth = GetMatchingValue(currentWidth, allowedValues);
                    matchingHeight = GetMatchingValue(currentHeight, allowedValues);

                    promptWindowUI.WidthSliderValue = matchingWidth != -1 ? matchingWidth : currentWidth;
                    promptWindowUI.HeightSliderValue = matchingHeight != -1 ? matchingHeight : currentHeight;

                    promptWindowUI.selectedOptionIndex = promptWindowUI.NearestValueIndex(promptWindowUI.WidthSliderValue, allowedValues);
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

        /// <summary>
        /// Check if there is a same value
        /// </summary>
        /// <param name="targetValue"> Expected value </param>
        /// <param name="values"> Array of values to check in </param>
        /// <returns></returns>
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

        #endregion
    }
}
