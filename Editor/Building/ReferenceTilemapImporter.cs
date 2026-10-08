using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Editor.Building
{
    public class ReferenceTilemapImporter
    {
        private const float MaxSaneMatrixValue = 100f;
        private const float MinSaneDeterminant = 0.001f;

        private readonly ReferenceSpriteImporter _sprites;
        private readonly BuildingTileLibrary _tiles;
        private readonly BuildingImportReport _report;

        public ReferenceTilemapImporter(ReferenceSpriteImporter sprites, BuildingTileLibrary tiles, BuildingImportReport report)
        {
            _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void RequestSprites(UnityYamlNode tilemapFields)
        {
            foreach (string guid in ReadSpriteGuids(tilemapFields))
            {
                if (guid != null)
                {
                    _sprites.Request(guid, BuildingAssetPaths.TileSpritesFolder, true);
                }
            }
        }

        public void Fill(UnityYamlNode tilemapFields, Tilemap tilemap, string context)
        {
            List<string> spriteGuids = ReadSpriteGuids(tilemapFields);
            List<Matrix4x4> matrices = ReadMatrices(tilemapFields);
            List<Color> colors = ReadColors(tilemapFields);

            tilemap.ClearAllTiles();
            tilemap.tileAnchor = tilemapFields.GetVector3("m_TileAnchor", new Vector3(0.5f, 0.5f, 0f));
            tilemap.color = tilemapFields.GetColor("m_Color", Color.white);

            int placed = 0;
            int skipped = 0;
            int tinted = 0;

            foreach (UnityYamlNode entry in tilemapFields.GetOrEmpty("m_Tiles").Items)
            {
                Vector3Int cell = entry.GetVector3Int("first");
                UnityYamlNode data = entry.GetOrEmpty("second");
                int spriteIndex = data.GetInt("m_TileSpriteIndex", -1);
                string guid = spriteIndex >= 0 && spriteIndex < spriteGuids.Count ? spriteGuids[spriteIndex] : null;
                Sprite sprite = _sprites.Resolve(guid);

                if (sprite == null)
                {
                    skipped++;
                    continue;
                }

                tilemap.SetTile(cell, _tiles.GetTile(sprite));

                int matrixIndex = data.GetInt("m_TileMatrixIndex", -1);

                if (matrixIndex >= 0 && matrixIndex < matrices.Count && matrices[matrixIndex] != Matrix4x4.identity)
                {
                    tilemap.SetTransformMatrix(cell, matrices[matrixIndex]);
                }

                int colorIndex = data.GetInt("m_TileColorIndex", -1);

                if (colorIndex >= 0 && colorIndex < colors.Count && colors[colorIndex] != Color.white)
                {
                    tinted++;
                }

                placed++;
            }

            _report.Count("tiles placed", placed);

            if (skipped > 0)
            {
                _report.Warn(context + ": " + skipped + " tiles skipped (sprite missing)");
            }

            if (tinted > 0)
            {
                _report.Warn(context + ": " + tinted + " tinted tiles imported as white");
            }
        }

        private List<string> ReadSpriteGuids(UnityYamlNode tilemapFields)
        {
            List<string> guids = new List<string>();

            foreach (UnityYamlNode slot in tilemapFields.GetOrEmpty("m_TileSpriteArray").Items)
            {
                UnityYamlReference reference = slot.GetReference("m_Data");
                guids.Add(reference.IsExternal ? reference.Guid : null);
            }

            return guids;
        }

        private List<Matrix4x4> ReadMatrices(UnityYamlNode tilemapFields)
        {
            List<Matrix4x4> matrices = new List<Matrix4x4>();

            foreach (UnityYamlNode slot in tilemapFields.GetOrEmpty("m_TileMatrixArray").Items)
            {
                Matrix4x4 matrix = slot.GetMatrix("m_Data");
                matrices.Add(IsSane(matrix) ? matrix : Matrix4x4.identity);
            }

            return matrices;
        }

        private List<Color> ReadColors(UnityYamlNode tilemapFields)
        {
            List<Color> colors = new List<Color>();

            foreach (UnityYamlNode slot in tilemapFields.GetOrEmpty("m_TileColorArray").Items)
            {
                colors.Add(slot.GetColor("m_Data", Color.white));
            }

            return colors;
        }

        private bool IsSane(Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++)
            {
                float value = matrix[i];

                if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) > MaxSaneMatrixValue)
                {
                    return false;
                }
            }

            float determinant = matrix.m00 * (matrix.m11 * matrix.m22 - matrix.m12 * matrix.m21)
                - matrix.m01 * (matrix.m10 * matrix.m22 - matrix.m12 * matrix.m20)
                + matrix.m02 * (matrix.m10 * matrix.m21 - matrix.m11 * matrix.m20);

            return Mathf.Abs(determinant) > MinSaneDeterminant && Mathf.Abs(matrix.m33 - 1f) < 0.001f;
        }
    }
}
