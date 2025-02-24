using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public enum ECreationMode 
    {
        Text_To_Image,
        Image_To_Image,
        ControlNet,
        Inpaint,
        IP_Adapter,
        Reference_Only,
        Image_To_Image__ControlNet,
        //Reference_Only__Control_Net,
        Image_To_Image__IP_Adapter,
        ControlNet__IP_Adapter
    }

    /// <summary>
    /// PromptPusher class, manage all the generation process by itself.
    /// Containing all necessary data to display, or not, the settings and also use it from everywhere.
    /// </summary>
    [ExecuteInEditMode]
    [InitializeOnLoad]
    public class PromptPusher
    {
        #region Public Fields

        public static PromptPusher Instance = null;

        /// <summary>
        /// Translation to api calling modalities options, treat previous values in the exact same order.
        /// </summary>
        public readonly string[] CorrespondingOptionsValue =
        {
            "",
            "character",
            "landscape",
            "canny",
            "pose",
            "depth",
            "seg",
            "illusion",
            "city",
            "interior",
            "lines",
            "scribble",
            "normal-map",
            "lineart"
        };

        /// <summary>
        /// Get all scheduler options available.
        /// </summary>
        public readonly string[] SchedulerOptions = new string[] // Scheduler options extracted from your HTML
        {
            "Default", "DDIMScheduler", "DDPMScheduler", "DEISMultistepScheduler", "DPMSolverMultistepScheduler",
            "DPMSolverSinglestepScheduler", "EulerAncestralDiscreteScheduler", "EulerDiscreteScheduler",
            "HeunDiscreteScheduler", "KDPM2AncestralDiscreteScheduler", "KDPM2DiscreteScheduler",
            "LCMScheduler", "LMSDiscreteScheduler", "PNDMScheduler", "UniPCMultistepScheduler"
        };

        #endregion

        #region Private Fields

        /// <summary>
        /// Reference the model name selected
        /// </summary>
        internal string modelName = string.Empty;

        /// <summary>
        /// List all mode of creation available
        /// </summary>
        internal List<CreationMode> creationModeList = new List<CreationMode>();

        /// <summary>
        /// Reference of the mode of generation selected
        /// </summary>
        internal CreationMode activeMode = null;

        /// <summary>
        /// Reference modality preset selected // Investigate
        /// </summary>
        internal string selectedPreset = string.Empty;

        /// <summary>
        /// Reference of the modality selected like for controlNet
        /// </summary>
        internal int modalitySelected = -1;

        /// <summary>
        /// Reference the name of the selected modality
        /// </summary>
        internal string modalityName = string.Empty;

        /// <summary>
        /// Reference of the modaliti's value.
        /// </summary>
        internal float modalityValue = 0.0f;

        /// <summary>
        /// Number of images expected to be generated.
        /// </summary>
        internal int numberOfImages = 4;

        /// <summary>
        /// Reference the user's input prompt
        /// </summary>
        internal string promptInput = string.Empty;

        /// <summary>
        /// Reference the user's negative input prompt
        /// </summary>
        internal string promptNegativeInput = string.Empty;

        /// <summary>
        /// Reference number of samples expected on generation
        /// </summary>
        internal int samplesStep = 4;

        /// <summary>
        /// 
        /// </summary>
        internal float guidance = 7.0f;

        /// <summary>
        /// Reference of the seed from a user input
        /// </summary>
        internal string seedInput = "-1";

        /// <summary>
        /// Reference width of the image generated
        /// </summary>
        internal int width = 1024;

        /// <summary>
        /// Reference height size of the image generated
        /// </summary>
        internal int height = 1024;

        /// <summary>
        /// Reference influence value // Investigate useless ?
        /// </summary>
        internal float influenceOption = 0.25f;

        /// <summary>
        /// Reference the image upload to be used as reference
        /// </summary>
        internal Texture2D imageUpload = null;

        /// <summary>
        /// Reference the mask image to be used
        /// </summary>
        internal Texture2D maskImage = null;

        /// <summary>
        /// Reference scheluder selected.
        /// </summary>
        internal int schedulerSelected = 0;

        /// <summary>
        /// Reference the additional modality value with two mode active.
        /// </summary>
        internal float additionalModalityValue = 0.5f;

        /// <summary>
        /// Reference the second image upload to be used as reference
        /// </summary>
        internal Texture2D additionalImageUpload = null;

        #endregion

        #region Editor Callbacks

        static PromptPusher()
        {
            if (Instance == null)
            {
                PromptPusher pusher = new PromptPusher();
                Instance = pusher;
                Instance.Init();
            }
        }

        /// <summary>
        /// Create all Creation Mode available
        /// </summary>
        public void Init()
        {
            InitCreationMode();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get a specific creation mode by the mode type.
        /// </summary>
        /// <param name="_mode"> Expected mode type </param>
        /// <returns></returns>
        public CreationMode GetMode(ECreationMode _mode)
        {
            if (CheckListCreationMode())
            {
                foreach (CreationMode mode in creationModeList)
                {
                    if (mode.EMode == _mode)
                    { 
                        return mode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get a the state of a creation mode by it's mode type
        /// </summary>
        /// <param name="_mode"> Expected mode type</param>
        /// <returns></returns>
        public bool GetModeState(ECreationMode _mode)
        {
            if (CheckListCreationMode())
            {
                return GetMode(_mode).IsActive;
            }

            return false;
        }

        /// <summary>
        /// Get the mode which is active
        /// </summary>
        /// <returns></returns>
        public CreationMode GetActiveMode()
        {
            if (CheckListCreationMode())
            {
                foreach (CreationMode mode in creationModeList)
                {
                    if (mode.IsActive)
                    {
                        return mode;
                    }
                }
            }

            return GetMode(ECreationMode.Text_To_Image);
        }

        /// <summary>
        /// Set a specific mode state by it's mode type
        /// </summary>
        /// <param name="_mode"> Expected Mode type </param>
        /// <param name="_isActive"> New state value </param>
        public void SetMode(ECreationMode _mode, bool _isActive)
        {
            if (CheckListCreationMode())
            { 
                GetMode(_mode).IsActive = _isActive;
            }
        }

        /// <summary>
        /// Active a creation mode by it's mode type
        /// </summary>
        /// <param name="_mode"> Expected Mode type </param>
        public void ActiveMode(ECreationMode _mode)
        {
            if (CheckListCreationMode())
            {
                foreach (CreationMode mode in creationModeList)
                {
                    if (mode.EMode != _mode)
                    {
                        mode.IsActive = false;
                    }
                    else
                    {
                        mode.IsActive = true;
                    }
                }
            }
        }

        /// <summary>
        /// Active mode by it's name.
        /// </summary>
        /// <param name="_modeName"> The mode name </param>
        public void ActiveMode(string _modeName)
        {
            if (CheckListCreationMode())
            {
                foreach (CreationMode mode in creationModeList)
                {
                    if (mode.ModeName.Equals(_modeName))
                    {
                        mode.IsActive = true;
                    }
                    else
                    {
                        mode.IsActive = false;
                    }
                }
            }
        }

        /// <summary>
        /// Active a mode at a specific index
        /// </summary>
        /// <param name="_index"> Index of a creation mode </param>
        public void ActiveMode(int _index)
        {
            if (CheckListCreationMode())
            {
                for (int i = 0; i < creationModeList.Count; i++)
                {
                    if (i == _index)
                    {
                        creationModeList[i].IsActive = true;
                    }
                    else
                    {
                        creationModeList[i].IsActive = false;
                    }
                }
            }
        }

        /// <summary>
        /// Update data to a specific creation mode
        /// </summary>
        /// <param name="_activeMode"> Creation Mode expected </param>
        public void UpdateActiveMode(CreationMode _activeMode)
        {
            if (CheckListCreationMode())
            {
                int indexMode = -1;
                foreach (CreationMode mode in creationModeList)
                {
                    if(mode.EMode == _activeMode.EMode)
                    {
                        indexMode = creationModeList.IndexOf(mode);
                    }
                }

                if (indexMode > -1)
                {
                    creationModeList[indexMode] = _activeMode;
                }
            }
        }

        /// <summary>
        /// Launch the generation process.
        /// </summary>
        public void GenerateImage(string _seed)
        {
            Generate(_seed);
        }

        /// <summary>
        /// If you want to generate an image through Isometric Workflow you get the cost before generation
        /// </summary>
        /// <param name="_modelName"></param>
        /// <param name="_texture"></param>
        /// <param name="_numberOfImages"></param>
        /// <param name="_promptText"></param>
        /// <param name="_samples"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_guidance"></param>
        /// <param name="_seed"></param>
        /// <param name="_useCanny"></param>
        /// <param name="_cannyStrength"></param>
        public void AskGenerateIsometricImage(string _modelName, ECreationMode _mode, Texture2D _texture = null, int _numberOfImages = 4, string _promptText = "", int _samples = 30, int _width = 1024, int _height = 1024, float _guidance = 6.0f, string _seed = "-1", bool _useCanny = false, float _cannyStrength = 0.8f, Action<string> _onInferenceRequested = null)
        {
            modelName = _modelName;

            ActiveMode(_mode);

            ManageOptionMode();

            imageUpload = _texture;
            numberOfImages = _numberOfImages;
            promptInput = _promptText;
            samplesStep = _samples;
            width = _width;
            height = _height;
            guidance = _guidance;
            seedInput = _seed;
            modalitySelected = Array.IndexOf(CorrespondingOptionsValue, "canny") + 1;
            modalityValue = _cannyStrength;

            AskGenerate(_seed == "-1" ? null : _seed, _onInferenceRequested);
        }

        /// <summary>
        /// If you want to generate an image through Isometric Workflow
        /// </summary>
        /// <param name="_modelName"></param>
        /// <param name="_texture"></param>
        /// <param name="_numberOfImages"></param>
        /// <param name="_promptText"></param>
        /// <param name="_samples"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_guidance"></param>
        /// <param name="_seed"></param>
        /// <param name="_useCanny"></param>
        /// <param name="_cannyStrength"></param>
        public void GenerateIsometricImage(string _modelName, ECreationMode _mode, Texture2D _texture = null, int _numberOfImages = 4, string _promptText = "", int _samples = 30, int _width = 1024, int _height = 1024, float _guidance = 6.0f, string _seed = "-1", bool _useCanny = false, float _cannyStrength = 0.8f, Action<string> _onInferenceRequested = null)
        {
            modelName = _modelName;

            ActiveMode(_mode);

            ManageOptionMode();

            imageUpload = _texture;
            numberOfImages = _numberOfImages;
            promptInput = _promptText;
            samplesStep = _samples;
            width = _width;
            height = _height;
            guidance = _guidance;
            seedInput = _seed;
            modalitySelected = Array.IndexOf(CorrespondingOptionsValue, "canny") + 1;
            modalityValue = _cannyStrength;

            Generate(_seed == "-1" ? null : _seed, _onInferenceRequested);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Verify data input and launch api request to get cost
        /// </summary>
        /// <param name="_seed"></param>
        /// <param name="_onInferenceRequested"></param>
        private void AskGenerate(string _seed, Action<string> _onInferenceRequested = null)
        {
            Debug.Log("Ask to generate Image button clicked. Model: " + modelName + ", Seed: " + _seed);
            if (IsPromptDataValid(out string inputData))
            {
                InferenceManager.PostAskInferenceRequest(inputData, _onInferenceRequested);
            }
        }

        /// <summary>
        /// Verify data input and launch api request
        /// </summary>
        /// <param name="_seed"></param>
        /// <param name="_onInferenceRequested"></param>
        private void Generate(string _seed, Action<string> _onInferenceRequested = null)
        {
            Debug.Log("Generate Image button clicked. Model: " + modelName + ", Seed: " + _seed);
            if (IsPromptDataValid(out string inputData))
            {
                InferenceManager.PostInferenceRequest(inputData,
                    numberOfImages,
                    promptInput,
                    samplesStep,
                    width,
                    height,
                    guidance,
                    SchedulerOptions[schedulerSelected],
                    seedInput,
                    _onInferenceRequested);
            }
        }

        /// <summary>
        /// Usefull from workflow behaviour. Automatically set mode options 
        /// Used by Isometric WorkFlow
        /// </summary>
        private void ManageOptionMode()
        {
            activeMode = GetActiveMode();
            switch (activeMode.EMode)
            {
                case ECreationMode.ControlNet:
                    activeMode.UseControlNet = true;
                    activeMode.UseAdvanceSettings = true;
                    break;
            }
        }

        /// <summary>
        /// Verify correct data input and complete it
        /// </summary>
        /// <param name="inputData"> Input Data to check and complete </param>
        /// <returns></returns>
        private bool IsPromptDataValid(out string inputData)
        {
            activeMode = GetActiveMode();

            if (activeMode != null)
            {
                string modality = "";
                string operationType = activeMode.OperationName;
                string dataUrl = "\"\"";
                string dataAdditionalUrl = "\"\"";
                string maskDataUrl = "\"\"";

                inputData = "";

                switch (activeMode.EMode)
                {
                    case ECreationMode.Text_To_Image:

                        break;

                    case ECreationMode.Image_To_Image:

                        if (imageUpload == null)
                        {
                            Debug.LogError("Img2Img Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);

                        break;

                    case ECreationMode.ControlNet:

                        if (imageUpload == null)
                        {
                            Debug.LogError("ControlNet Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);

                        break;

                    case ECreationMode.Inpaint:

                        if (imageUpload == null)
                        {
                            Debug.LogError("Inpaint Must have an image uploaded.");
                            return false;
                        }
                        else
                        {
                            dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        }

                        if (maskImage == null)
                        {
                            Debug.LogError("Inpaint Must have a mask uploaded.");
                            return false;
                        }
                        else
                        {
                            maskDataUrl = ProcessMask();
                        }

                        break;

                    case ECreationMode.IP_Adapter:
                        if (imageUpload == null)
                        {
                            Debug.LogError("ControlNet Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        break;

                    case ECreationMode.Reference_Only:
                        if (imageUpload == null)
                        {
                            Debug.LogError("ControlNet Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        break;

                    case ECreationMode.Image_To_Image__ControlNet:

                        if (imageUpload == null || additionalImageUpload == null)
                        {
                            Debug.LogError("Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        dataAdditionalUrl = CommonUtils.Texture2DToDataURL(additionalImageUpload);

                        break;

                    /*case ECreationMode.Reference_Only__Control_Net:

                        if (imageUpload == null || additionalImageUpload == null)
                        {
                            Debug.LogError("Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        dataAdditionalUrl = CommonUtils.Texture2DToDataURL(additionalImageUpload);

                        break;*/

                    case ECreationMode.Image_To_Image__IP_Adapter:
                        if (imageUpload == null || additionalImageUpload == null)
                        {
                            Debug.LogError("Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        dataAdditionalUrl = CommonUtils.Texture2DToDataURL(additionalImageUpload);
                        break;

                    case ECreationMode.ControlNet__IP_Adapter:

                        if (imageUpload == null || additionalImageUpload == null)
                        {
                            Debug.LogError("Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        dataAdditionalUrl = CommonUtils.Texture2DToDataURL(additionalImageUpload);

                        break;
                }

                Dictionary<string, string> modalitySettings = PrepareModalitySettings(ref modality, ref operationType);

                modality = PrepareModality(modalitySettings);

                inputData = PrepareInputData(modality, operationType, dataUrl, dataAdditionalUrl, maskDataUrl);
                //Debug.Log("Input Data: " + inputData);
                return true;
            }
            else
            {
                Debug.LogError("Issue with mode selected");
                inputData = null;
                return false;
            }
        }

        /// <summary>
        /// Prepare control net modalities values
        /// </summary>
        /// <param name="modality"></param>
        /// <param name="operationType"></param>
        /// <returns></returns>
        private Dictionary<string, string> PrepareModalitySettings(ref string modality, ref string operationType)
        {
            Dictionary<string, string> modalitySettings = new();

            if (activeMode.UseControlNet)
            {
                PrepareAdvancedModalitySettings(out modality, modalitySettings);
            }

            return modalitySettings;
        }

        /// <summary>
        /// Prepare control net modalities values
        /// </summary>
        /// <param name="modalitySettings"></param>
        /// <returns></returns>
        private string PrepareModality(Dictionary<string, string> modalitySettings)
        {
            string modality;
            if (activeMode.UseControlNet && activeMode.UseAdvanceSettings)
            {
                modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
            }
            else
            {
                modality = selectedPreset;
            }

            return modality;
        }

        /// <summary>
        /// Prepare control net modalities values if it's advance settings activate
        /// </summary>
        /// <param name="modality"></param>
        /// <param name="modalitySettings"></param>
        private void PrepareAdvancedModalitySettings(out string modality, /*out string operationType,*/ Dictionary<string, string> modalitySettings)
        {
            if (modalitySelected > 0)
            {
                modalityName = CorrespondingOptionsValue[modalitySelected - 1];
                if (!modalitySettings.ContainsKey(modalityName))
                {
                    modalitySettings.Add(modalityName, $"{modalityValue:0.00}");
                }
            }
            
            modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
        }

        /// <summary>
        /// Process generation of a mask
        /// </summary>
        /// <returns></returns>
        private string ProcessMask()
        {
            Texture2D processedMask = Texture2D.Instantiate(maskImage);

            Color[] pixels = processedMask.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0)
                {
                    pixels[i] = Color.black;
                }
            }

            processedMask.SetPixels(pixels);
            processedMask.Apply();

            return CommonUtils.Texture2DToDataURL(processedMask);
        }

        /// <summary>
        /// Prepare the whole data input before launching api request
        /// </summary>
        /// <param name="modality"></param>
        /// <param name="operationType"></param>
        /// <param name="dataUrl"></param>
        /// <param name="_additionalDataUrl"></param>
        /// <param name="maskDataUrl"></param>
        /// <returns></returns>
        private string PrepareInputData(string modality, string operationType, string dataUrl, string _additionalDataUrl, string maskDataUrl)
{
    // Ensure modelId is a string
    bool hideResults = false;
    string modelId = DataCache.instance.SelectedModelId;
    if (modelId == null)
    {
        modelId = ""; // Or handle the null case appropriately
    }

    // Start building the JSON payload as a string
    string inputData = $@"{{
        ""modelId"": ""{modelId}"",
        ""hideResults"": {hideResults.ToString().ToLower()},
        ""type"": ""{operationType}"",
        ""dryRun"": true,
        ""prompt"": ""{promptInput}"","; // Include prompt here

    // Add other parameters based on the active mode
    switch (activeMode.EMode)
    {
        case ECreationMode.Image_To_Image:
            if (activeMode.IsControlNet)
            {
                inputData += $@"""image"": ""{dataUrl}"",";
                inputData += $@"""strength"": {(100 - influenceOption) * 0.01f},"; // Strength for img2img
            }
            break;

        case ECreationMode.Inpaint:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""mask"": ""{maskDataUrl}"",";
            inputData += $@"""strength"": {(100 - influenceOption) * 0.01f},"; // Strength for inpaint
            break;

        case ECreationMode.IP_Adapter:
            inputData += $@"""ipAdapterImage"": ""{dataUrl}"",";
            inputData += $@"""ipAdapterScale"": {additionalModalityValue},"; // Scale for IP_Adapter
            break;

        case ECreationMode.Reference_Only:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""styleFidelity"": {additionalModalityValue},"; // Fidelity for Reference_Only
            inputData += $@"""referenceAdain"": {activeMode.AdditionalSettings["Reference AdaIN"].ToString().ToLower()},";
            inputData += $@"""referenceAttn"": {activeMode.AdditionalSettings["Reference Attn"].ToString().ToLower()},";
            break;

        case ECreationMode.ControlNet:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""modality"": ""{modality}"",";
            break;

        case ECreationMode.ControlNet__IP_Adapter:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""modality"": ""{modality}"",";
            inputData += $@"""ipAdapterImage"": ""{_additionalDataUrl}"",";
            inputData += $@"""ipAdapterScale"": {additionalModalityValue},"; // Scale for IP_Adapter
            break;

        case ECreationMode.Image_To_Image__ControlNet:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""strength"": {(100 - influenceOption) * 0.01f},"; // Strength for img2img
            inputData += $@"""controlImage"": ""{_additionalDataUrl}"",";
            inputData += $@"""modality"": ""{modality}"",";
            break;

        case ECreationMode.Image_To_Image__IP_Adapter:
            inputData += $@"""image"": ""{dataUrl}"",";
            inputData += $@"""strength"": {(100 - influenceOption) * 0.01f},"; // Strength for img2img
            inputData += $@"""ipAdapterImage"": ""{_additionalDataUrl}"",";
            inputData += $@"""ipAdapterScale"": {additionalModalityValue},"; // Scale for IP_Adapter
            break;

        // Add a case for ECreationMode.Reference_Only__Control_Net if needed

        default:
            // Handle unknown or unsupported modes
            break;
    }

    // Add common parameters
    if (seedInput!= "-1")
    {
        inputData += $@"""seed"": {ulong.Parse(seedInput)},";
    }
    inputData += $@"""negativePrompt"": ""{promptNegativeInput}"",
        ""guidance"": {guidance.ToString("F2", CultureInfo.InvariantCulture)},
        ""numInferenceSteps"": {samplesStep},
        ""width"": {width},
        ""height"": {height},
        ""numSamples"": {numberOfImages}
        {(schedulerSelected > 0? $@",""scheduler"": ""{SchedulerOptions[schedulerSelected]}""": "")}
    }}";

    return inputData;
}

        /// <summary>
        /// Create all mode available to generate image
        /// </summary>
        private void InitCreationMode()
        {
            if (creationModeList != null && creationModeList.Count == 0)
            {
                foreach (ECreationMode e in Enum.GetValues(typeof(ECreationMode)))
                {
                    CreationMode mode = new CreationMode(e.ToString("G").Replace("__", " + ").Replace("_", " "), e);

                    switch (e)
                    {
                        case ECreationMode.Text_To_Image:
                            mode.IsControlNet = false;
                            mode.OperationName = "txt2img";
                            break;

                        case ECreationMode.Image_To_Image:
                            mode.IsControlNet = true;
                            mode.OperationName = "img2img";
                            break;

                        case ECreationMode.Inpaint:
                            mode.IsControlNet = true;
                            mode.OperationName = "inpaint";
                            break;

                        case ECreationMode.IP_Adapter:
                            mode.IsControlNet = true;
                            mode.OperationName = "txt2img_ip_adapter";
                            break;

                        case ECreationMode.Reference_Only:
                            mode.IsControlNet = true;
                            mode.OperationName = "reference";
                            break;

                        case ECreationMode.ControlNet:
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet";
                            break;

                        case ECreationMode.ControlNet__IP_Adapter:
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet_ip_adapter";
                            break;

                        case ECreationMode.Image_To_Image__ControlNet:
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet_img2img";
                            break;

                        case ECreationMode.Image_To_Image__IP_Adapter:
                            mode.IsControlNet = true;
                            mode.OperationName = "img2img_ip_adapter";
                            break;

                        /*case ECreationMode.Reference_Only__Control_Net:
                            mode.IsControlNet = true;
                            mode.OperationName = "reference_controlnet";
                            break;*/ //Not Available now

                    }

                    creationModeList.Add(mode);
                }
            }
        }

        /// <summary>
        /// Check if the list of creation mode is set.
        /// </summary>
        /// <returns></returns>
        private bool CheckListCreationMode()
        {
            if (creationModeList != null && creationModeList.Count > 0)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
