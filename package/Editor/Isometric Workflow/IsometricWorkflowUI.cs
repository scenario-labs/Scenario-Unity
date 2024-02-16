using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

namespace Scenario.Editor
{
    public class IsometricWorkflowUI
    {
        private static IsometricWorkflow isometricWorkflow;
        private bool baseNone = true;
        private bool baseSquare = false;
        private bool baseCustom = false;

        internal Texture2D customTexture;

        public void Init(IsometricWorkflow _isometricWorkflow)
        {
            isometricWorkflow = _isometricWorkflow;
        }

        /// <summary>
        /// Draws the background of the UI element with the specified position.
        /// This function fills the background of a UI element with a given color.
        /// </summary>
        /// <param name="position">The position and dimensions of the UI element.</param>
        private void DrawBackground(Rect position)
        {
            Color backgroundColor = EditorStyle.GetBackgroundColor();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        }

        #region Draw UI

        /// <summary>
        /// This function is responsible for rendering the interface for the first step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawBaseGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 1. Choose a Base", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Space(50);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                //None
                GUILayout.BeginVertical();
                {
                    CustomStyle.Space(45);
                    baseNone = GUILayout.Toggle(baseNone, "", GUILayout.Height(10));
                    CustomStyle.Space(45);

                    if (baseNone)
                    {
                        isometricWorkflow.selectedBase = IsometricWorkflow.Base.None;
                        baseSquare = false;
                        baseCustom = false;
                    }
                }
                GUILayout.EndVertical();
                CustomStyle.Space(-25);
                GUILayout.BeginVertical();
                {
                    GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(100));
                    CustomStyle.Label("None", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                //Space
                GUILayout.FlexibleSpace();

                //Square
                GUILayout.BeginVertical();
                {
                    CustomStyle.Space(45);
                    baseSquare = GUILayout.Toggle(baseSquare, "", GUILayout.Height(10));
                    CustomStyle.Space(45);

                    if (baseSquare)
                    {
                        isometricWorkflow.selectedBase = IsometricWorkflow.Base.Square;
                        baseNone = false;
                        baseCustom = false;
                    }
                }
                GUILayout.EndVertical();
                CustomStyle.Space(-25);
                GUILayout.BeginVertical();
                {
                    GUILayout.Box(IsometricWorkflow.settings.squareBaseTexture, GUILayout.Width(100), GUILayout.Height(100));
                    CustomStyle.Label("Square", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                //Space
                GUILayout.FlexibleSpace();

                //Custom
                GUILayout.BeginVertical();
                {
                    CustomStyle.Space(45);
                    baseCustom = GUILayout.Toggle(baseCustom, "", GUILayout.Height(10));
                    CustomStyle.Space(45);
                    if(customTexture == null)
                    {
                        baseCustom = false;
                    }

                    if (baseCustom)
                    {
                        isometricWorkflow.selectedBase = IsometricWorkflow.Base.Custom;
                        baseNone = false;
                        baseSquare = false;
                    }

                }
                GUILayout.EndVertical();
                CustomStyle.Space(-25);
                GUILayout.BeginVertical();
                {
                    customTexture = (Texture2D)EditorGUILayout.ObjectField(customTexture, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
                    CustomStyle.Label("Custom", alignment: TextAnchor.MiddleCenter);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            if(!baseNone && !baseSquare && !baseCustom)
            {
                baseNone = true;
            }


            //Bottom
            GUILayout.FlexibleSpace();
            CustomStyle.ButtonPrimary("Next", 30, () =>
            {
                isometricWorkflow.currentStep = IsometricWorkflow.Step.Style;
            });
            CustomStyle.Space();
        }


        /// <summary>
        /// This function is responsible for rendering the interface for the second step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawStyleGUI(Rect _dimension)
        {

            DrawBackground(_dimension);
            CustomStyle.Space();
            CustomStyle.Label("Step 2. Choose a Style", 18, TextAnchor.UpperLeft, bold: true);
            CustomStyle.Space(50);
            //isometricWorkflow.selectedModel = (IsometricWorkflow.ModelStyle)GUILayout.SelectionGrid((int)isometricWorkflow.selectedBase, Enum.GetNames(typeof(IsometricWorkflow.ModelStyle)), 2, GUI.skin.GetStyle("toggle"));

            GUILayout.BeginVertical(); // Begin vertical grouping
            {
                var modelStyles = Enum.GetValues(typeof(IsometricWorkflow.ModelStyle));
                int selected = (int)isometricWorkflow.selectedModel;

                GUILayout.BeginHorizontal(); // Organize in rows
                {
                    GUILayout.FlexibleSpace();
                    foreach (IsometricWorkflow.ModelStyle modelStyle in modelStyles)
                    {
                        CustomStyle.Space(25); // Space between each containers
                        GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(150), GUILayout.Height(175)); // Container for each item
                        {
                            GUILayout.BeginVertical();
                            {
                                GUILayout.FlexibleSpace();
                                // Toggle button - adjust for your actual selected logic
                                bool isSelected = GUILayout.Toggle(selected == (int)modelStyle, ""); // Placeholder toggle, adjust as necessary
                                if (isSelected && selected != (int)modelStyle)
                                {
                                    isometricWorkflow.selectedModel = modelStyle; // Update the selected model
                                    selected = (int)modelStyle;
                                }
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical();
                            {
                                // Thumbnail
                                if (IsometricWorkflow.settings.isometricModelThumbnails.Exists(x => x.style == modelStyle))
                                {
                                    GUILayout.Label(IsometricWorkflow.settings.isometricModelThumbnails.Find(x => x.style == modelStyle).thumbnail, GUILayout.Width(150), GUILayout.Height(150)); // Adjust size as needed
                                }

                                // Name
                                CustomStyle.Label(modelStyle.ToString(), width:150, alignment:TextAnchor.MiddleCenter); // Centered text under the thumbnail
                            }
                            GUILayout.EndVertical();

                        }
                        GUILayout.EndHorizontal();

                        if ((int)modelStyle % 2 == 1) // Assuming you want 2 items per row
                        {
                            GUILayout.FlexibleSpace(); //flexible space at the right side
                            GUILayout.EndHorizontal();

                            CustomStyle.Space(25); // Space between each containers

                            GUILayout.BeginHorizontal(); // Start a new row after every 2 items
                            GUILayout.FlexibleSpace(); //flexible space at the left side
                        }
                    }

                    if (modelStyles.Length % 2 != 0)
                    {
                        GUILayout.EndHorizontal(); // End the row if an odd number of items
                    }

                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical();

            //Bottom
            GUILayout.FlexibleSpace();
            CustomStyle.ButtonPrimary("Next", 30, () =>
            {
                isometricWorkflow.currentStep = IsometricWorkflow.Step.Theme;
            });
            CustomStyle.Space();
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the third step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawThemeGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the fourth step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawAssetsGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
        }

        /// <summary>
        /// This function is responsible for rendering the interface for the last step
        /// </summary>
        /// <param name="_dimension">The dimensions of the UI element.</param>
        public void DrawValidationGUI(Rect _dimension)
        {
            DrawBackground(_dimension);
        }

        #endregion

        #region Utils


        #endregion
    }
}
