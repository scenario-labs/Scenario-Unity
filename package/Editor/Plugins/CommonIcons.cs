using System;
using System.Collections;
using System.Collections.Generic;
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

        static void LoadIcons()
        {
            var icons = Enum.GetValues(typeof(Icon));

            foreach (Icon icon in icons)
            {
                string path = CommonUtils.PluginFolderPath() + "Assets/Icons/" + icon.ToString() + ".png";
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                {
                    throw new Exception($"There is no texture at {path}");
                }
                else
                {
                    iconTextures.Add(icon, tex);
                }

            }
        }



        public enum Icon
        {
            wastebasket,
        }
    }
}
