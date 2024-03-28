using Codice.CM.Common;
using System;
using System.Collections.Generic;
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
        /// 
        /// </summary>
        internal string modelName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        internal List<CreationMode> creationModeList = new List<CreationMode>();

        /// <summary>
        /// 
        /// </summary>
        internal int numberOfImages = 4;

        /// <summary>
        /// 
        /// </summary>
        internal string promptInput = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        internal string promptNegativeInput = string.Empty;

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        internal int width = 1024;

        /// <summary>
        /// 
        /// </summary>
        internal int height = 1024;

        /// <summary>
        /// 
        /// </summary>
        internal int selectedOption = -1;

        /// <summary>
        /// 
        /// </summary>
        internal float influenceOption = 0.25f;

        /// <summary>
        /// 
        /// </summary>
        internal Texture2D imageUpload = null;

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

            CreationMode activeMode = GetActiveMode();

            switch (activeMode.EMode)
            {
                case ECreationMode.None:
                    break;

                case ECreationMode.Text_To_Image:
                    break;

                case ECreationMode.Image_To_Image:
                    break;

                case ECreationMode.Control_Net:
                    break;

                case ECreationMode.In_Painting:
                    break;

                case ECreationMode.Ip_Adapter:
                    break;

                case ECreationMode.Reference_Only:
                    break;

                case ECreationMode.Image_To_Image__Control_Net:
                    break;

                case ECreationMode.Reference_Only__Control_Net:
                    break;

                case ECreationMode.Image_To_Image__Ip_Adapter:
                    break;

                case ECreationMode.Control_Net__Ip_Adapter:
                    break;
            }

            /*if (promptWindowUI.isImageToImage)
            {
                operationType = "img2img";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("Img2Img Must have a image uploaded.");
                    return false;
                }

                dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
            }
            else if (promptWindowUI.isControlNet)
            {
                operationType = "controlnet";

                if (PromptWindowUI.imageUpload == null)
                {
                    Debug.LogError("ControlNet Must have a image uploaded.");
                    return false;
                }

                dataUrl = CommonUtils.Texture2DToDataURL(PromptWindowUI.imageUpload);
            }

            if (promptWindowUI.isImageToImage && promptWindowUI.isControlNet)
            {
                operationType = "controlnet";
            }

            Dictionary<string, string> modalitySettings = PrepareModalitySettings(ref modality, ref operationType);

            modality = PrepareModality(modalitySettings);

            if (promptWindowUI.isInpainting)
            {
                operationType = "inpaint";

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
            }

            inputData = PrepareInputData(modality, operationType, dataUrl, maskDataUrl);
            Debug.Log("Input Data: " + inputData);
            */
            return true;
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
