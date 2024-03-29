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
        None,
        Text_To_Image,
        Image_To_Image,
        Control_Net,
        In_Painting,
        Ip_Adapter,
        Reference_Only,
        Image_To_Image__Control_Net,
        Reference_Only__Control_Net,
        Image_To_Image__Ip_Adapter,
        Control_Net__Ip_Adapter
    }

    [ExecuteInEditMode]
    [InitializeOnLoad]
    public class PromptPusher
    {
        #region Public Fields

        public static PromptPusher Instance = null;

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
        /// 
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
        /// Reference scheluder selected.
        /// </summary>
        internal string schedulerSelected = string.Empty;

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

        public void Init()
        {
            InitCreationMode();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_mode"></param>
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
        /// 
        /// </summary>
        /// <param name="_mode"></param>
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
        /// 
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

            return GetMode(ECreationMode.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_mode"></param>
        /// <param name="_isActive"></param>
        public void SetMode(ECreationMode _mode, bool _isActive)
        {
            if (CheckListCreationMode())
            { 
                GetMode(_mode).IsActive = _isActive;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_mode"></param>
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
        /// 
        /// </summary>
        public void GenerateImage()
        { 
            
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReGenerateImage()
        { 
            
        }

        #endregion

        #region Private Methods

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
                    seedInput/*,
                    _onInferenceRequested*/);
            }
        }

        private bool IsPromptDataValid(out string inputData)
        {
            string modality = "";
            string operationType = "txt2img";
            string dataUrl = "\"\"";
            string maskDataUrl = "\"\"";

            inputData = "";

            activeMode = GetActiveMode();

            switch (activeMode.EMode)
            {
                case ECreationMode.None:
                    break;

                case ECreationMode.Text_To_Image:
                    
                    break;

                case ECreationMode.Image_To_Image:

                    if (imageUpload == null)
                    {
                        Debug.LogError("Img2Img Must have a image uploaded.");
                        return false;
                    }

                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);

                    break;

                case ECreationMode.Control_Net:

                    if (PromptWindowUI.imageUpload == null)
                    {
                        Debug.LogError("ControlNet Must have a image uploaded.");
                        return false;
                    }

                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);

                    break;

                case ECreationMode.In_Painting:

                    if (PromptWindowUI.imageUpload == null)
                    {
                        Debug.LogError("Inpainting Must have an image uploaded.");
                        return false;
                    }
                    else
                    {
                        dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
                    }

                    if (PromptWindowUI.imageMask == null)
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
                    break;

                case ECreationMode.Reference_Only:
                    break;

                case ECreationMode.Image_To_Image__Control_Net:

                    if (PromptWindowUI.imageUpload == null)
                    {
                        Debug.LogError("ControlNet Must have a image uploaded.");
                        return false;
                    }

                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);

                    break;

                case ECreationMode.Reference_Only__Control_Net:

                    if (PromptWindowUI.imageUpload == null)
                    {
                        Debug.LogError("ControlNet Must have a image uploaded.");
                        return false;
                    }

                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);

                    break;

                case ECreationMode.Image_To_Image__Ip_Adapter:
                    break;

                case ECreationMode.Control_Net__Ip_Adapter:

                    if (PromptWindowUI.imageUpload == null)
                    {
                        Debug.LogError("ControlNet Must have a image uploaded.");
                        return false;
                    }

                    dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);

                    break;
            }

            Dictionary<string, string> modalitySettings = PrepareModalitySettings(ref modality, ref operationType);

            modality = PrepareModality(modalitySettings);

            inputData = PrepareInputData(modality, operationType, dataUrl, maskDataUrl);
            Debug.Log("Input Data: " + inputData);
            return true;
        }

        private Dictionary<string, string> PrepareModalitySettings(ref string modality, ref string operationType)
        {
            Dictionary<string, string> modalitySettings = new();

            if (activeMode.HasAdvancedOptions)
            {
                PrepareAdvancedModalitySettings(out modality, modalitySettings);
            }

            return modalitySettings;
        }

        private string PrepareModality(Dictionary<string, string> modalitySettings)
        {
            string modality;
            if (activeMode.OperationName.Contains("Control Net") && activeMode.HasAdvancedOptions)
            {
                modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
            }
            else
            {
                modality = selectedPreset;
            }

            return modality;
        }

        private void PrepareAdvancedModalitySettings(out string modality, /*out string operationType,*/ Dictionary<string, string> modalitySettings)
        {
            if (activeMode.EMode != ECreationMode.None)
            {
                if (!string.IsNullOrEmpty(modalityName))
                {
                    if (!modalitySettings.ContainsKey(modalityName))
                    {
                        modalitySettings.Add(modalityName, $"{modalityValue:0.00}");
                    }
                }
            }
            
            modality = string.Join(",", modalitySettings.Select(kv => $"{kv.Key}:{float.Parse(kv.Value).ToString(CultureInfo.InvariantCulture)}"));
        }

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

        private string PrepareInputData(string modality, string operationType, string dataUrl, string maskDataUrl)
        {
            bool hideResults = false;
            string type = operationType;
            string mask = $"\"{maskDataUrl}\"";
            string prompt = promptInput;
            string seedField = "";
            string image = $"\"{dataUrl}\"";

            if (seedInput != "-1")
            {
                ulong seed = ulong.Parse(seedInput);
                seedField = $@"""seed"": {seed},";
            }

            string negativePrompt = promptNegativeInput;
            float strength = Mathf.Clamp((float)Math.Round((100 - influenceOption) * 0.01f, 2), 0.01f, 1f); //strength is 100-influence (and between 0.01 & 1) // not used and usefull
            float guidance = this.guidance;
            int width = (int)this.width;
            int height = (int)this.height;
            int numInferenceSteps = (int)samplesStep;
            int numSamples = (int)numberOfImages;
            string scheduler = schedulerSelected;

            string inputData = $@"{{
                ""parameters"": {{
                    ""hideResults"": {hideResults.ToString().ToLower()},
                    ""type"": ""{type}"",
                    {/*TODO add other option containing thoses parts*/(activeMode.EMode == ECreationMode.Image_To_Image || activeMode.EMode == ECreationMode.In_Painting || activeMode.EMode == ECreationMode.Control_Net ? $@"""image"": ""{dataUrl}""," : "")}
                    {(activeMode.EMode == ECreationMode.Control_Net || activeMode.HasAdvancedOptions ? $@"""modality"": ""{modality}""," : "")}
                    {(activeMode.EMode == ECreationMode.In_Painting ? $@"""mask"": ""{maskDataUrl}""," : "")}
                    ""prompt"": ""{prompt}"",
                    {seedField}
                    {(string.IsNullOrEmpty(negativePrompt) ? "" : $@"""negativePrompt"": ""{negativePrompt}"",")}
                    {(activeMode.EMode == ECreationMode.Image_To_Image || activeMode.EMode == ECreationMode.Control_Net ? $@"""strength"": {strength.ToString("F2", CultureInfo.InvariantCulture)}," : "")}
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
        /// 
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
                        case ECreationMode.None:
                            mode.OperationName = "txt2img";
                            break;

                        case ECreationMode.Text_To_Image:
                            mode.OperationName = "txt2img";
                            break;

                        case ECreationMode.Image_To_Image:
                            mode.OperationName = "img2img";
                            break;

                        case ECreationMode.In_Painting:
                            mode.OperationName = "inpaint";
                            break;

                        case ECreationMode.Control_Net:
                            mode.HasAdvancedOptions = true;
                            mode.OperationName = "controlnet";
                            break;

                        case ECreationMode.Control_Net__Ip_Adapter:
                            mode.HasAdvancedOptions = true;
                            mode.OperationName = "controlnet";
                            break;

                        case ECreationMode.Image_To_Image__Control_Net:
                            mode.HasAdvancedOptions = true;
                            mode.OperationName = "controlnet";
                            break;

                        case ECreationMode.Reference_Only__Control_Net:
                            mode.HasAdvancedOptions = true;
                            mode.OperationName = "controlnet";
                            break;

                    }

                    creationModeList.Add(mode);
                }
            }
        }

        /// <summary>
        /// 
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
