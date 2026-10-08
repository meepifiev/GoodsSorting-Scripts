using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Editor.Building
{
    public class BuildingTileLibrary
    {
        private readonly Dictionary<Sprite, Tile> _tiles = new Dictionary<Sprite, Tile>();

        public Tile GetTile(Sprite sprite)
        {
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (_tiles.TryGetValue(sprite, out Tile cached))
            {
                return cached;
            }

            if (Directory.Exists(BuildingAssetPaths.TilesFolder) == false)
            {
                Directory.CreateDirectory(BuildingAssetPaths.TilesFolder);
                AssetDatabase.Refresh();
            }

            string path = BuildingAssetPaths.TilesFolder + "/" + SanitizeName(sprite.name) + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);

            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.colliderType = Tile.ColliderType.None;
                tile.flags = TileFlags.LockColor;
                AssetDatabase.CreateAsset(tile, path);
            }
            else if (tile.sprite != sprite)
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }

            _tiles[sprite] = tile;
            return tile;
        }

        private string SanitizeName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return name;
        }
    }
}
