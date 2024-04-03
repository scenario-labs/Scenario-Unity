using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scenario.Editor
{
    public class CreationMode
    {
        #region Public Fields

        public string ModeName { get { return modeName; } }
        public ECreationMode EMode { get { return eMode; } }
        public bool IsActive { get { return isActive; } set { isActive = value; } }
        public string OperationName { get { return operationName; } set { operationName = value; } }
        public bool IsControlNet { get { return isControlNet; } set { isControlNet = value; } }
        public bool UseControlNet { get { return useControlNet; } set { useControlNet = value; } }
        public bool UseAdvanceSettings { get { return useAdvanceSettings; } set { useAdvanceSettings = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// 
        /// </summary>
        private string modeName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private ECreationMode eMode = ECreationMode.Text_To_Image;

        /// <summary>
        /// 
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// 
        /// </summary>
        private string operationName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private bool isControlNet = false;

        /// <summary>
        /// 
        /// </summary>
        private bool useControlNet = false;

        /// <summary>
        /// 
        /// </summary>
        private bool useAdvanceSettings = false;

        #endregion

        #region Editor Callbacks

        public CreationMode(string _modeName) 
        {
            modeName = _modeName;
            eMode = ECreationMode.Text_To_Image;
            isActive = false;
        }

        public CreationMode(string _modeName, ECreationMode _eMode, bool _isActive = false, bool _useControlNet = false) 
        {
            modeName= _modeName;
            eMode = _eMode;
            isActive = _isActive;
            useControlNet = _useControlNet;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
