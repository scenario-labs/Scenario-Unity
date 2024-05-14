using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    /// <summary>
    /// 
    /// </summary>
    [ExecuteInEditMode]
    public class CommonGraphics
    {
        #region Public Fields

        /// <summary>
        /// 
        /// </summary>
        public static List<Shader> Shaders = new List<Shader>();

        /// <summary>
        /// 
        /// </summary>
        public static List<Material> Materials = new List<Material>();

        #endregion

        #region Private Fields

        /// <summary>
        /// 
        /// </summary>
        private static DirectoryInfo directory = null;

        /// <summary>
        /// 
        /// </summary>
        private static List<FileInfo> filesInfo = new List<FileInfo>();

        #endregion

        static CommonGraphics()
        {
            LoadStandardShaders();
            LoadStandardMaterials();
        }

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_materialName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Material GetMaterial(string _materialName)
        {
            if (Materials != null && Materials.Count > 0)
            {
                foreach (Material mat in Materials)
                {
                    if (mat.name.Equals(_materialName))
                    { 
                        return mat;
                    }
                }

                throw new Exception($"No material {_materialName} loaded");
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_shaderName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Shader GetShader(string _shaderName)
        {
            if (Shaders != null && Shaders.Count > 0)
            {
                foreach (Shader shade in Shaders)
                {
                    if (shade.name.Equals(_shaderName))
                    {
                        return shade;
                    }
                }

                throw new Exception($"No shader {_shaderName} loaded");
            }
            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        static void LoadStandardShaders()
        {
            directory = new DirectoryInfo(CommonUtils.PluginFolderPath() + "Assets/Built-in/Shaders/");

            filesInfo = directory.GetFiles().ToList();

            Shaders.Clear();

            foreach (FileInfo info in filesInfo)
            {
                if (!info.Extension.Equals(".meta"))
                {
                    LoadStandardShader(CommonUtils.PluginFolderPath() + "Assets/Built-in/Shaders/" + info.Name);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_path"></param>
        /// <exception cref="Exception"></exception>
        static void LoadStandardShader(string _path)
        {
            Shader shade = AssetDatabase.LoadAssetAtPath<Shader>(_path);

            if (shade != null)
            {
                Shaders.Add(shade);
            }
            else
            {
                throw new Exception($"There is no shader {_path}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void LoadStandardMaterials()
        {
            directory = new DirectoryInfo(CommonUtils.PluginFolderPath() + "Assets/Built-in/Materials/");

            filesInfo = directory.GetFiles().ToList();

            Materials.Clear();

            foreach (FileInfo info in filesInfo) 
            {
                if (!info.Extension.Equals(".meta"))
                { 
                    LoadStandardMaterial(CommonUtils.PluginFolderPath() + "Assets/Built-in/Materials/" + info.Name);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_path"></param>
        /// <exception cref="Exception"></exception>
        static void LoadStandardMaterial(string _path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(_path);

            if (mat != null)
            {
                Materials.Add(mat);
            }
            else
            {
                throw new Exception($"There is no material {_path}");
            }
        }

        /// <summary>
        /// Coroutine to wait assets to be ready to load it
        /// </summary>
        /// <returns></returns>
        static IEnumerator WaitToAssetToBeLoaded()
        {
            var icons = Enum.GetValues(typeof(Icon));

            foreach (Icon icon in icons)
            {
                string path = CommonUtils.PluginFolderPath() + "Assets/Icons/" + icon.ToString() + ".png";
                if (!AssetDatabase.LoadMainAssetAtPath(path))
                {
                    yield break;
                }

                //LoadIcon(icon, path);
            }
            yield return null;
        }

        #endregion
    }
}
