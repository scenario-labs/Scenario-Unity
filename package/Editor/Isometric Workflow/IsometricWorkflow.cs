using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Scenario.Editor.Models;

namespace Scenario.Editor
{
    public class IsometricWorkflow : EditorWindow
    {
        /// <summary>
        /// Static field that contains all the UI functions
        /// </summary>
        private static readonly IsometricWorkflowUI isometricWorkflowUI = new();

        /// <summary>
        /// is TRUE when the window is visible. FALSE otherwise
        /// </summary>
        private static bool isVisible = false;

        /// <summary>
        /// The isometric workflow is made of multiples steps. This field contains the current step
        /// </summary>
        internal Step currentStep;

        /// <summary>
        /// The first step of the workflow is to select a base. This field contains the base that the user has choosen
        /// </summary>
        internal Base selectedBase = Base.None;


        /// <summary>
        /// The second step of the workflow is to select a Style. This field contains the model that match the style that the user has choosen
        /// </summary>
        internal ModelStyle selectedModel = ModelStyle.lora1;


        /// <summary>
        /// The third step of the workflow is to select a theme. This field contains the theme that match the theme that the user has choosen
        /// </summary>
        internal Theme selectedTheme = Theme.None;


        /// <summary>
        /// The fourth step of the workflow is to create a list of asset name. 
        /// </summary>
        internal List<string> assetList = new List<string>();

        /// <summary>
        /// Foreach asset, 4 images are generated. the use will be able to select one texture per asset. This is the Dictionary that contains, foreach asset, the current selected texture
        /// First value is the assetName, second value is the id of the selected image
        /// </summary>
        internal Dictionary<string, string> selectedImages = new Dictionary<string, string>();

        internal static IsometricWorkflowSettings settings;


        [MenuItem("Window/Scenario/Workflows/1. Isometric Workflow")]
        public static void ShowWindow()
        {
            if (isVisible)
                return;

            GetWindow(typeof(IsometricWorkflow));

            settings = IsometricWorkflowSettings.GetSerializedSettings();
        }

        /// <summary>
        /// Auto add some asset name as an example
        /// </summary>
        /// <returns></returns>
        public void FillAssetSamples()
        {
            List<string> samples = new List<string>();
            samples.Add("Tavern");
            samples.Add("Hospital");
            samples.Add("Police Station");
            samples.Add("Rocket Launcher");
            samples.Add("Factory");
            samples.Add("Treehouse");
            samples.Add("Arena");
            samples.Add("Temple");
            samples.Add("Church");
            samples.Add("Building Block");
            assetList.AddRange(samples);
        }

        private void CreateGUI()
        {
            var isometricWorkflow = (IsometricWorkflow)GetWindow(typeof(IsometricWorkflow));
            isometricWorkflowUI.Init(isometricWorkflow);
        }

        public void GenerateImage()
        {
        }



        private void OnGUI()
        {
            switch (currentStep)
            {
                case Step.Base:
                    isometricWorkflowUI.DrawBaseGUI(this.position);
                    break;
                case Step.Style:
                    isometricWorkflowUI.DrawStyleGUI(this.position);
                    break;
                case Step.Theme:
                    isometricWorkflowUI.DrawThemeGUI(this.position);
                    break;
                case Step.Asset:
                    isometricWorkflowUI.DrawAssetsGUI(this.position);
                    break;
                case Step.Validation:
                    isometricWorkflowUI.DrawValidationGUI(this.position);
                    break;
                default:
                    break;
            }
        }

        private void OnDestroy()
        {

        }

        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
        }

        public enum ModelStyle
        {
            lora1,
            lora2,
            lora3,
            lora4,
            lora5,
            lora6,
        }


        public enum Theme
        {
            None,
            Medieval,
            Futuristic,
            Comtemporary,
            Ancient,
            MagicalForest,
            WorldWar,
        }


        public enum Step
        {
            Base = 0,
            Style = 1,
            Theme = 2,
            Asset = 3,
            Validation = 4,
        }

        public enum Base
        {
            None = 0,
            Square = 1,
            Custom = 2,
        }
    }
}
