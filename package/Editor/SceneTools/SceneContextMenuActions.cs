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
    /// cloning, deleting, flipping, cropping, and setting background images.
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
            menu.AddItem(new GUIContent("Crop"), false, CropLayer);

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
            if (spriteRenderer == null)
            {
                Debug.LogError("RemoveBackground: No SpriteRenderer found on the selected GameObject.");
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                Debug.LogError("RemoveBackground: No sprite found on the SpriteRenderer.");
                return;
            }

            Texture2D texture2D = spriteRenderer.sprite.texture;
            if (texture2D == null)
            {
                Debug.LogError("RemoveBackground: No texture found on the sprite.");
                return;
            }

            string texturePath = AssetDatabase.GetAssetPath(texture2D);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (importer != null)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();

                texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture2D == null)
                {
                    Debug.LogError("RemoveBackground: Could not reload the texture after reimporting.");
                    return;
                }
            }
            else
            {
                // Handle runtime-generated textures
                if (!texture2D.isReadable)
                {
                    Debug.LogError("RemoveBackground: The texture is not readable and not an asset texture.");
                    return;
                }
            }

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

        /// <summary>
        /// Sets the selected layer as the background by moving it to the first position in the hierarchy.
        /// </summary>
        private static void SetAsBackground()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            selectedLayer.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Crops the selected layer's sprite by removing transparent pixels until the closest non-transparent pixels are reached.
        /// </summary>
        private static void CropLayer()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Texture2D texture = spriteRenderer.sprite.texture;
                Rect nonTransparentRect = FindNonTransparentRect(texture);
                
                Texture2D croppedTexture = new Texture2D((int)nonTransparentRect.width, (int)nonTransparentRect.height);
                for (int y = (int)nonTransparentRect.y; y < nonTransparentRect.yMax; y++)
                {
                    for (int x = (int)nonTransparentRect.x; x < nonTransparentRect.xMax; x++)
                    {
                        croppedTexture.SetPixel(x - (int)nonTransparentRect.x, y - (int)nonTransparentRect.y, texture.GetPixel(x, y));
                    }
                }
                croppedTexture.Apply();

                Sprite croppedSprite = Sprite.Create(croppedTexture, new Rect(0, 0, croppedTexture.width, croppedTexture.height), new Vector2(0.5f, 0.5f));
                spriteRenderer.sprite = croppedSprite;
            }
        }

        /// <summary>
        /// Finds the bounding rectangle of the non-transparent pixels in a texture.
        /// </summary>
        /// <param name="texture">The texture to search.</param>
        /// <returns>A rectangle that bounds all the non-transparent pixels.</returns>
        private static Rect FindNonTransparentRect(Texture2D texture)
        {
            int xMin = texture.width;
            int xMax = 0;
            int yMin = texture.height;
            int yMax = 0;

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (texture.GetPixel(x, y).a != 0)
                    {
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }
            }

            if (xMax < xMin || yMax < yMin) return new Rect(0, 0, 0, 0); // Handle fully transparent case

            return new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }
    }
}