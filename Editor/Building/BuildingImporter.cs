using System;
using System.Collections.Generic;
using System.IO;
using _Project.Core.Building;
using _Project.Features.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace _Project.Editor.Building
{
    public class BuildingImporter
    {
        private const float CameraBoundsMargin = 4f;

        private class ImportContext
        {
            public ReferenceSpriteImporter Sprites;
            public ReferenceTilemapImporter Tilemaps;
            public BuildingItemPrefabImporter Prefabs;
            public ReferenceItemResolver Resolver;
            public BuildingBalanceImporter Balance;
            public BuildingLocalizationImporter Localization;
            public List<ReferenceItemResolver.ResolvedItem> Items = new List<ReferenceItemResolver.ResolvedItem>();
        }

        private readonly int _areaIndex;
        private readonly int _firstGlobalDayIndex;
        private readonly int _firstGlobalTaskIndex;
        private readonly int _unlockLevel;
        private readonly int _gemsPerWin;

        private readonly BuildingImportReport _report = new BuildingImportReport();
        private readonly ReferenceExport _export = new ReferenceExport();
        private readonly IsoProjection _projection = new IsoProjection();

        public BuildingImporter(int areaIndex, int firstGlobalDayIndex, int firstGlobalTaskIndex, int unlockLevel, int gemsPerWin)
        {
            _areaIndex = areaIndex;
            _firstGlobalDayIndex = firstGlobalDayIndex;
            _firstGlobalTaskIndex = firstGlobalTaskIndex;
            _unlockLevel = unlockLevel;
            _gemsPerWin = gemsPerWin;
        }

        public BuildingImportReport Run()
        {
            ImportContext context = CreateContext();
            EnsureFolders();

            ReferenceAreaData area = LoadArea();
            RequestAreaItems(area, context);

            ReferenceEnvironment environment = LoadEnvironment();

            foreach (ReferenceEnvironment.TilemapLayer layer in environment.Layers)
            {
                if (layer.Active)
                {
                    context.Tilemaps.RequestSprites(layer.Tilemap);
                }
            }

            context.Sprites.ImportAll();
            context.Prefabs.ImportAll();

            BuildingBalanceConfig balance = context.Balance.Import(_unlockLevel, _gemsPerWin);
            SaveAreaConfig(area, context.Balance.TaskTypes);
            BuildingItemCatalog catalog = SaveCatalog(context.Items, context.Prefabs.Prefabs);
            context.Localization.ImportTaskTitles(_firstGlobalTaskIndex, area.TaskCount);
            context.Localization.ImportStaticKeys();
            BuildingWorldConfig worldConfig = new BuildingWorldConfigInstaller(_report).BuildConfig();

            Rect bounds = ComputeCameraBounds(area, out Vector2 center);
            BuildingSceneBuilder sceneBuilder = new BuildingSceneBuilder(context.Tilemaps, context.Prefabs, _report);
            BuildingWorldView world = sceneBuilder.Build(environment, bounds, center);
            new ReferenceDecorImporter(new ReferenceSpriteImporter(_export, _report), _report).Import(environment, world);

            Dictionary<string, Sprite> uiSprites = new BuildingUiSpriteImporter(_export, _report).Import();
            new BuildingUiBuilder(_report, uiSprites).Build(world.gameObject.scene, world, worldConfig, balance, catalog);
            new BuildingSceneFile().Save(world.gameObject.scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _report.Count("balance config: " + AssetDatabase.GetAssetPath(balance));
            return _report;
        }

        public BuildingImportReport ImportAreaAssets()
        {
            ImportContext context = CreateContext();
            EnsureFolders();

            ReferenceAreaData area = LoadArea();
            RequestAreaItems(area, context);

            context.Sprites.ImportAll();
            context.Prefabs.ImportAll();

            context.Balance.Import(_unlockLevel, _gemsPerWin);
            SaveAreaConfig(area, context.Balance.TaskTypes);
            SaveCatalog(context.Items, context.Prefabs.Prefabs);
            context.Localization.ImportTaskTitles(_firstGlobalTaskIndex, area.TaskCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return _report;
        }

        public BuildingImportReport ImportDecor()
        {
            BuildingSceneFile sceneFile = new BuildingSceneFile();
            Scene scene = sceneFile.OpenOrCreate();
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world == null)
            {
                throw new InvalidOperationException("Run the full import first: the Building scene has no world");
            }

            EnsureFolders();
            ReferenceEnvironment environment = LoadEnvironment();
            new ReferenceDecorImporter(new ReferenceSpriteImporter(_export, _report), _report).Import(environment, world);
            sceneFile.Save(scene);
            AssetDatabase.SaveAssets();
            return _report;
        }

        public BuildingImportReport ImportEnvironmentTiles()
        {
            BuildingSceneFile sceneFile = new BuildingSceneFile();
            Scene scene = sceneFile.OpenOrCreate();
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world == null || world.EnvironmentGrid == null)
            {
                throw new InvalidOperationException("Run the full import first: the Building scene has no environment");
            }

            EnsureFolders();
            ImportContext context = CreateContext();
            ReferenceEnvironment environment = LoadEnvironment();

            foreach (ReferenceEnvironment.TilemapLayer layer in environment.Layers)
            {
                if (layer.Active)
                {
                    context.Tilemaps.RequestSprites(layer.Tilemap);
                }
            }

            context.Sprites.ImportAll();
            int refilled = 0;

            foreach (ReferenceEnvironment.TilemapLayer layer in environment.Layers)
            {
                if (layer.Active == false)
                {
                    continue;
                }

                Transform layerObject = world.EnvironmentGrid.transform.Find(layer.Name);
                Tilemap tilemap = layerObject != null ? layerObject.GetComponent<Tilemap>() : null;

                if (tilemap == null)
                {
                    _report.Warn("Environment layer missing in scene: " + layer.Name);
                    continue;
                }

                context.Tilemaps.Fill(layer.Tilemap, tilemap, "Environment/" + layer.Name);
                refilled++;
            }

            _report.Count("environment layers refilled", refilled);
            sceneFile.Save(scene);
            AssetDatabase.SaveAssets();
            return _report;
        }

        public BuildingImportReport RebuildUi()
        {
            new BuildingLocalizationImporter(_report).ImportStaticKeys();

            BuildingSceneFile sceneFile = new BuildingSceneFile();
            Scene scene = sceneFile.OpenOrCreate();
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world == null)
            {
                throw new InvalidOperationException("Run the full import first: the Building scene has no world");
            }

            BuildingWorldConfig worldConfig = new BuildingWorldConfigInstaller(_report).BuildConfig();
            BuildingBalanceConfig balance = AssetDatabase.LoadAssetAtPath<BuildingBalanceConfig>(BuildingAssetPaths.BalancePath);
            BuildingItemCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingItemCatalog>(BuildingAssetPaths.CatalogPath);

            Dictionary<string, Sprite> uiSprites = new BuildingUiSpriteImporter(_export, _report).Import();
            new BuildingUiBuilder(_report, uiSprites).Build(scene, world, worldConfig, balance, catalog);
            sceneFile.Save(scene);
            AssetDatabase.SaveAssets();
            return _report;
        }

        private ImportContext CreateContext()
        {
            ImportContext context = new ImportContext();
            context.Sprites = new ReferenceSpriteImporter(_export, _report);
            BuildingTileLibrary tiles = new BuildingTileLibrary();
            context.Tilemaps = new ReferenceTilemapImporter(context.Sprites, tiles, _report);
            context.Prefabs = new BuildingItemPrefabImporter(_export, context.Sprites, context.Tilemaps, _report);
            context.Resolver = new ReferenceItemResolver(_export, _report);
            context.Balance = new BuildingBalanceImporter(_export, _report);
            context.Localization = new BuildingLocalizationImporter(_report);
            return context;
        }

        private ReferenceAreaData LoadArea()
        {
            ReferenceAreaData area = new ReferenceAreaData();
            area.Load(_export.LoadMonoBehaviour(ReferenceExport.MonoBehaviourFolder + "/Area_" + _areaIndex + ".asset"));
            _report.Count("days", area.Days.Count);
            _report.Count("tasks", area.TaskCount);
            _report.Count("decor placements", area.DecorBefore.Count + area.DecorAfter.Count);
            return area;
        }

        private ReferenceEnvironment LoadEnvironment()
        {
            ReferenceEnvironment environment = new ReferenceEnvironment();
            environment.Load(_export.LoadDocuments(ReferenceExport.BuildingsScenePath));
            return environment;
        }

        private void RequestAreaItems(ReferenceAreaData area, ImportContext context)
        {
            foreach (int itemId in area.CollectItemIds())
            {
                ReferenceItemResolver.ResolvedItem item = context.Resolver.Resolve(itemId);

                if (item != null)
                {
                    context.Items.Add(item);
                    context.Prefabs.Request(item.Id, item.PrefabPath, item.IsoSize);
                }
            }

            _report.Count("unique item ids", context.Items.Count);
        }

        private BuildingAreaConfig SaveAreaConfig(ReferenceAreaData area, List<BuildingTaskType> taskTypes)
        {
            List<BuildingDayConfig> days = new List<BuildingDayConfig>();
            BuildingDependencyResolver dependencies = new BuildingDependencyResolver(_report);
            int globalTask = _firstGlobalTaskIndex;

            for (int dayIndex = 0; dayIndex < area.Days.Count; dayIndex++)
            {
                ReferenceAreaData.Day day = area.Days[dayIndex];
                List<BuildingTaskConfig> tasks = new List<BuildingTaskConfig>();

                for (int taskIndex = 0; taskIndex < day.Tasks.Count; taskIndex++)
                {
                    ReferenceAreaData.Task task = day.Tasks[taskIndex];
                    BuildingTaskType type = globalTask < taskTypes.Count ? taskTypes[globalTask] : BuildingTaskType.Medium;

                    if (globalTask >= taskTypes.Count)
                    {
                        _report.Warn("Task type missing for global task " + globalTask + "; using Medium");
                    }

                    int dependency = dependencies.ToSameDayIndex(area, _firstGlobalTaskIndex, dayIndex, taskIndex, task.DependsOnTaskIndex);

                    tasks.Add(new BuildingTaskConfig(
                        BuildingAssetPaths.TaskTitleKeyPrefix + globalTask,
                        type,
                        dependency,
                        task.ItemsBefore.ToArray(),
                        task.ItemsAfter.ToArray()));
                    globalTask++;
                }

                days.Add(new BuildingDayConfig(tasks.ToArray()));
            }

            string path = BuildingAssetPaths.AreasFolder + "/Area_" + _areaIndex + ".asset";
            BuildingAreaConfig config = AssetDatabase.LoadAssetAtPath<BuildingAreaConfig>(path);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildingAreaConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.Initialize(_areaIndex, _firstGlobalDayIndex, days.ToArray(), area.DecorBefore.ToArray(), area.DecorAfter.ToArray());
            EditorUtility.SetDirty(config);
            _report.Count("area config: " + path);
            return config;
        }

        private BuildingItemCatalog SaveCatalog(List<ReferenceItemResolver.ResolvedItem> items, IReadOnlyDictionary<int, BuildingItemView> prefabs)
        {
            BuildingItemCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingItemCatalog>(BuildingAssetPaths.CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BuildingItemCatalog>();
                AssetDatabase.CreateAsset(catalog, BuildingAssetPaths.CatalogPath);
            }

            Dictionary<int, BuildingItemCatalog.Entry> entries = new Dictionary<int, BuildingItemCatalog.Entry>();

            for (int i = 0; i < catalog.Count; i++)
            {
                BuildingItemCatalog.Entry existing = catalog.GetAt(i);
                entries[existing.Id] = existing;
            }

            foreach (ReferenceItemResolver.ResolvedItem item in items)
            {
                if (prefabs.TryGetValue(item.Id, out BuildingItemView _))
                {
                    entries[item.Id] = new BuildingItemCatalog.Entry(item.Id, item.IsoSize);
                }
                else
                {
                    _report.Warn("Catalog: prefab missing for id " + item.Id + " (" + item.SoName + ")");
                }
            }

            List<int> ids = new List<int>(entries.Keys);
            ids.Sort();
            BuildingItemCatalog.Entry[] ordered = new BuildingItemCatalog.Entry[ids.Count];

            for (int i = 0; i < ids.Count; i++)
            {
                ordered[i] = entries[ids[i]];
            }

            catalog.SetEntries(ordered);
            EditorUtility.SetDirty(catalog);
            _report.Count("catalog entries", ordered.Length);
            return catalog;
        }

        private Rect ComputeCameraBounds(ReferenceAreaData area, out Vector2 center)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (BuildingItemPlacement placement in area.CollectAllPlacements())
            {
                Vector2 screen = _projection.IsoToScreen(placement.IsoPosition);
                minX = Mathf.Min(minX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxX = Mathf.Max(maxX, screen.x);
                maxY = Mathf.Max(maxY, screen.y);
            }

            if (minX > maxX)
            {
                center = Vector2.zero;
                return new Rect(-10f, -10f, 20f, 20f);
            }

            Rect bounds = Rect.MinMaxRect(
                minX - CameraBoundsMargin,
                minY - CameraBoundsMargin,
                maxX + CameraBoundsMargin,
                maxY + CameraBoundsMargin);
            center = bounds.center;
            return bounds;
        }

        private void EnsureFolders()
        {
            string[] folders =
            {
                BuildingAssetPaths.SpritesFolder,
                BuildingAssetPaths.ItemSpritesFolder,
                BuildingAssetPaths.TileSpritesFolder,
                BuildingAssetPaths.DecorSpritesFolder,
                BuildingAssetPaths.ConfigsFolder,
                BuildingAssetPaths.TilesFolder,
                BuildingAssetPaths.AreasFolder,
                BuildingAssetPaths.ItemPrefabsFolder
            };

            bool created = false;

            foreach (string folder in folders)
            {
                if (Directory.Exists(folder) == false)
                {
                    Directory.CreateDirectory(folder);
                    created = true;
                }
            }

            if (created)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
