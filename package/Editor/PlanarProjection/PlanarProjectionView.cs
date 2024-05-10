using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class PlanarProjectionView 
    {
        #region Public Fields
        #endregion

        #region Private Fields

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
                    break;

                default:
                    MainView();
                    break;
            }
        }

        #endregion

        #region Private Methods

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

#if UNITY_RECORDER
#else    
                if(!planarProjection.CallRecorderInstall)
                {
                    if (EditorUtility.DisplayDialog("Unity Recorder required", "Unity Recorder is required for this stage. Would you like to install it?", "Install", "Cancel"))
                    {
                        planarProjection.CheckUnityRecorder();
                        planarProjection.CallRecorderInstall = true;
                    }
                    else
                    {
                        planarProjection.CallRecorderInstall = true;
                    }
                }

                if (GUILayout.Button("Install Recorder", button))
                {
                    planarProjection.CheckUnityRecorder();
                }
#endif

                if (GUILayout.Button("Capture Scene", button))
                {
                    planarProjection.LaunchUnityRecorder();
                }

                if (planarProjection.CaptureImage != null)
                {
                    GUI.DrawTexture(planarProjection.position, planarProjection.CaptureImage);
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Previous", button))
                    {
                        planarProjection.FlagWindow = 1;
                    }
                    if (GUILayout.Button("Next", button))
                    {
                        planarProjection.FlagWindow = 3;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();   
        }

#endregion
    }
}
