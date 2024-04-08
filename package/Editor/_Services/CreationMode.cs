using System.Collections.Generic;

namespace Scenario.Editor
{
    /// <summary>
    /// Creation Mode Class is a way to create variable generation mode.
    /// </summary>
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
        public Dictionary<string, bool> AdditionalSettings { get { return additionalSettings; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Name of the creation mode created
        /// </summary>
        private string modeName = string.Empty;

        /// <summary>
        /// Type mode of the creation mode
        /// </summary>
        private ECreationMode eMode = ECreationMode.Text_To_Image;

        /// <summary>
        /// If this creation mode is active or not
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// Api request operation name
        /// </summary>
        private string operationName = string.Empty;

        /// <summary>
        /// If it's a control net mode // May have to be rename ?
        /// </summary>
        private bool isControlNet = false;

        /// <summary>
        /// If this creation mode use a control net option
        /// </summary>
        private bool useControlNet = false;

        /// <summary>
        /// If this mode has advance settings and use it
        /// </summary>
        private bool useAdvanceSettings = false;

        /// <summary>
        /// Stock all additional settings value inside it to get it to api request
        /// </summary>
        private Dictionary<string, bool> additionalSettings = new Dictionary<string, bool>();

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
