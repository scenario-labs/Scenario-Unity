using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.GridLayout;

namespace Scenario.Editor
{
    public class TileCreator
    {
        private GameObject tilePalette;
        private Grid grid;
        private GridPalette gridPalette;
        private CellLayout layout;

        private ImageDataStorage.ImageData selectedImage = null;

        private string selectedTextureId;

        private bool isProcessing = false;


        public TileCreator(string _selectedTextureIndex)
        {
            selectedTextureId = _selectedTextureIndex;
        }

        public TileCreator(ImageDataStorage.ImageData _selectedImage)
        {
            selectedImage = _selectedImage;
        }

        public void OnGUI()
        {
            tilePalette = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Tile Palette", "The Tile Palette asset in which the tile will be added. It should be the parent prefab, not the Grid Palette under it."), tilePalette, typeof(GameObject), false);

            if (tilePalette == null)
            {
                GUILayout.Label("Please set a Tile Palette to begin.", EditorStyles.wordWrappedLabel);
                return;
            }


            grid = tilePalette.GetComponent<Grid>();
            gridPalette = CommonUtils.GetSubObjectsOfType<GridPalette>(tilePalette)[0];
            layout = grid.cellLayout;

            if (layout == CellLayout.Hexagon || layout == CellLayout.IsometricZAsY)
            {
                GUILayout.Label($"Sorry, the Scenario Plugin only works with Rectangle & Isometric for the moment. Please modify the Cell Layout parameter of the Grid component of your Tile Palette Prefab.",  EditorStyles.wordWrappedLabel);
                return;
            }
            GUILayout.Label($"Your Grid layout : {layout}", EditorStyles.wordWrappedLabel);

            if(!isProcessing)
            {
                GUILayout.Label($"Generated Tiles will be downloaded in the same folder as your Tile Palette Prefab.", EditorStyles.wordWrappedLabel);

                if(GUILayout.Button(new GUIContent("Download as Tile", "The image will be processed to remove background then downloaded as a sprite in the Scenario Settings save folder. Then a Tile asset will be created (in the same folder as your Tile Palette) out of this Sprite and added to the Tile Palette your referenced.")))
                {
                    isProcessing = true;
                    CommonUtils.FetchTextureFromURL(Images.GetImageDataById(selectedTextureId).Url, response => { 
                        BackgroundRemoval.RemoveBackground(response, imageBytes =>
                        {
                            CommonUtils.SaveImageDataAsPNG(imageBytes, null, PluginSettings.TilePreset, (spritePath) =>
                            {
                                if (PluginSettings.UsePixelsUnitsEqualToImage)
                                {
                                    CommonUtils.ApplyPixelsPerUnit(spritePath);
                                }

                                Tile tile = CreateTile(Path.GetDirectoryName(AssetDatabase.GetAssetPath(tilePalette)), (Sprite)AssetDatabase.LoadAssetAtPath(spritePath, typeof(Sprite)));
                                AddTileToTilePalette(tile);
                                isProcessing = false;
                            });
                        });
                    });
                }
            }
            else
            {
                GUILayout.Label($"Please wait... Image background is being removed. The resulting sprite will be saved in the Scenario Settings save folder. Then a Tile will be created and added in your Tile Palette.", EditorStyles.wordWrappedLabel);
            }

        }

        private void AddTileToTilePalette(Tile _tile)
        {
            Tilemap tilemap = tilePalette.GetComponentInChildren<Tilemap>();
            Vector3Int emptyTile = Vector3Int.zero;
            while(tilemap.HasTile(emptyTile))
            {
                emptyTile.x += 1;
                emptyTile.y -= 1;
            }
            
            tilemap.SetTile(emptyTile, _tile);
        }

        /// <summary>
        /// Create a tile asset with a specific sprite at a specific path.
        /// Found here : https://docs.unity3d.com/Manual/Tilemap-ScriptableTiles-Example.html
        /// </summary>
        /// <param name="_tilePalettePath"></param>
        /// <param name="_sprite"></param>
        private Tile CreateTile(string _tilePalettePath, Sprite _sprite)
        {
            Tile newTile = ScriptableObject.CreateInstance<Tile>();
            newTile.sprite = _sprite;
            newTile.name = tilePalette.name + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".asset";
            string filePath = Path.Combine(_tilePalettePath, newTile.name);
            AssetDatabase.CreateAsset(newTile, filePath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath));

            return newTile;
        }

    }
}
