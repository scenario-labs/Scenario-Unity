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

namespace Scenario.Editor
{
    public class PlanarProjection : EditorWindow
    {
        #region Public Fields

        public int FlagWindow { get { return flagWindow; } set { flagWindow = value; } }
        public GameObject ReferenceObject { get { return referenceObject; } set { referenceObject = value; } }
        public bool CallRecorderInstall { get { return callRecorderInstall; } set { callRecorderInstall = value; } }
        public bool CallPPInstall { get { return callPPInstall; } set { callPPInstall = value; } }
        public Texture2D CaptureImage { get { return captureImage; } set { captureImage = value; } }

        #endregion

        #region Private Fields

        private PlanarProjectionView planarProjectionView = null;

        private int flagWindow = 0;

        private GameObject referenceObject = null;

        private Camera mainCamera = null;

        private GameObject volumePP = null;

        private Request request = null;

        private bool callRecorderInstall = false;

        private bool callPPInstall = false;

        private RecorderWindow recorderWindow = null;
        private RecorderControllerSettings recorderSettings = null;
        private RecorderController recorderController = null;
        private ImageRecorderSettings imageRecorderSettings = null;

        private DirectoryInfo directoryInfo = null;

        private Texture2D captureImage = null;

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

        public void CheckUnityRecorder()
        {
            request = Client.Add("com.unity.recorder");
            EditorApplication.update += Progress;
        }

        public void CheckPostProcessing()
        {
            request = Client.Add("com.unity.postprocessing");
            EditorApplication.update += Progress;
        }

        public void LaunchUnityRecorder()
        {
            recorderWindow = GetWindow<RecorderWindow>();

            PrepareRecorderSettings();

        }

        #endregion

        #region Private Methods

        private void GetMainCamera()
        { 
            mainCamera = Camera.main;
        }

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

        IEnumerator Start()
        {
            yield return new EditorWaitForSeconds(1f);
            // TODO Trouble on register the first capture from the recorder inside assets folder
            recorderWindow.StartRecording();
        }

        IEnumerator CloseRecorder()
        {
            yield return new EditorWaitForSeconds(1f);
            recorderWindow.Close();
            recorderWindow = null;
        }

        #endregion
    }
}
