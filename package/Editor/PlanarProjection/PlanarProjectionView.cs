using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    /// <summary>
    /// Class view of the planar projection process.
    /// </summary>
    public class PlanarProjectionView 
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Reference to the planar projection controller
        /// </summary>
        private PlanarProjection planarProjection = null;

        #endregion

        #region MonoBehaviour Callbacks

        public PlanarProjectionView() 
        {
            planarProjection = new PlanarProjection();
        }

        public PlanarProjectionView(PlanarProjection _planarProjection)
        { 
            planarProjection = _planarProjection;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manage all display from this method, to each step.
        /// </summary>
        /// <param name="_position"></param>
        public void Render(Rect _position)
        {
            switch (planarProjection.FlagWindow)
            {
                case 0:
                    MainView();
                    break;

                case 1:
                    BasicPrepareView();
                    break;

                case 2:
                    RenderSceneView();
                    break;

                case 3:
                    RenderPromptView();
                    break;

                case 4:
                    RenderResult();
                    break;

                default:
                    MainView();
                    break;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The First and starting display.
        /// </summary>
        private void MainView()
        {
            GUILayout.BeginVertical();
            { 
                GUIStyle title = new GUIStyle(GUI.skin.label);
                title.fontSize = 18;
                title.fontStyle = FontStyle.Bold;

                GUILayout.Label("Planar Projection", title);

                GUIStyle button = new GUIStyle(GUI.skin.button);
                button.fontSize = 16;
                button.alignment = TextAnchor.MiddleCenter;
                button.fontStyle = FontStyle.Bold;

                if (GUILayout.Button("Project !", button))
                { 
                    planarProjection.FlagWindow = 1;
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Second step to create the reference gameObject and prepare the scene with post processing.
        /// </summary>
        private void BasicPrepareView()
        {
            GUILayout.BeginVertical();
            {
                GUIStyle title = new GUIStyle(GUI.skin.label);
                title.fontSize = 18;
                title.fontStyle = FontStyle.Bold;

                GUIStyle button = new GUIStyle(GUI.skin.button);
                button.fontSize = 14;
                button.fixedWidth = 250;
                button.fixedHeight = 50;
                button.alignment = TextAnchor.MiddleCenter;
                button.fontStyle = FontStyle.Bold;

                GUILayout.Label("Prepare your scene", title);

                if (GUILayout.Button("Create reference object", button))
                {
                    planarProjection.CreateReferenceObject();
                }

                planarProjection.ReferenceObject = (GameObject)EditorGUILayout.ObjectField(planarProjection.ReferenceObject, typeof(GameObject), true);
                if (planarProjection.ReferenceObject != null)
                { 
                    if(!planarProjection.ReferenceObject.tag.Equals("Scenario Object Projection"))
                        planarProjection.ReferenceObject.tag = "Scenario Object Projection"; 
                }


                if (GUILayout.Button("Auto configure scene", button))
                {
                    planarProjection.AutoConfigureScene();
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Previous", button))
                    {
                        planarProjection.FlagWindow = 0;
                    }
                    if (GUILayout.Button("Next", button))
                    {
                        planarProjection.FlagWindow = 2;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Third step to launch the Unity Recorder and save, load the capture.
        /// </summary>
        private void RenderSceneView()
        {
            GUILayout.BeginVertical();
            {
                GUIStyle title = new GUIStyle(GUI.skin.label);
                title.fontSize = 18;
                title.fontStyle = FontStyle.Bold;

                GUIStyle button = new GUIStyle(GUI.skin.button);
                button.fontSize = 14;
                button.fixedWidth = 250;
                button.fixedHeight = 50;
                button.alignment = TextAnchor.MiddleCenter;
                button.fontStyle = FontStyle.Bold;

                GUILayout.Label("Render scene", title);
   
                EditorGUILayout.BeginHorizontal();
                { 
                    if (GUILayout.Button("Capture Scene", button))
                    {
                        planarProjection.LaunchUnityRecorder();
                    }


                    if (planarProjection.CaptureImage != null)
                    {
                        GUIStyle imageStyle = new GUIStyle(GUI.skin.box);

                        imageStyle.fixedWidth = planarProjection.CaptureImage.width / 6;
                        imageStyle.fixedHeight = planarProjection.CaptureImage.height / 6;

                        GUILayout.Box(planarProjection.CaptureImage, imageStyle);
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Previous", button))
                    {
                        planarProjection.FlagWindow = 1;
                    }
                    if (GUILayout.Button("Next", button))
                    {
                        planarProjection.FlagWindow = 3;

                        planarProjection.OpenPromptWindow();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();   
        }

        /// <summary>
        /// Fourth step call the prompt window with the render capture.
        /// </summary>
        private void RenderPromptView()
        {
            GUILayout.BeginVertical();
            {
                GUIStyle title = new GUIStyle(GUI.skin.label);
                title.fontSize = 18;
                title.fontStyle = FontStyle.Bold;

                GUIStyle infoLabel = new GUIStyle(GUI.skin.label);
                infoLabel.fontSize = 14;
                infoLabel.alignment = TextAnchor.MiddleLeft;

                GUIStyle button = new GUIStyle(GUI.skin.button);
                button.fontSize = 14;
                button.fixedWidth = 250;
                button.fixedHeight = 50;
                button.alignment = TextAnchor.MiddleCenter;
                button.fontStyle = FontStyle.Bold;

                GUILayout.Label("Create your scene !", title);
                GUILayout.Label("Prompt your projection ideas into the prompt window.", infoLabel);

                GUILayout.Space(25);

                if (GUILayout.Button("Select existing images", button))
                {
                    planarProjection.OpenImageWindow();
                }

                GUILayout.Space(25);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Previous", button))
                    {
                        planarProjection.FlagWindow = 2;
                    }
                    if (GUILayout.Button("Next", button))
                    {
                        planarProjection.FlagWindow = 4;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Fifth step, Project the result into your scene.
        /// </summary>
        private void RenderResult()
        {
            GUILayout.BeginVertical();
            {
                GUIStyle title = new GUIStyle(GUI.skin.label);
                title.fontSize = 18;
                title.fontStyle = FontStyle.Bold;

                GUIStyle infoLabel = new GUIStyle(GUI.skin.label);
                infoLabel.fontSize = 14;
                infoLabel.alignment = TextAnchor.MiddleLeft;

                GUIStyle button = new GUIStyle(GUI.skin.button);
                button.fontSize = 14;
                button.fixedWidth = 250;
                button.fixedHeight = 50;
                button.alignment = TextAnchor.MiddleCenter;
                button.fontStyle = FontStyle.Bold;

                GUILayout.Label("Projection Menu", title);
                GUILayout.Label("Project the result", infoLabel);

                if (planarProjection.RenderResultSelected != null)
                {
                    GUIStyle imageStyle = new GUIStyle(GUI.skin.box);

                    imageStyle.fixedWidth = planarProjection.RenderResultSelected.width / 6;
                    imageStyle.fixedHeight = planarProjection.RenderResultSelected.height / 6;

                    GUILayout.Box(planarProjection.RenderResultSelected, imageStyle);
                }

                GUILayout.Space(25);

                if (GUILayout.Button("Render Projection", button))
                {
                    planarProjection.RenderProjectionWork();
                }

                GUILayout.Space(25);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Previous", button))
                    {
                        planarProjection.FlagWindow = 3;
                    }
                    if (GUILayout.Button("Next", button))
                    {
                        planarProjection.FlagWindow = 4;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        #endregion
    }
}