using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class SceneContextMenuActions
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

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

        private static void MoveLayerUp()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            int siblingIndex = selectedLayer.transform.GetSiblingIndex();
            selectedLayer.transform.SetSiblingIndex(siblingIndex + 1);
        }

        private static void MoveLayerDown()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            int siblingIndex = selectedLayer.transform.GetSiblingIndex();
            selectedLayer.transform.SetSiblingIndex(siblingIndex - 1);
        }

        private static void CloneLayer()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            GameObject clonedLayer = UnityEngine.Object.Instantiate(selectedLayer);
            clonedLayer.name = selectedLayer.name + " (Clone)";
        }

        private static void DeleteLayer()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            UnityEngine.Object.DestroyImmediate(selectedLayer);
        }

        private static void FlipHorizontal()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }

        private static void FlipVertical()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipY = !spriteRenderer.flipY;
            }
        }

        private static void RemoveBackground()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = selectedLayer.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Texture2D texture2D = spriteRenderer.sprite.texture;

                // Ensure the texture is readable and uncompressed
                string texturePath = AssetDatabase.GetAssetPath(texture2D);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                // Convert texture to uncompressed format
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

        private static void SetAsBackground()
        {
            GameObject selectedLayer = Selection.activeGameObject;
            selectedLayer.transform.SetAsFirstSibling();
        }
    }
}
