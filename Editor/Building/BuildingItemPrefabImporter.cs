using System;
using System.Collections.Generic;
using System.IO;
using _Project.Features.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Editor.Building
{
    public class BuildingItemPrefabImporter
    {
        private const string ThemeNamePrefix = "Circle";
        private const string VisualRootName = "Visual";

        private class Job
        {
            public int ItemId;
            public string SourcePath;
            public Vector3 IsoSize;
            public ReferencePrefabHierarchy Hierarchy;
        }

        private readonly ReferenceExport _export;
        private readonly ReferenceSpriteImporter _sprites;
        private readonly ReferenceTilemapImporter _tilemaps;
        private readonly BuildingImportReport _report;
        private readonly List<Job> _jobs = new List<Job>();
        private readonly Dictionary<int, BuildingItemView> _prefabs = new Dictionary<int, BuildingItemView>();
        private Material _spriteMaterial;

        public BuildingItemPrefabImporter(
            ReferenceExport export,
            ReferenceSpriteImporter sprites,
            ReferenceTilemapImporter tilemaps,
            BuildingImportReport report)
        {
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            _tilemaps = tilemaps ?? throw new ArgumentNullException(nameof(tilemaps));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public IReadOnlyDictionary<int, BuildingItemView> Prefabs => _prefabs;

        public void Request(int itemId, string sourcePrefabPath, Vector3 isoSize)
        {
            ReferencePrefabHierarchy hierarchy = new ReferencePrefabHierarchy();
            hierarchy.Build(_export.LoadDocuments(sourcePrefabPath));

            if (hierarchy.Root == null)
            {
                _report.Warn("Prefab has no root transform: " + sourcePrefabPath);
                return;
            }

            RequestSprites(hierarchy.Root);
            _jobs.Add(new Job
            {
                ItemId = itemId,
                SourcePath = sourcePrefabPath,
                IsoSize = isoSize,
                Hierarchy = hierarchy
            });
        }

        public void ImportAll()
        {
            if (Directory.Exists(BuildingAssetPaths.ItemPrefabsFolder) == false)
            {
                Directory.CreateDirectory(BuildingAssetPaths.ItemPrefabsFolder);
                AssetDatabase.Refresh();
            }

            _spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            foreach (Job job in _jobs)
            {
                BuildPrefab(job);
            }

            AssetDatabase.SaveAssets();
            _report.Count("item prefabs generated", _prefabs.Count);
        }

        private void RequestSprites(ReferencePrefabHierarchy.Node node)
        {
            if (node.SpriteRenderer != null)
            {
                UnityYamlReference sprite = node.SpriteRenderer.GetReference("m_Sprite");

                if (sprite.IsExternal)
                {
                    _sprites.Request(sprite.Guid, BuildingAssetPaths.ItemSpritesFolder, false);
                }
            }

            if (node.Tilemap != null)
            {
                _tilemaps.RequestSprites(node.Tilemap);
            }

            foreach (ReferencePrefabHierarchy.Node child in node.Children)
            {
                RequestSprites(child);
            }
        }

        private void BuildPrefab(Job job)
        {
            ReferencePrefabHierarchy.Node rootNode = job.Hierarchy.Root;
            GameObject root = new GameObject(rootNode.Name);

            try
            {
                foreach (ReferencePrefabHierarchy.Node child in rootNode.Children)
                {
                    CreateNode(child, root.transform, job.SourcePath);
                }

                Transform visualRoot = ResolveVisualRoot(root.transform);
                GameObject[] themes = ResolveThemes(visualRoot);

                for (int i = 0; i < themes.Length; i++)
                {
                    themes[i].SetActive(i == 0);
                }

                BuildingItemView view = root.AddComponent<BuildingItemView>();
                view.Initialize(job.ItemId, job.IsoSize, visualRoot, themes);

                string path = BuildingAssetPaths.ItemPrefabsFolder + "/" + rootNode.Name + "_id" + job.ItemId + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);

                if (success == false || saved == null)
                {
                    _report.Warn("Failed to save prefab " + path);
                    return;
                }

                _prefabs[job.ItemId] = saved.GetComponent<BuildingItemView>();
                _report.Count(themes.Length > 1 ? "items with theme variants" : "items with single theme");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private void CreateNode(ReferencePrefabHierarchy.Node node, Transform parent, string context)
        {
            GameObject gameObject = new GameObject(node.Name);
            gameObject.transform.SetParent(parent, false);
            Vector3 localPosition = node.Transform.GetVector3("m_LocalPosition");

            if (node.Tilemap == null)
            {
                localPosition.z = 0f;
            }

            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = node.Transform.GetVector3("m_LocalScale", Vector3.one);
            gameObject.transform.localRotation = ReadRotation(node.Transform);
            gameObject.SetActive(node.Active);

            if (node.SpriteRenderer != null)
            {
                CreateSpriteRenderer(node, gameObject, context);
            }

            if (node.Grid != null)
            {
                Grid grid = gameObject.AddComponent<Grid>();
                grid.cellSize = node.Grid.GetVector3("m_CellSize", new Vector3(2f, 1f, 1f));
                grid.cellGap = node.Grid.GetVector3("m_CellGap");
                grid.cellLayout = (GridLayout.CellLayout)node.Grid.GetInt("m_CellLayout", (int)GridLayout.CellLayout.Isometric);
                grid.cellSwizzle = (GridLayout.CellSwizzle)node.Grid.GetInt("m_CellSwizzle");
            }

            if (node.Tilemap != null)
            {
                Tilemap tilemap = gameObject.AddComponent<Tilemap>();
                TilemapRenderer renderer = gameObject.AddComponent<TilemapRenderer>();
                ConfigureTilemapRenderer(renderer, node.TilemapRenderer);
                _tilemaps.Fill(node.Tilemap, tilemap, context + "/" + node.Name);
            }

            foreach (ReferencePrefabHierarchy.Node child in node.Children)
            {
                CreateNode(child, gameObject.transform, context);
            }
        }

        private void CreateSpriteRenderer(ReferencePrefabHierarchy.Node node, GameObject gameObject, string context)
        {
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            UnityYamlReference spriteReference = node.SpriteRenderer.GetReference("m_Sprite");
            Sprite sprite = _sprites.Resolve(spriteReference.Guid);

            if (sprite == null && spriteReference.IsExternal)
            {
                _report.Warn(context + "/" + node.Name + ": sprite not resolved (" + spriteReference.Guid + ")");
            }

            renderer.sprite = sprite;
            renderer.sharedMaterial = _spriteMaterial;
            renderer.flipX = node.SpriteRenderer.GetInt("m_FlipX") != 0;
            renderer.flipY = node.SpriteRenderer.GetInt("m_FlipY") != 0;
            renderer.color = node.SpriteRenderer.GetColor("m_Color", Color.white);
            renderer.sortingOrder = node.SpriteRenderer.GetInt("m_SortingOrder");
        }

        public void ConfigureTilemapRenderer(TilemapRenderer renderer, UnityYamlNode fields)
        {
            renderer.sharedMaterial = _spriteMaterial;

            if (fields == null)
            {
                return;
            }

            renderer.sortingOrder = fields.GetInt("m_SortingOrder");
            renderer.sortOrder = (TilemapRenderer.SortOrder)fields.GetInt("m_SortOrder");
            renderer.mode = (TilemapRenderer.Mode)fields.GetInt("m_Mode");
        }

        private Quaternion ReadRotation(UnityYamlNode transform)
        {
            UnityYamlNode rotation = transform["m_LocalRotation"];

            if (rotation == null || rotation.IsMap == false)
            {
                return Quaternion.identity;
            }

            return new Quaternion(rotation.GetFloat("x"), rotation.GetFloat("y"), rotation.GetFloat("z"), rotation.GetFloat("w", 1f));
        }

        private Transform ResolveVisualRoot(Transform root)
        {
            if (root.childCount == 1)
            {
                return root.GetChild(0);
            }

            GameObject visual = new GameObject(VisualRootName);
            visual.transform.SetParent(root, false);

            List<Transform> children = new List<Transform>();

            foreach (Transform child in root)
            {
                if (child != visual.transform)
                {
                    children.Add(child);
                }
            }

            foreach (Transform child in children)
            {
                child.SetParent(visual.transform, false);
            }

            return visual.transform;
        }

        private GameObject[] ResolveThemes(Transform visualRoot)
        {
            List<GameObject> themes = new List<GameObject>();

            foreach (Transform child in visualRoot)
            {
                if (child.name.StartsWith(ThemeNamePrefix, StringComparison.Ordinal) && child.GetComponent<SpriteRenderer>() != null)
                {
                    themes.Add(child.gameObject);
                }
            }

            if (themes.Count == 0 || themes.Count != visualRoot.childCount)
            {
                return Array.Empty<GameObject>();
            }

            themes.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return themes.ToArray();
        }
    }
}
