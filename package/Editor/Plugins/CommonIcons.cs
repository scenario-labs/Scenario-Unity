using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    [ExecuteInEditMode]
    [InitializeOnLoad]
    public class CommonIcons
    {
        public static Dictionary<Icon, Texture2D> iconTextures = new Dictionary<Icon, Texture2D>();

        static CommonIcons()
        {
            LoadIcons();
        }

        /// <summary>
        /// Get icons when they're loaded.
        /// </summary>
        /// <param name="_iconName"> Name of the icon to get </param>
        /// <returns></returns>
        public static Texture2D GetIcon(Icon _iconName)
        {
            if (iconTextures.ContainsKey(_iconName))
            {
                if (iconTextures[_iconName] != null)
                {
                    return iconTextures[_iconName];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Load icons of the plugin.
        /// </summary>
        static void LoadIcons()
        {
            var icons = Enum.GetValues(typeof(Icon));

            foreach (Icon icon in icons)
            {
                string path = CommonUtils.PluginFolderPath() + "Assets/Icons/" + icon.ToString() + ".png";
                if (AssetDatabase.LoadMainAssetAtPath(path))
                {
                    LoadIcon(icon, path);
                }
                else
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(WaitToAssetToBeLoaded());
                }
            }
        }

        /// <summary>
        /// Process to load one icon.
        /// </summary>
        /// <param name="_icon"> Icon to load </param>
        /// <param name="_path"> Path of the icon </param>
        /// <exception cref="Exception"></exception>
        static void LoadIcon(Icon _icon, string _path)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(_path);
            if (tex == null)
            {
                throw new Exception($"There is no texture at {_path}");
            }
            else
            {
                if (!iconTextures.ContainsKey(_icon))
                {
                    iconTextures.Add(_icon, tex);
                }
                else
                {
                    iconTextures[_icon] = tex;
                }
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

                LoadIcon(icon, path);
            }
            yield return null;
        }

        public enum Icon
        {
            wastebasket,
        }
    }
}
