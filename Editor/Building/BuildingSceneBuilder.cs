using System;
using System.Collections.Generic;
using _Project.Features.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace _Project.Editor.Building
{
    public class BuildingSceneBuilder
    {
        private const string CameraName = "Camera_Building";
        private const string WorldName = "World";
        private const string EnvironmentName = "Environment";
        private const string ItemsName = "Items";
        private const string LockedAreasLayerName = "Tilemap_lockarea";
        private const string MainCameraTag = "MainCamera";
        private const float DefaultOrthographicSize = 10f;
        private const float MinOrthographicSize = 6f;
        private const float MaxOrthographicSize = 18f;
        private const float EntryOrthographicSize = 6f;
        private const float TravelOrthographicSize = 9f;
        private const float TravelDelay = 1f;
        private const float CameraDepth = -10f;
        private static readonly Color BackgroundColor = new Color(0.47058824f, 0.47058824f, 0.61960787f, 1f);

        private readonly ReferenceTilemapImporter _tilemaps;
        private readonly BuildingItemPrefabImporter _prefabs;
        private readonly BuildingImportReport _report;

        public BuildingSceneBuilder(ReferenceTilemapImporter tilemaps, BuildingItemPrefabImporter prefabs, BuildingImportReport report)
        {
            _tilemaps = tilemaps ?? throw new ArgumentNullException(nameof(tilemaps));
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public BuildingWorldView Build(ReferenceEnvironment environment, Rect cameraBounds, Vector2 cameraStart)
        {
            Scene scene = new BuildingSceneFile().OpenOrCreate();
            RemoveGenerated(scene);

            BuildingCameraController cameraController = CreateCamera(cameraBounds, cameraStart);

            GameObject world = new GameObject(WorldName);
            BuildingWorldView worldView = world.AddComponent<BuildingWorldView>();

            GameObject environmentObject = new GameObject(EnvironmentName);
            environmentObject.transform.SetParent(world.transform, false);
            Grid grid = environmentObject.AddComponent<Grid>();
            grid.cellSize = environment.Grid.GetVector3("m_CellSize", new Vector3(2f, 1f, 1f));
            grid.cellGap = environment.Grid.GetVector3("m_CellGap");
            grid.cellLayout = (GridLayout.CellLayout)environment.Grid.GetInt("m_CellLayout", (int)GridLayout.CellLayout.Isometric);
            grid.cellSwizzle = (GridLayout.CellSwizzle)environment.Grid.GetInt("m_CellSwizzle");

            Tilemap lockedAreas = null;

            foreach (ReferenceEnvironment.TilemapLayer layer in environment.Layers)
            {
                if (layer.Active == false)
                {
                    _report.Count("environment layers skipped (inactive)");
                    continue;
                }

                Tilemap tilemap = CreateLayer(layer, environmentObject.transform);

                if (layer.Name == LockedAreasLayerName)
                {
                    lockedAreas = tilemap;
                }

                _report.Count("environment layers");
            }

            GameObject items = new GameObject(ItemsName);
            items.transform.SetParent(world.transform, false);

            worldView.Initialize(grid, items.transform, lockedAreas, cameraController);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureInBuildSettings();
            return worldView;
        }

        private void RemoveGenerated(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == CameraName || root.name == WorldName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private BuildingCameraController CreateCamera(Rect bounds, Vector2 start)
        {
            GameObject cameraObject = new GameObject(CameraName);
            cameraObject.tag = MainCameraTag;
            cameraObject.transform.position = new Vector3(start.x, start.y, CameraDepth);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = DefaultOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            cameraObject.AddComponent<AudioListener>();

            BuildingCameraController controller = cameraObject.AddComponent<BuildingCameraController>();
            controller.SetZoomRange(MinOrthographicSize, MaxOrthographicSize);
            controller.SetEntryZoom(EntryOrthographicSize);
            controller.SetTravelZoom(TravelOrthographicSize, TravelDelay);
            controller.SetWorldBounds(bounds);
            return controller;
        }

        private Tilemap CreateLayer(ReferenceEnvironment.TilemapLayer layer, Transform parent)
        {
            GameObject layerObject = new GameObject(layer.Name);
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = layer.LocalPosition;

            Tilemap tilemap = layerObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = layerObject.AddComponent<TilemapRenderer>();
            _prefabs.ConfigureTilemapRenderer(renderer, layer.Renderer);
            _tilemaps.Fill(layer.Tilemap, tilemap, "Environment/" + layer.Name);
            return tilemap;
        }

        private void EnsureInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == BuildingAssetPaths.ScenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(BuildingAssetPaths.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            _report.Count("build settings: Building scene added");
        }
    }
}
