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
        Control_Net,
        In_Painting,
        Ip_Adapter,
        Reference_Only,
        Image_To_Image__Control_Net,
        //Reference_Only__Control_Net,
        Image_To_Image__Ip_Adapter,
        Control_Net__Ip_Adapter
    }

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
        internal int samplesStep = 30;

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
        /// Re launch the generation process
        /// </summary>
        public void ReGenerateImage()
        { 
            
        }

        #endregion

        #region Private Methods

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
                PromptFetcher.PostInferenceRequest(inputData,
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

                    case ECreationMode.Control_Net:

                        if (imageUpload == null)
                        {
                            Debug.LogError("ControlNet Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);

                        break;

                    case ECreationMode.In_Painting:

                        if (imageUpload == null)
                        {
                            Debug.LogError("Inpainting Must have an image uploaded.");
                            return false;
                        }
                        else
                        {
                            dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        }

                        if (maskImage == null)
                        {
                            Debug.LogError("Inpainting Must have a mask uploaded.");
                            return false;
                        }
                        else
                        {
                            maskDataUrl = ProcessMask();
                        }

                        break;

                    case ECreationMode.Ip_Adapter:
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

                    case ECreationMode.Image_To_Image__Control_Net:

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

                    case ECreationMode.Image_To_Image__Ip_Adapter:
                        if (imageUpload == null || additionalImageUpload == null)
                        {
                            Debug.LogError("Must have a image uploaded.");
                            return false;
                        }

                        dataUrl = CommonUtils.Texture2DToDataURL(imageUpload);
                        dataAdditionalUrl = CommonUtils.Texture2DToDataURL(additionalImageUpload);
                        break;

                    case ECreationMode.Control_Net__Ip_Adapter:

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
                Debug.Log("Input Data: " + inputData);
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
        private static string ProcessMask()
        {
            Texture2D processedMask = Texture2D.Instantiate(PromptWindowUI.imageMask);

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
            bool hideResults = false;
            string type = operationType;
            string mask = $"\"{maskDataUrl}\"";
            string prompt = promptInput;
            string seedField = "";
            string image = $"\"{dataUrl}\"";
            string controlImage = $"\"{_additionalDataUrl}\""; // controlImageId || ipAdapterImage and ipAdapterImageId

            if (seedInput != "-1")
            {
                ulong seed = ulong.Parse(seedInput);
                seedField = $@"""seed"": {seed},";
            }

            string negativePrompt = promptNegativeInput;
            float strength = Mathf.Clamp((float)Math.Round((100 - influenceOption) * 0.01f, 2), 0.01f, 1f); //strength is 100-influence (and between 0.01 & 1) // not used and usefull
            float guidance = this.guidance;
            int width = this.width;
            int height = this.height;
            int numInferenceSteps = samplesStep;
            int numSamples = numberOfImages;
            string scheduler = SchedulerOptions[schedulerSelected];
            float addModality = additionalModalityValue;

            string inputData = $@"{{
                ""parameters"": {{
                    ""hideResults"": {hideResults.ToString().ToLower()},
                    ""type"": ""{type}"",";

            switch (activeMode.EMode)
            {
                case ECreationMode.Text_To_Image:

                    break;

                case ECreationMode.Image_To_Image:
                    if (activeMode.IsControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    break;

                case ECreationMode.In_Painting:
                    if (activeMode.IsControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""mask"": ""{maskDataUrl}"",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""mask"": "",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    break;

                case ECreationMode.Ip_Adapter: // image ref ipAdapterScale
                    if (activeMode.IsControlNet)
                    {
                        inputData += $@"""ipAdapterImage"": ""{dataUrl}"",";
                        inputData += $@"""ipAdapterScale"": {addModality.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    else
                    {
                        inputData += $@"""ipAdapterImage"": "",";
                        inputData += $@"""ipAdapterScale"": "",";
                    }
                    break;

                case ECreationMode.Reference_Only: //image ref styleFidelity
                    if (activeMode.IsControlNet)
                    {
                        Debug.Log(addModality.ToString("F2", CultureInfo.InvariantCulture));
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""styleFidelity"": {addModality.ToString("F2", CultureInfo.InvariantCulture)},";
                        inputData += $@"""referenceAdain"": {activeMode.AdditionalSettings["Reference AdaIN"].ToString().ToLower()},";
                        inputData += $@"""referenceAttn"": {activeMode.AdditionalSettings["Reference Attn"].ToString().ToLower()},";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""styleFidelity"": "",";
                        inputData += $@"""referenceAdain"": """",";
                        inputData += $@"""referenceAttn"": """",";
                    }
                    break;

                case ECreationMode.Control_Net:
                    if (activeMode.IsControlNet && activeMode.UseControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""modality"": ""{modality}"",";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""modality"": "",";
                    }
                    break;

                case ECreationMode.Control_Net__Ip_Adapter: // double ref image and modality on second
                    if (activeMode.IsControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""modality"": ""{modality}"",";
                        inputData += $@"""ipAdapterImage"": ""{_additionalDataUrl}"",";
                        inputData += $@"""ipAdapterScale"": {addModality.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    else
                    {
                        inputData += $@"""image"": """;
                        inputData += $@"""modality"": """;
                        inputData += $@"""ipAdapterImage"": "",";
                        inputData += $@"""ipAdapterScale"": "",";
                    }
                    break;

                case ECreationMode.Image_To_Image__Control_Net: // double ref image and modality on second
                    if (activeMode.IsControlNet && activeMode.UseControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                        inputData += $@"""controlImage"": ""{_additionalDataUrl}"",";
                        inputData += $@"""modality"": ""{modality}"",";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                        inputData += $@"""controlImage"": "",";
                        inputData += $@"""modality"": "",";
                    }
                    break;

                case ECreationMode.Image_To_Image__Ip_Adapter: // double ref image and influence on second ipAdapterScale: 0.75
                    if (activeMode.IsControlNet)
                    {
                        inputData += $@"""image"": ""{dataUrl}"",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                        inputData += $@"""ipAdapterImage"": ""{_additionalDataUrl}"",";
                        inputData += $@"""ipAdapterScale"": {addModality.ToString("F2", CultureInfo.InvariantCulture)},";
                    }
                    else
                    {
                        inputData += $@"""image"": "",";
                        inputData += $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)},";
                        inputData += $@"""ipAdapterImage"": "",";
                        inputData += $@"""ipAdapterScale"": "",";
                    }
                    break;

                    /*case ECreationMode.Reference_Only__Control_Net:
                        mode.IsControlNet = true;
                        mode.OperationName = "reference_controlnet";
                        break;*/ //Not Available now
            }

            inputData += $@"""prompt"": ""{prompt}"",
                    {seedField}
                    {(string.IsNullOrEmpty(negativePrompt) ? "" : $@"""negativePrompt"": ""{negativePrompt}"",")}
                    ""guidance"": {guidance.ToString("F2", CultureInfo.InvariantCulture)},
                    ""numInferenceSteps"": {numInferenceSteps},
                    ""width"": {width},
                    ""height"": {height},
                    ""numSamples"": {numSamples}
                    {(scheduler != "Default" ? $@",""scheduler"": ""{scheduler}""" : "")}
                }}
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

                        case ECreationMode.In_Painting:
                            mode.IsControlNet = true;
                            mode.OperationName = "inpaint";
                            break;

                        case ECreationMode.Ip_Adapter: // image ref ipAdapterScale
                            mode.IsControlNet = true;
                            mode.OperationName = "txt2img_ip_adapter";
                            break;

                        case ECreationMode.Reference_Only: //image ref styleFidelity
                            mode.IsControlNet = true;
                            mode.OperationName = "reference";
                            break;

                        case ECreationMode.Control_Net:
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet";
                            break;

                        case ECreationMode.Control_Net__Ip_Adapter: // double ref image and modality on second
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet_ip_adapter";
                            break;

                        case ECreationMode.Image_To_Image__Control_Net: // double ref image and modality on second
                            mode.IsControlNet = true;
                            mode.OperationName = "controlnet_img2img";
                            break;

                        case ECreationMode.Image_To_Image__Ip_Adapter: // double ref image and influence on second ipAdapterScale: 0.75
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
