using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceSpriteImporter
    {
        private const int PngWidthOffset = 16;
        private const float DefaultPixelsPerUnit = 100f;
        private const uint SpriteExtrude = 1;
        private const float EdgeTolerancePixels = 0.6f;

        private class SpriteRequest
        {
            public string RefSpriteGuid;
            public string RefTextureGuid;
            public string Name;
            public Vector2 Pivot;
            public Vector4 Border;
            public float PixelsPerUnit;
            public bool FullRectMesh;
            public Rect Rect;
            public UnityYamlNode Sprite;
        }

        private class TextureJob
        {
            public string DestinationPath;
            public Vector2Int Size;
            public Vector2 Pivot;
            public Vector4 Border;
            public float PixelsPerUnit;
            public bool FullRectMesh;
            public string RefSpriteGuid;
            public byte[] Png;
            public string SourcePath;
        }

        private readonly ReferenceExport _export;
        private readonly BuildingImportReport _report;
        private readonly Dictionary<string, SpriteRequest> _requests = new Dictionary<string, SpriteRequest>();
        private readonly Dictionary<string, List<SpriteRequest>> _requestsByTexture = new Dictionary<string, List<SpriteRequest>>();
        private readonly Dictionary<string, string> _folderByTexture = new Dictionary<string, string>();
        private readonly Dictionary<string, Sprite> _resolved = new Dictionary<string, Sprite>();
        private readonly HashSet<string> _usedDestinations = new HashSet<string>();
        private readonly List<TextureJob> _jobs = new List<TextureJob>();

        public ReferenceSpriteImporter(ReferenceExport export, BuildingImportReport report)
        {
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Request(string refSpriteGuid, string destinationFolder, bool fullRectMesh)
        {
            if (string.IsNullOrEmpty(refSpriteGuid) || _requests.ContainsKey(refSpriteGuid))
            {
                return;
            }

            UnityYamlNode sprite = _export.LoadSprite(refSpriteGuid);

            if (sprite == null)
            {
                _report.Warn("Sprite asset not found in reference export: " + refSpriteGuid);
                _requests[refSpriteGuid] = null;
                return;
            }

            UnityYamlNode renderData = sprite.GetOrEmpty("m_RD");
            UnityYamlReference texture = renderData.GetReference("texture");

            if (texture.IsExternal == false)
            {
                _report.Warn("Sprite has no texture: " + sprite.GetString("m_Name") + " (" + refSpriteGuid + ")");
                _requests[refSpriteGuid] = null;
                return;
            }

            Rect rect = sprite.GetRect("m_Rect");
            Rect textureRect = renderData.GetRect("textureRect");

            if (textureRect.width > 0f && textureRect.height > 0f)
            {
                rect = textureRect;
            }

            SpriteRequest request = new SpriteRequest
            {
                RefSpriteGuid = refSpriteGuid,
                RefTextureGuid = texture.Guid,
                Name = sprite.GetString("m_Name", "Sprite"),
                Pivot = sprite.GetVector2("m_Pivot", new Vector2(0.5f, 0.5f)),
                Border = ReadBorder(sprite),
                PixelsPerUnit = sprite.GetFloat("m_PixelsToUnits", DefaultPixelsPerUnit),
                FullRectMesh = fullRectMesh,
                Rect = rect,
                Sprite = sprite
            };

            _requests[refSpriteGuid] = request;

            if (_requestsByTexture.TryGetValue(texture.Guid, out List<SpriteRequest> list) == false)
            {
                list = new List<SpriteRequest>();
                _requestsByTexture[texture.Guid] = list;
                _folderByTexture[texture.Guid] = destinationFolder;
            }

            list.Add(request);
        }

        public void ImportAll()
        {
            _jobs.Clear();

            foreach (KeyValuePair<string, List<SpriteRequest>> pair in _requestsByTexture)
            {
                PrepareTextureJobs(pair.Key, pair.Value, _folderByTexture[pair.Key]);
            }

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (TextureJob job in _jobs)
                {
                    WriteTexture(job);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();

            foreach (TextureJob job in _jobs)
            {
                ConfigureImporter(job);
            }

            AssetDatabase.Refresh();

            foreach (TextureJob job in _jobs)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(job.DestinationPath);

                if (sprite == null)
                {
                    _report.Warn("Sprite failed to import: " + job.DestinationPath);
                    continue;
                }

                _resolved[job.RefSpriteGuid] = sprite;
            }

            _report.Count("textures imported", _jobs.Count);
            _report.Count("sprites resolved", _resolved.Count);
        }

        public Sprite Resolve(string refSpriteGuid)
        {
            if (string.IsNullOrEmpty(refSpriteGuid))
            {
                return null;
            }

            return _resolved.TryGetValue(refSpriteGuid, out Sprite sprite) ? sprite : null;
        }

        private void PrepareTextureJobs(string textureGuid, List<SpriteRequest> requests, string folder)
        {
            string sourcePath = _export.FindPathByGuid(textureGuid);

            if (sourcePath == null || sourcePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false)
            {
                _report.Warn("Texture PNG not found for sprites of texture " + textureGuid);
                return;
            }

            Vector2Int size = ReadPngSize(sourcePath);

            if (requests.Count == 1 && CoversWholeTexture(requests[0].Rect, size))
            {
                SpriteRequest request = requests[0];
                _jobs.Add(new TextureJob
                {
                    DestinationPath = ReserveDestination(folder, Path.GetFileNameWithoutExtension(sourcePath)),
                    Size = size,
                    Pivot = request.Pivot,
                    Border = request.Border,
                    PixelsPerUnit = request.PixelsPerUnit,
                    FullRectMesh = request.FullRectMesh,
                    RefSpriteGuid = request.RefSpriteGuid,
                    SourcePath = sourcePath
                });
                return;
            }

            Texture2D atlas = LoadReadableTexture(sourcePath);

            try
            {
                foreach (SpriteRequest request in requests)
                {
                    TextureJob job = ExtractSprite(atlas, request, folder);

                    if (job != null)
                    {
                        _jobs.Add(job);
                        _report.Count("sprites cut from atlases");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private TextureJob ExtractSprite(Texture2D atlas, SpriteRequest request, string folder)
        {
            ReferenceSpriteMesh mesh = new ReferenceSpriteMesh();

            if (mesh.TryRead(request.Sprite) == false)
            {
                _report.Warn("Sprite mesh unreadable, skipping atlas sprite " + request.Name);
                return null;
            }

            int width = atlas.width;
            int height = atlas.height;
            Vector2[] pixels = mesh.PixelPositions;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < pixels.Length; i++)
            {
                minX = Mathf.Min(minX, pixels[i].x);
                minY = Mathf.Min(minY, pixels[i].y);
                maxX = Mathf.Max(maxX, pixels[i].x);
                maxY = Mathf.Max(maxY, pixels[i].y);
            }

            if (minX < -1f || minY < -1f || maxX > width + 1f || maxY > height + 1f)
            {
                _report.Warn("Sprite mesh outside atlas, skipping " + request.Name);
                return null;
            }

            Vector2 pivotInAtlas = mesh.PivotPixel;
            int left = Mathf.Clamp(Mathf.FloorToInt(minX), 0, width - 1);
            int bottom = Mathf.Clamp(Mathf.FloorToInt(minY), 0, height - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt(maxX), left + 1, width);
            int top = Mathf.Clamp(Mathf.CeilToInt(maxY), bottom + 1, height);
            int cutWidth = right - left;
            int cutHeight = top - bottom;

            Vector2[] packedPolygon = FlipPolygon(pixels, mesh.PackedFlippedHorizontally, mesh.PackedFlippedVertically, minX + maxX, minY + maxY);
            Color32[] source = atlas.GetPixels32();
            Color32[] target = new Color32[cutWidth * cutHeight];

            for (int y = 0; y < cutHeight; y++)
            {
                for (int x = 0; x < cutWidth; x++)
                {
                    Vector2 center = new Vector2(left + x + 0.5f, bottom + y + 0.5f);

                    if (IsInsideMesh(center, packedPolygon, mesh.Indices) == false)
                    {
                        continue;
                    }

                    int targetX = mesh.PackedFlippedHorizontally ? cutWidth - 1 - x : x;
                    int targetY = mesh.PackedFlippedVertically ? cutHeight - 1 - y : y;
                    target[targetY * cutWidth + targetX] = source[(bottom + y) * width + left + x];
                }
            }

            if (mesh.PackedFlippedHorizontally || mesh.PackedFlippedVertically)
            {
                _report.Count("atlas sprites unflipped");
            }

            Texture2D cut = new Texture2D(cutWidth, cutHeight, TextureFormat.RGBA32, false);
            cut.SetPixels32(target);
            cut.Apply();
            byte[] png = cut.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(cut);

            Vector2 pivot = new Vector2((pivotInAtlas.x - left) / cutWidth, (pivotInAtlas.y - bottom) / cutHeight);

            return new TextureJob
            {
                DestinationPath = ReserveDestination(folder, request.Name),
                Size = new Vector2Int(cutWidth, cutHeight),
                Pivot = pivot,
                Border = Vector4.zero,
                PixelsPerUnit = request.PixelsPerUnit,
                FullRectMesh = request.FullRectMesh,
                RefSpriteGuid = request.RefSpriteGuid,
                Png = png
            };
        }

        private Vector2[] FlipPolygon(Vector2[] vertices, bool horizontal, bool vertical, float sumX, float sumY)
        {
            if (horizontal == false && vertical == false)
            {
                return vertices;
            }

            Vector2[] flipped = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                flipped[i] = new Vector2(
                    horizontal ? sumX - vertices[i].x : vertices[i].x,
                    vertical ? sumY - vertices[i].y : vertices[i].y);
            }

            return flipped;
        }

        private bool IsInsideMesh(Vector2 point, Vector2[] vertices, int[] indices)
        {
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                if (IsInsideTriangle(point, vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

            if (Mathf.Abs(area) < 0.0001f)
            {
                return false;
            }

            float sign = area > 0f ? 1f : -1f;
            return SignedDistance(point, a, b) * sign >= -EdgeTolerancePixels
                && SignedDistance(point, b, c) * sign >= -EdgeTolerancePixels
                && SignedDistance(point, c, a) * sign >= -EdgeTolerancePixels;
        }

        private float SignedDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 edge = to - from;
            float length = edge.magnitude;

            if (length < 0.0001f)
            {
                return 0f;
            }

            return ((to.x - from.x) * (point.y - from.y) - (to.y - from.y) * (point.x - from.x)) / length;
        }

        private void WriteTexture(TextureJob job)
        {
            string directory = Path.GetDirectoryName(job.DestinationPath);

            if (string.IsNullOrEmpty(directory) == false && Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            if (job.Png != null)
            {
                File.WriteAllBytes(job.DestinationPath, job.Png);
            }
            else
            {
                File.Copy(job.SourcePath, job.DestinationPath, true);
            }
        }

        private void ConfigureImporter(TextureJob job)
        {
            AssetDatabase.ImportAsset(job.DestinationPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(job.DestinationPath) as TextureImporter;

            if (importer == null)
            {
                _report.Warn("TextureImporter missing for " + job.DestinationPath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = job.PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = Mathf.Max(job.Size.x, job.Size.y) > 2048 ? 4096 : 2048;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = job.FullRectMesh ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteExtrude = SpriteExtrude;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = job.Pivot;
            settings.spriteBorder = job.Border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private Texture2D LoadReadableTexture(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (texture.LoadImage(File.ReadAllBytes(path)) == false)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidDataException(path);
            }

            return texture;
        }

        private bool CoversWholeTexture(Rect rect, Vector2Int size)
        {
            return Mathf.Abs(rect.x) < 0.5f
                && Mathf.Abs(rect.y) < 0.5f
                && Mathf.Abs(rect.width - size.x) < 0.5f
                && Mathf.Abs(rect.height - size.y) < 0.5f;
        }

        private Vector4 ReadBorder(UnityYamlNode sprite)
        {
            UnityYamlNode border = sprite["m_Border"];

            if (border == null || border.IsMap == false)
            {
                return Vector4.zero;
            }

            return new Vector4(border.GetFloat("x"), border.GetFloat("y"), border.GetFloat("z"), border.GetFloat("w"));
        }

        private string ReserveDestination(string folder, string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            name = name.Replace(' ', '_');
            string destination = folder + "/" + name + ".png";
            int suffix = 1;

            while (_usedDestinations.Add(destination) == false)
            {
                destination = folder + "/" + name + "_" + suffix++ + ".png";
            }

            return destination;
        }

        private Vector2Int ReadPngSize(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] header = new byte[24];
                int read = stream.Read(header, 0, header.Length);

                if (read < header.Length)
                {
                    throw new InvalidDataException(path);
                }

                int width = (header[PngWidthOffset] << 24) | (header[PngWidthOffset + 1] << 16) | (header[PngWidthOffset + 2] << 8) | header[PngWidthOffset + 3];
                int height = (header[PngWidthOffset + 4] << 24) | (header[PngWidthOffset + 5] << 16) | (header[PngWidthOffset + 6] << 8) | header[PngWidthOffset + 7];
                return new Vector2Int(width, height);
            }
        }
    }
}
