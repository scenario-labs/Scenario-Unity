using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Scenario.Editor;

namespace Scenario.Editor
{
    public class PlanarProjection : EditorWindow
    {
        #region Public Fields

        public static PlanarProjection Instance = null;

        public int FlagWindow { get { return flagWindow; } set { flagWindow = value; } }
        public GameObject ReferenceObject { get { return referenceObject; } set { referenceObject = value; } }
        public bool CallRecorderInstall { get { return callRecorderInstall; } set { callRecorderInstall = value; } }
        public bool CallPPInstall { get { return callPPInstall; } set { callPPInstall = value; } }
        public Texture2D CaptureImage { get { return captureImage; } set { captureImage = value; } }
        public Texture2D RenderResultSelected { get { return renderResultSelected; } set { renderResultSelected = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// 
        /// </summary>
        private PlanarProjectionView planarProjectionView = null;

        /// <summary>
        /// 
        /// </summary>
        private int flagWindow = 0;

        /// <summary>
        /// 
        /// </summary>
        private GameObject referenceObject = null;

        /// <summary>
        /// 
        /// </summary>
        private Camera mainCamera = null;

        /// <summary>
        /// 
        /// </summary>
        private GameObject volumePP = null;

        /// <summary>
        /// 
        /// </summary>
        private Request request = null;

        /// <summary>
        /// 
        /// </summary>
        private bool callRecorderInstall = false;

        /// <summary>
        /// 
        /// </summary>
        private bool callPPInstall = false;

        /// <summary>
        /// 
        /// </summary>
        private RecorderWindow recorderWindow = null;

        /// <summary>
        /// 
        /// </summary>
        private RecorderControllerSettings recorderSettings = null;

        /// <summary>
        /// 
        /// </summary>
        private RecorderController recorderController = null;

        /// <summary>
        /// 
        /// </summary>
        private ImageRecorderSettings imageRecorderSettings = null;

        /// <summary>
        /// 
        /// </summary>
        private DirectoryInfo directoryInfo = null;

        /// <summary>
        /// 
        /// </summary>
        private Texture2D captureImage = null;

        /// <summary>
        /// 
        /// </summary>
        private Texture2D renderResultSelected = null;

        /// <summary>
        /// 
        /// </summary>
        private PromptWindow promptWindow = null;

        /// <summary>
        /// 
        /// </summary>
        private Projector projector = null;

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

            if (Instance == null)
            {
                Instance = this;
            }

            if (recorderWindow != null)
            {
                EditorCoroutineUtility.StartCoroutine(CloseRecorder(), this);
            }

            if (directoryInfo == null)
            {
                directoryInfo = new DirectoryInfo($"{Application.dataPath}/Recordings");
                LoadLastCapture();
            }
        }

        private void OnGUI()
        {
            planarProjectionView.Render(this.position);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        public void CreateReferenceObject()
        {
            referenceObject = new GameObject("LEVEL");
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public void CheckUnityRecorder()
        {
            request = Client.Add("com.unity.recorder");
            EditorApplication.update += Progress;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CheckPostProcessing()
        {
            request = Client.Add("com.unity.postprocessing");
            EditorApplication.update += Progress;
        }

        /// <summary>
        /// 
        /// </summary>
        public void LaunchUnityRecorder()
        {
            recorderWindow = GetWindow<RecorderWindow>();

            PrepareRecorderSettings();

        }

        /// <summary>
        /// 
        /// </summary>
        public void OpenPromptWindow()
        { 
            promptWindow = GetWindow<PromptWindow>();
            if (PromptPusher.Instance != null)
            {
                promptWindow.SetActiveModeUI(ECreationMode.ControlNet);
                PromptWindow.SetDropImageContent(captureImage);
                promptWindow.ActiveAdvanceSettings(true);
                SetControlNetOptions();
                InferenceManager.SilenceMode = true;
                promptWindow.SetImageSettingWidth(1824);
                promptWindow.SetImageSettingHeight(1024);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_renderResult"></param>
        public void OpenPlanarProjection(Texture2D _renderResult)
        {
            ShowWindow();
            flagWindow = 4;

            renderResultSelected = new Texture2D(2,2);
            renderResultSelected = _renderResult;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenderProjectionWork()
        {
            if (projector == null)
            {
                if (mainCamera != null)
                {
                    GameObject projectorObject = new GameObject("Projector");
                    projectorObject.transform.parent = mainCamera.transform;
                    projector = projectorObject.AddComponent<Projector>();

                }
            }

            CreateProjectedLayer("Projected");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        private void GetMainCamera()
        { 
            mainCamera = Camera.main;
        }

        /// <summary>
        /// 
        /// </summary>
        private void PrepareRecorderSettings()
        {
            recorderSettings = CreateInstance<RecorderControllerSettings>();
            recorderSettings.ExitPlayMode = true;
            recorderSettings.SetRecordModeToSingleFrame(1);

            imageRecorderSettings = CreateInstance<ImageRecorderSettings>();
            imageRecorderSettings.Enabled = true;
            imageRecorderSettings.RecordMode = RecordMode.Manual;
            imageRecorderSettings.name = "Scenario Sequence";

            imageRecorderSettings.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1824,
                OutputHeight = 1024,
            };

            imageRecorderSettings.Enabled = true;
            imageRecorderSettings.FileNameGenerator.ForceAssetsFolder = true;
            imageRecorderSettings.FileNameGenerator.FileName = "<Recorder>_<Take>";

            if (!Directory.Exists($"{Application.dataPath}/{imageRecorderSettings.FileNameGenerator.Leaf}"))
            {
                directoryInfo = Directory.CreateDirectory($"{Application.dataPath}/{imageRecorderSettings.FileNameGenerator.Leaf}");
            }
            else
            {
                directoryInfo = new DirectoryInfo($"{Application.dataPath}/{imageRecorderSettings.FileNameGenerator.Leaf}");
                Debug.Log(directoryInfo.FullName);
            }

            recorderSettings.AddRecorderSettings(imageRecorderSettings);
            recorderController = new RecorderController(recorderSettings);
            recorderWindow.SetRecorderControllerSettings(recorderSettings);

            EditorCoroutineUtility.StartCoroutine(Start(), this);
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadLastCapture()
        {
            if (directoryInfo != null && directoryInfo.GetFileSystemInfos().Length > 0)
            { 
                string pathFile = string.Empty;
                FileSystemInfo fileSystemInfo = null;
                foreach (FileSystemInfo fsi in directoryInfo.EnumerateFileSystemInfos())
                {
                    if (fileSystemInfo == null)
                    { 
                        fileSystemInfo = fsi;
                        continue;
                    }

                    if (fsi.CreationTimeUtc < fileSystemInfo.CreationTimeUtc)
                    {
                        fileSystemInfo = fsi;
                        continue;
                    }
                }
                Debug.Log(fileSystemInfo.FullName);
                pathFile = fileSystemInfo.FullName;

                captureImage = new Texture2D(2, 2);
                byte[] imageData = File.ReadAllBytes(pathFile);
                captureImage.LoadImage(imageData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetControlNetOptions()
        {
            if (promptWindow != null)
            {
                promptWindow.SetAdvancedModality(6);
                promptWindow.SetAdvancedModalityValue(75);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool CreateProjectedLayer(string _layerName)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            // Layers Property
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (!PropertyExists(layersProp, 0, 31, _layerName))
            {
                SerializedProperty sp;
                // Start at layer 9th index -> 8 (zero based) => first 8 reserved for unity / greyed out
                for (int i = 8, j = 31; i < j; i++)
                {
                    sp = layersProp.GetArrayElementAtIndex(i);
                    if (sp.stringValue == "")
                    {
                        // Assign string value to layer
                        sp.stringValue = _layerName;
                        Debug.Log("Layer: " + _layerName + " has been added");
                        // Save settings
                        tagManager.ApplyModifiedProperties();
                        return true;
                    }
                    if (i == j)
                        Debug.Log("All allowed layers have been filled");
                }
            }
            else
            {
                //Debug.Log ("Layer: " + layerName + " already exists");
            }
            return false;
        }

        private bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int i = start; i < end; i++)
            {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void Progress()
        {
            if (request.IsCompleted)
            {
                if (request.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + request.ToString());
                }
                else if (request.Status >= StatusCode.Failure)
                {
                    Debug.Log(request.Error.message);
                }

                EditorApplication.update -= Progress;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator Start()
        {
            yield return new EditorWaitForSeconds(1f);
            // TODO Trouble on register the first capture from the recorder inside assets folder
            recorderWindow.StartRecording();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator CloseRecorder()
        {
            yield return new EditorWaitForSeconds(1f);
            recorderWindow.Close();
            recorderWindow = null;
        }

        #endregion
    }
}
