using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#if UNITY_RECORDER
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Scenario.Editor
{
    [Serializable]
    public struct TargetBundle
    {
        #region Public Fields

        public GameObject Target { get { return target; } set { target = value; } }
        public MeshFilter MeshTarget { get { return meshTarget; } set { meshTarget = value; } }
        public MeshRenderer MeshRenderer { get { return meshRenderer; } set { meshRenderer = value; } }
        //public EOrientation EOrientation { get { return eOrientation; } set { eOrientation = value; } }
        public List<Texture2D> TexturesGenerated { get { return texturesGenerated; } set { texturesGenerated = value; } }

        #endregion

        #region Private Fields

        [SerializeField]
        private GameObject target;

        [SerializeField]
        private MeshFilter meshTarget;

        [SerializeField]
        private MeshRenderer meshRenderer;

        /*[SerializeField]
        private EOrientation eOrientation;*/

        [SerializeField]
        private List<Texture2D> texturesGenerated;

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    /// <summary>
    /// Planar Projection Window Class, manage all the planar projection workflow.
    /// All models (MVC pattern like) are used inside this class
    /// </summary>
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
        /// Reference of the planar projection View.
        /// </summary>
        private PlanarProjectionView planarProjectionView = null;

        /// <summary>
        /// Use the flag to display a specific screen in the view.
        /// </summary>
        private int flagWindow = 0;

        /// <summary>
        /// Reference of the gameObject where all the projection workflow will happen.
        /// </summary>
        private GameObject referenceObject = null;

        /// <summary>
        /// The Main Camera of the scene.
        /// </summary>
        private Camera mainCamera = null;

        /// <summary>
        /// Reference of the post processing volume.
        /// </summary>
        private GameObject volumePP = null;

        /// <summary>
        /// Request used to make a request to the unity package manager, to install unity recorder.
        /// </summary>
        private Request request = null;

        /// <summary>
        /// Check if the installation of the recorder is already done.
        /// </summary>
        private bool callRecorderInstall = false;

        /// <summary>
        /// Check if the installation of the post processing is already done.
        /// </summary>
        private bool callPPInstall = false;

#if UNITY_RECORDER

        /// <summary>
        /// Get a reference of the recorder window.
        /// </summary>
        private RecorderWindow recorderWindow = null;

#endif

        /// <summary>
        /// Allow to manipulate directories' project.
        /// </summary>
        private DirectoryInfo directoryInfo = null;

        /// <summary>
        /// The image captured from the unity recorder
        /// </summary>
        private Texture2D captureImage = null;

        /// <summary>
        /// The image selected from the scenario generations.
        /// </summary>
        private Texture2D renderResultSelected = null;

        /// <summary>
        /// The result of the projection register in one texture
        /// </summary>
        private Texture2D projectedTexture = null;

        /// <summary>
        /// Reference of the prompt window
        /// </summary>
        private PromptWindow promptWindow = null;

        /// <summary>
        /// Projector to project a texture into the scene.
        /// </summary>
        private Projector projector = null;

        /// <summary>
        /// The material to apply on the projector.
        /// </summary>
        private Material globalProjector = null;

        /// <summary>
        /// A specific shader to create a material to apply on the projector.
        /// </summary>
        private Shader projectionShader = null;

        /// <summary>
        /// A specific shader to create a material to apply on the target object.
        /// </summary>
        private Shader renderShader = null;

        /// <summary>
        /// A temporary camera to register the result of the projection.
        /// </summary>
        private Camera renderCamera = null;

        /// <summary>
        /// A temporary render texture to take a picture of the result before rendering it into a texture.
        /// </summary>
        private RenderTexture renderTexture = null;

        /// <summary>
        /// Navigate into all target objects available inside the referenceObject.
        /// </summary>
        private int flag = -1;

        /// <summary>
        /// List all object to apply the projection on.
        /// </summary>
        private List<TargetBundle> targetBundles = new List<TargetBundle>();

        /// <summary>
        /// The selected target object to treat.
        /// </summary>
        private TargetBundle selectedTargetBundle;

        /// <summary>
        /// Path file to save the texture unwrapped of the projection.
        /// </summary>
        private string destinationPath = string.Empty;

        /// <summary>
        /// Variable to manipulate scale on the projection shader.
        /// </summary>
        private float scaleFlat = 2f;

        /// <summary>
        /// Correctly placed the unwrapped texture to the render texture on X axis.
        /// </summary>
        private float xOffset = 1f;

        /// <summary>
        /// Correctly placed the unwrapped texture to the render texture on Y axis.
        /// </summary>
        private float yOffset = 1f;

        /// <summary>
        /// Default size of the texture.
        /// </summary>
        private Vector2 textureSize = new Vector2(1024, 1024);

        /// <summary>
        /// Get the width size of the texture and reused it.
        /// </summary>
        private int textureWidth = 1024;

        /// <summary>
        /// Get the height size of the texture and reused it.
        /// </summary>
        private int textureHeight = 1024;

        /// <summary>
        /// Option to make appear the project folder window to select a path and register the destination folder path.
        /// </summary>
        private bool activeSelectFolderSave = true;

        /// <summary>
        /// The workflow is processing.
        /// </summary>
        private bool processing = false;

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
#if UNITY_RECORDER
            if (recorderWindow != null)
            {
                EditorCoroutineUtility.StartCoroutine(CloseRecorder(), this);
            }
            else
            {
                directoryInfo = new DirectoryInfo($"{Application.dataPath}/Recordings");
                LoadLastCapture();
            }
#endif
        }

        private void OnGUI()
        {
            planarProjectionView.Render(this.position);
        }

#endregion

        #region Public Methods

        /// <summary>
        /// Create a reference object directly into the current scene.
        /// </summary>
        public void CreateReferenceObject()
        {
            referenceObject = new GameObject("LEVEL");
        }

        /// <summary>
        /// Prepare the scene to use correctly the workflow.
        /// Get the main camera once.
        /// Add a post process layer component to the camera.
        /// Create the post process volume and automaticaly set it to use ambient occlusion.
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
            #if UNITY_RECORDER
            recorderWindow = GetWindow<RecorderWindow>();

            var preset = AssetDatabase.LoadAssetAtPath<RecorderControllerSettingsPreset>($"{CommonUtils.PluginFolderPath()}/Assets/Recorder/RecorderSettingPreset.asset");

            recorderWindow.ApplyPreset( preset );
            PrepareRecorderSettings(true);
#endif

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
        public void OpenImageWindow()
        {
            InferenceManager.SilenceMode = true;
            GetWindow<Images>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_renderResult"></param>
        public void OpenPlanarProjection(string _filePath)
        {
            ShowWindow();
            flagWindow = 4;

            byte[] imageData = File.ReadAllBytes(_filePath);
            renderResultSelected = new Texture2D(2,2);
            renderResultSelected.LoadImage(imageData);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenderProjectionWork()
        {
            if (projector == null)
            {
                SetProjector();
            }

            CreateProjectedLayer("Projected");

            globalProjector = CommonGraphics.GetMaterial("Scenario_Projector Global");

            if (globalProjector != null)
            {
                if (globalProjector.HasTexture("_Decal"))
                {
                    if (renderResultSelected != null)
                    {
                        globalProjector.SetTexture("_Decal", renderResultSelected);
                    }
                    else
                    {
                        throw new Exception("Result selected texture is empty");
                    }
                }

                projector.material = globalProjector;
            }
            
            renderShader = null;
            if (renderShader == null)
            {
                renderShader = CommonGraphics.GetShader("Scenario/Unlit/Scenario 2UV");
            }

            if (referenceObject != null)
            {
                referenceObject.layer = LayerMask.NameToLayer("Projected");

                if (referenceObject.GetComponent<MeshRenderer>())
                { 
                    MeshRenderer renderer = referenceObject.GetComponent<MeshRenderer>();
                    renderer.material = globalProjector;
                }

                SetLevel();
            }

            if (renderCamera == null)
            {
                CreateRenderCamera();
            }

            SearchTargets();

            RenderProjection();
        }

#endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        private void SearchTargets()
        {
            if (targetBundles != null && targetBundles.Count > 0)
            {
                targetBundles.Clear();
            }

            if (referenceObject != null)
            {
                SearchTarget(referenceObject.transform);
            }
            else
            {
                throw new Exception("Reference Object not filled, go back to previous step.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void RenderProjection()
        {
            flag = -1;
            EditorCoroutineUtility.StartCoroutine(RenderAll(), this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_searchTarget"></param>
        private void SearchTarget(Transform _searchTarget)
        {
            if (_searchTarget != null)
            {
                foreach (Transform child in _searchTarget.transform)
                {
                    if (child.childCount > 0)
                    {
                        SearchTarget(child);
                    }

                    TargetBundle bundleObject = new TargetBundle();
                    bundleObject.Target = child.gameObject;
                    if (child.GetComponent<MeshFilter>())
                    {
                        bundleObject.MeshTarget = child.GetComponent<MeshFilter>();
                    }
                    else
                    {
                        Debug.LogError($"{bundleObject.Target} does not have a Mesh Filter Component", bundleObject.Target);
                    }

                    if (child.GetComponent<MeshRenderer>())
                    {
                        bundleObject.MeshRenderer = child.GetComponent<MeshRenderer>();
                    }
                    else
                    {
                        Debug.LogError($"{bundleObject.Target} does not have a Mesh Renderer Component", bundleObject.Target);
                    }

                    //bundleObject.EOrientation = facingOrientation;

                    targetBundles.Add(bundleObject);

                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool SelectTarget()
        {
            if (targetBundles != null && targetBundles.Count > 0)
            {
                if (flag + 1 < targetBundles.Count)
                {
                    flag++;
                    selectedTargetBundle = targetBundles[flag];
                    return true;
                }
                else
                {
                    flag = -1;
                    return false;
                }
            }
            return false;
        }

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
        private void CreateRenderCamera()
        {
            // Calculate the orthographic size of the camera to fit the mesh
            //Bounds bounds = selectedTargetBundle.MeshRenderer.bounds;
            //float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float maxDimension = 100;
            float orthographicSize = maxDimension / 2f;

            GameObject tempCameraObject = new GameObject("RenderCamera");
            renderCamera = tempCameraObject.AddComponent<Camera>();
            renderCamera.orthographic = true;
            renderCamera.farClipPlane = 150;
            renderCamera.clearFlags = CameraClearFlags.Skybox;
            //renderCamera.backgroundColor = Color.white;
            renderCamera.orthographicSize = orthographicSize;
            renderCamera.cullingMask = 1 << LayerMask.NameToLayer("Projected");

            /*float sizeA, sizeB;

            sizeB = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            if (sizeB == bounds.size.x)
            {
                sizeA = Mathf.Max(bounds.size.y, bounds.size.z);
            }
            else if (sizeB == bounds.size.y)
            {
                sizeA = Mathf.Max(bounds.size.x, bounds.size.z);
            }
            else
            {
                sizeA = Mathf.Max(bounds.size.x, bounds.size.y);
            }

            if ((sizeA / sizeB) > 1.0f)
            {
                renderCamera.aspect = sizeB / sizeA;
            }
            else
            {
                renderCamera.aspect = sizeA / sizeB;
            }
            Debug.Log("Vector3 bounds: " + bounds.size + " calcul: " + sizeA + " / " + sizeB + " = " + renderCamera.aspect, renderCamera.gameObject);
            //tempCamera.aspect = 0.9f;
            */

            renderCamera.transform.position = referenceObject.transform.position - referenceObject.transform.forward * 10f;
            //renderCamera.transform.parent = selectedTargetBundle.Target.transform;
            renderCamera.transform.LookAt(referenceObject.transform.position);

            /*Quaternion tempCameraRotation = renderCamera.transform.localRotation;
            tempCameraRotation.eulerAngles = new Vector3(renderCamera.transform.localRotation.eulerAngles.x, 0, 0);
            renderCamera.transform.localRotation = tempCameraRotation;*/

            //Create a render texture for rendering
            if (renderResultSelected != null)
            {
                renderTexture = new RenderTexture(1024, 1024, 0);
            }
            else
            {
                renderTexture = new RenderTexture(1024, 1024, 0);
            }
            renderCamera.targetTexture = renderTexture;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetProjector()
        {
            if (mainCamera != null)
            {
                GameObject projectorObject = new GameObject("Projector");
                projectorObject.transform.parent = mainCamera.transform;
                projectorObject.transform.position = mainCamera.transform.position;
                projectorObject.transform.rotation = mainCamera.transform.rotation;

                projector = projectorObject.AddComponent<Projector>();

                projector.aspectRatio = ((float)renderResultSelected.width / (float)renderResultSelected.height);

                projector.orthographicSize = 5;

                projector.ignoreLayers = LayerMask.NameToLayer("Everything") - ( 1 << LayerMask.NameToLayer("Projected"));
            }
            else
            {
                GetMainCamera();
                SetProjector();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetLevel()
        {
            if (referenceObject != null)
            {
                if (referenceObject.transform.childCount > 0)
                {
                    foreach (Transform child in referenceObject.transform)
                    {
                        SetPart(child);
                    }
                }
                else
                {
                    SetPart(referenceObject.transform);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_part"></param>
        private void SetPart(Transform _part)
        {
            if (_part != null)
            {
                _part.gameObject.layer = LayerMask.NameToLayer("Projected");

                if (_part.GetComponent<MeshRenderer>())
                {
                    MeshRenderer childRenderer = _part.GetComponent<MeshRenderer>();
                    if (globalProjector != null)
                    {
                        childRenderer.material = globalProjector;
                    }
                }

                if (_part.childCount > 0)
                {
                    foreach (Transform child in _part)
                    {
                        SetPart(child);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PrepareRecorderSettings(bool _record)
        {
            if (_record)
            { 
                EditorCoroutineUtility.StartCoroutine(StartRecorder(), this);
            }
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
                    if (!fsi.Extension.Equals(".meta"))
                    { 
                        if (fileSystemInfo == null)
                        { 
                            fileSystemInfo = fsi;
                            continue;
                        }

                        if (fsi.CreationTimeUtc > fileSystemInfo.CreationTimeUtc)
                        {
                            fileSystemInfo = fsi;
                            continue;
                        }
                    }
                }
                pathFile = fileSystemInfo.FullName;

                Debug.Log(pathFile);

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
        public void SetPropertiesToRender()
        {
            Material projectorMaterial = selectedTargetBundle.MeshRenderer.material;

            if (projectorMaterial.HasFloat("_Slider"))
            {
                projectorMaterial.SetFloat("_Slider", 0.0f);
                projectorMaterial.SetFloat("_ScaleFlat", scaleFlat);
                projectorMaterial.SetFloat("_OffsetXFlat", xOffset);
                projectorMaterial.SetFloat("_OffsetYFlat", yOffset);
            }
        }

        [ContextMenu("Reset properties")]
        public void ResetPropertiesToRender()
        {
            Material projectorMaterial = selectedTargetBundle.MeshRenderer.material;

            if (projectorMaterial.HasFloat("_Slider"))
            {
                projectorMaterial.SetFloat("_Slider", 1.0f);
                /*projectorMaterial.SetFloat("_ScaleFlat", scaleFlat);
                projectorMaterial.SetFloat("_OffsetXFlat", xOffset);
                projectorMaterial.SetFloat("_OffsetYFlat", yOffset);*/
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PrepareBundleProperties()
        {
            if (selectedTargetBundle.Target != null)
            {
                if (selectedTargetBundle.TexturesGenerated == null)
                {
                    selectedTargetBundle.TexturesGenerated = new List<Texture2D>();
                }
                else if (selectedTargetBundle.TexturesGenerated.Count > 0)
                {
                    selectedTargetBundle.TexturesGenerated.Clear();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenderCamera()
        {
            // Render the scene from the camera's perspective
            renderCamera.Render();

            // Read pixels from the render texture
            RenderTexture.active = renderTexture;
            projectedTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            projectedTexture.Apply();
            RenderTexture.active = null;

            if (selectedTargetBundle.Target != null)
            {
                Texture2D toAdd = projectedTexture;
                if (selectedTargetBundle.TexturesGenerated != null)
                {
                    selectedTargetBundle.TexturesGenerated.Add(toAdd);
                }
                else
                {
                    selectedTargetBundle.TexturesGenerated = new List<Texture2D>();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetTexture()
        {
            // Get the texture size
            textureWidth = (int)textureSize.x; // Adjust as needed
            textureHeight = (int)textureSize.y; // Adjust as needed

            // Create a new texture to store the projected result
            projectedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        }

        /// <summary>
        /// 
        /// </summary>
        [ContextMenu("Isolate Target")]
        public void IsolateTarget()
        {
            if (selectedTargetBundle.MeshRenderer != null)
            {
                if (selectedTargetBundle.MeshRenderer.transform.parent != null)
                {
                    Transform parent = selectedTargetBundle.MeshRenderer.transform.parent;

                    if (parent.childCount > 0)
                    {
                        foreach (Transform child in parent)
                        {
                            if (child != selectedTargetBundle.MeshRenderer.transform)
                            {
                                child.gameObject.SetActive(false);
                            }
                            else
                            {
                                child.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void GetBackOthers()
        {
            if (selectedTargetBundle.MeshRenderer != null)
            {
                if (selectedTargetBundle.MeshRenderer.transform.parent != null)
                {
                    Transform parent = selectedTargetBundle.MeshRenderer.transform.parent;

                    if (parent.childCount > 0)
                    {
                        foreach (Transform child in parent)
                        {
                            if (child != selectedTargetBundle.MeshRenderer.transform)
                            {
                                child.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveRender(/*int _index*/)
        {
            if (activeSelectFolderSave)
            {
                // Save the texture

                if (string.IsNullOrEmpty(destinationPath))
                {
                    destinationPath = EditorUtility.SaveFolderPanel("Save Projected Texture", "", $@"ProjectedTexture_{flag}.png");
                    Debug.Log(destinationPath);
                }

                if (destinationPath.Length != 0)
                {
                    byte[] bytes = projectedTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes($@"{destinationPath}/ProjectedTexture_{flag}_{selectedTargetBundle.Target.gameObject.name}.png", bytes);
                    Debug.Log("Projected texture saved to: " + $@"{destinationPath}/ProjectedTexture_{flag}_{selectedTargetBundle.Target.gameObject.name}.png");

                    // Apply the texture to the material of the target mesh
                    //Material material = selectedTargetBundle.MeshRenderer.material;
                    //material.mainTexture = projectedTexture;
                }
            }
            else
            {
                // TODO save by default
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ApplyProjection()
        {
            if (renderShader != null && selectedTargetBundle.TexturesGenerated != null && selectedTargetBundle.TexturesGenerated.Count > 0)
            {
                Material resultProjected = new Material(renderShader);


                if (resultProjected.HasTexture("_MainTex"))
                {
                    resultProjected.SetTexture("_MainTex", selectedTargetBundle.TexturesGenerated[0]);
                }

                /*if (resultProjected.HasTexture("_MainTexFront"))
                {
                    resultProjected.SetTexture("_MainTexFront", selectedTargetBundle.TexturesGenerated[1]);
                }

                if (resultProjected.HasTexture("_MainTexBack"))
                {
                    resultProjected.SetTexture("_MainTexBack", selectedTargetBundle.TexturesGenerated[3]);
                }

                if (resultProjected.HasTexture("_MainTexLeft"))
                {
                    resultProjected.SetTexture("_MainTexLeft", selectedTargetBundle.TexturesGenerated[4]);
                }

                if (resultProjected.HasTexture("_MainTexRight"))
                {
                    resultProjected.SetTexture("_MainTexRight", selectedTargetBundle.TexturesGenerated[5]);
                }

                if (resultProjected.HasTexture("_MainTexTop"))
                {
                    resultProjected.SetTexture("_MainTexTop", selectedTargetBundle.TexturesGenerated[0]);
                }

                if (resultProjected.HasTexture("_MainTexBottom"))
                {
                    resultProjected.SetTexture("_MainTexBottom", selectedTargetBundle.TexturesGenerated[2]);
                }*/

                selectedTargetBundle.MeshRenderer.material = resultProjected;
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
        IEnumerator StartRecorder()
        {
            yield return new EditorWaitForSeconds(1f);
            // TODO Trouble on register the first capture from the recorder inside assets folder
#if UNITY_RECORDER
            recorderWindow.StartRecording();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator CloseRecorder()
        {
            yield return new EditorWaitForSeconds(2f);

#if UNITY_RECORDER
            var preset = AssetDatabase.LoadAssetAtPath<RecorderControllerSettingsPreset>($"{CommonUtils.PluginFolderPath()}/Assets/Recorder/RecorderSettingPreset.asset");

            recorderWindow.ApplyPreset(preset);

            recorderWindow.Close();
            recorderWindow = null;

            yield return new EditorWaitForSeconds(2f);
            
            directoryInfo = new DirectoryInfo($"{Application.dataPath}/Recordings");
            LoadLastCapture();
#endif
        }

        IEnumerator RenderAll()
        {
            EditorCoroutine renderCoroutine = null;
            while (flag < targetBundles.Count - 1)
            {
                SelectTarget();

                if (renderCoroutine == null)
                {
                    processing = true;
                    renderCoroutine = EditorCoroutineUtility.StartCoroutine(RenderOneGeneration(), this);
                }

                while (processing)
                {
                    yield return new WaitForEndOfFrame();
                }

                renderCoroutine = null;
            }

            renderCoroutine = null;
            destinationPath = string.Empty;
            yield return null;
        }

        IEnumerator RenderOneGeneration()
        {
            // Ensure Projector component is assigned
            if (projector == null)
            {
                Debug.LogError("Projector component not assigned.");
                yield return null;
            }
            else if (!projector.enabled)
            {
                projector.enabled = true;
            }

            // Get the mesh
            if (selectedTargetBundle.Target == null || selectedTargetBundle.MeshTarget.sharedMesh == null)
            {
                Debug.LogError("MeshFilter component or mesh not found.");
                yield return null;
            }

            processing = true;

            //GetMeshFromMeshFilter();
            IsolateTarget();

            //ClearMaterialTexture();

            SetTexture();

            //CreateTemporaryRenderCamera();

            SetPropertiesToRender();
            yield return new WaitForSeconds(1.0f);

            PrepareBundleProperties();

            RenderCamera();

            SaveRender();
            yield return new WaitForSeconds(1.0f);
            ResetPropertiesToRender();

            ApplyProjection();

            // Deactivate the projector
            projector.enabled = false;

            GetBackOthers();

            processing = false;
            yield return null;
        }

#endregion
    }
}