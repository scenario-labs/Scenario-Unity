using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static log4net.Appender.ColoredConsoleAppender;
using static UnityEngine.GridLayout;

namespace Scenario.Editor
{
    public class PromptImagesTileCreator
    {
        private GameObject tilePalette;
        private Grid grid;
        private GridPalette gridPalette;
        private CellLayout layout;

        private PromptImages promptImages;
        private int selectedTextureIndex;


        public PromptImagesTileCreator(PromptImages _promptImages, int _selectedTextureIndex)
        {
            promptImages = _promptImages;
            selectedTextureIndex = _selectedTextureIndex;
        }

        public void OnGUI()
        {
            tilePalette = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Tile Palette", "The Tile Palette asset in which the tile will be added. It should be the parent prefab, not the Grid Palette under it."), tilePalette, typeof(GameObject), false);

            if (tilePalette == null)
            {
                GUILayout.Label("Please set a tile palette to begin.", EditorStyles.wordWrappedLabel);
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
            GUILayout.Label($"Generated Tiles will be downloaded in the same folder as your Tile Palette Prefab.", EditorStyles.wordWrappedLabel);

            if(GUILayout.Button(new GUIContent("Download as Tile", "The image will be processed to remove background then downloaded as a sprite in the Scenario Settings save folder. Then a Tile asset will be created (in the same folder as your Tile Palette) out of this Sprite and added to the Tile Palette your referenced.")))
            {
                promptImages.RemoveBackground(selectedTextureIndex);
                //here I should get the filePath
            }
        }

    }
}
