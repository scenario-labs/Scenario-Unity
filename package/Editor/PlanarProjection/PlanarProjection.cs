using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering.PostProcessing;

namespace Scenario.Editor
{
    public class PlanarProjection : EditorWindow
    {
        #region Public Fields

        public int FlagWindow { get { return flagWindow; } set { flagWindow = value; } }

        public GameObject ReferenceObject { get { return referenceObject; } set { referenceObject = value; } }

        #endregion

        #region Private Fields

        private PlanarProjectionView planarProjectionView = null;

        private int flagWindow = 0;

        private GameObject referenceObject = null;

        private Camera mainCamera = null;

        private GameObject volumePP = null;

        #endregion

        #region MonoBehaviour Callbacks

        [MenuItem("Window/Scenario/Workflow/Planar Projection", false,2)]
        public static void ShowWindow()
        {
            GetWindow<PlanarProjection>("Planar Projection");
        }

        private void OnEnable()
        {
            planarProjectionView = new PlanarProjectionView(this);
        }

        private void OnGUI()
        {
            planarProjectionView.Render(this.position);
        }

        #endregion

        #region Public Methods

        public void CreateReferenceObject()
        {
            referenceObject = new GameObject("LEVEL");
        }

        public void AutoConfigureScene()
        {
            if (mainCamera == null)
            { 
                GetMainCamera();
            }

            if (!mainCamera.gameObject.GetComponent<PostProcessLayer>())
            { 
                PostProcessLayer layer = mainCamera.gameObject.AddComponent<PostProcessLayer>();
            }

            volumePP = new GameObject("Volume");
            
            PostProcessVolume volume = volumePP.AddComponent<PostProcessVolume>();

            volume.isGlobal = true;
            PostProcessProfile profile = new PostProcessProfile();
            AmbientOcclusion ao = new AmbientOcclusion();

            AmbientOcclusionModeParameter aoMode = new AmbientOcclusionModeParameter();
            aoMode.value = AmbientOcclusionMode.MultiScaleVolumetricObscurance;
            aoMode.overrideState = true;

            ao.active = true;
            ao.enabled.value = true;

            ao.mode = aoMode;

            ao.intensity.value = 0.55f;
            ao.intensity.overrideState = true;

            ao.thicknessModifier.value = 1f;
            ao.thicknessModifier.overrideState = true;
            profile.AddSettings(ao);
            volume.profile = profile;
            volume.sharedProfile = profile;
            volume.runInEditMode = true;
        }

        #endregion

        #region Private Methods

        private void GetMainCamera()
        { 
            mainCamera = Camera.main;
        }

        #endregion
    }
}
