using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    /// <summary>
    /// This class provides custom context menu actions for the Unity editor's Scene view.
    /// When a user right-clicks (with Ctrl key pressed) on a selected GameObject in the Scene view,
    /// a context menu appears, offering various layer manipulation options such as moving,
    /// cloning, deleting, flipping, and setting background images.
    /// </summary>
    public class SceneContextMenuActions
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// Handles the Scene GUI events. Detects right-click with Ctrl key pressed and shows the context menu if a GameObject is selected.
        /// </summary>
        /// <param name="sceneView">The current SceneView.</param>
        private static void OnSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseDown && current.button == 1 && current.control)
            {
                if (Selection.activeGameObject != null)
                {
                    ShowContextMenu();
                    current.Use();
                }
            }
        }

        /// <summary>
        /// Displays the custom context menu with various layer manipulation options.
        /// </summary>
        private static void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Move Up"), false, MoveLayerUp);
            menu.AddItem(new GUIContent("Move Down"), false, MoveLayerDown);
            menu.AddItem(new GUIContent("Clone"), false, CloneLayer);
            menu.AddItem(new GUIContent("Delete"), false, DeleteLayer);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Flip/Horizontal"), false, FlipHorizontal);
            menu.AddItem(new GUIContent("Flip/Vertical"), false, FlipVertical);
            menu.AddItem(new GUIContent("Remove/Background"), false, RemoveBackground);
            menu.AddItem(new GUIContent("Set As Background"), false, SetAsBackground);

            menu.ShowAsContext();
        }

        /// <summary>
        /// Moves the selected layer up in the hierarchy.
        /// </summary>
        private static void MoveLayerUp()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            int siblingIndex = selectedLayer.transform.GetSiblingIndex();
            selectedLayer.transform.SetSiblingIndex(siblingIndex + 1);
        }

        /// <summary>
        /// Moves the selected layer down in the hierarchy.
        /// Ensures the new sibling index is within valid bounds to avoid errors.
        /// </summary>
        private static void MoveLayerDown()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            int siblingIndex = selectedLayer.transform.GetSiblingIndex();
            if (siblingIndex > 0)
            {
                selectedLayer.transform.SetSiblingIndex(siblingIndex - 1);
            }
        }

        /// <summary>
        /// Clones the selected layer.
        /// </summary>
        private static void CloneLayer()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            GameObject clonedLayer = UnityEngine.Object.Instantiate(selectedLayer);
            clonedLayer.name = selectedLayer.name + " (Clone)";
        }

        /// <summary>
        /// Deletes the selected layer.
        /// </summary>
        private static void DeleteLayer()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            UnityEngine.Object.DestroyImmediate(selectedLayer);
        }

        /// <summary>
        /// Flips the selected layer horizontally.
        /// </summary>
        private static void FlipHorizontal()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }

        /// <summary>
        /// Flips the selected layer vertically.
        /// </summary>
        private static void FlipVertical()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipY = !spriteRenderer.flipY;
            }
        }

        /// <summary>
        /// Removes the background of the selected layer's sprite by sending it to an external API.
        /// </summary>
        private static void RemoveBackground()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Texture2D texture2D = spriteRenderer.sprite.texture;

                string texturePath = AssetDatabase.GetAssetPath(texture2D);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                Texture2D uncompressedTexture = new Texture2D(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
                uncompressedTexture.SetPixels(texture2D.GetPixels());
                uncompressedTexture.Apply();

                string dataUrl = CommonUtils.Texture2DToDataURL(uncompressedTexture);
                string name = CommonUtils.GetRandomImageFileName();
                string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{name}\",\"format\":\"png\",\"returnImage\":\"false\"}}";
                Debug.Log(param);

                ApiClient.RestPut("images/erase-background", param, response =>
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                    string imageUrl = jsonResponse.asset.url;
                    CommonUtils.FetchTextureFromURL(imageUrl, texture =>
                    {
                        // Update the sprite directly in Unity
                        CommonUtils.ReplaceSprite(spriteRenderer, texture);
                    });
                });
            }
        }

        /// <summary>
        /// Sets the selected layer as the background by moving it to the first position in the hierarchy.
        /// </summary>
        private static void SetAsBackground()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            selectedLayer.transform.SetAsFirstSibling();
        }
    }
}
