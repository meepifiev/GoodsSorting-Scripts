using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Features.Building
{
    public class BuildingWorldView : MonoBehaviour
    {
        private const string LockedLayerName = "Tilemap_lockarea";
        private const string CloudLayerPrefix = "Tilemap_cloud";

        [SerializeField] private Grid _environmentGrid;
        [SerializeField] private Transform _itemsRoot;
        [SerializeField] private Tilemap _lockedAreasClouds;
        [SerializeField] private BuildingCameraController _cameraController;

        private List<Tilemap> _cloudTilemaps;

        public Grid EnvironmentGrid => _environmentGrid;
        public Transform ItemsRoot => _itemsRoot;
        public Tilemap LockedAreasClouds => _lockedAreasClouds;
        public BuildingCameraController CameraController => _cameraController;

        private IReadOnlyList<Tilemap> CloudTilemaps
        {
            get
            {
                if (_cloudTilemaps == null)
                {
                    CollectCloudTilemaps();
                }

                return _cloudTilemaps;
            }
        }

        public void Initialize(
            Grid environmentGrid,
            Transform itemsRoot,
            Tilemap lockedAreasClouds,
            BuildingCameraController cameraController)
        {
            _environmentGrid = environmentGrid;
            _itemsRoot = itemsRoot;
            _lockedAreasClouds = lockedAreasClouds;
            _cameraController = cameraController;
            _cloudTilemaps = null;
        }

        public void BindEnvironment(Grid environmentGrid, Tilemap lockedAreasClouds)
        {
            _environmentGrid = environmentGrid;
            _lockedAreasClouds = lockedAreasClouds;
            _cloudTilemaps = null;
        }

        public void RevealArea(Rect worldRect)
        {
            SetCloudsRendered(true);

            foreach (Tilemap clouds in CloudTilemaps)
            {
                ClearTilesInside(clouds, worldRect);
            }
        }

        public void SetCloudsRendered(bool rendered)
        {
            foreach (Tilemap clouds in CloudTilemaps)
            {
                TilemapRenderer renderer = clouds.GetComponent<TilemapRenderer>();

                if (renderer != null)
                {
                    renderer.enabled = rendered;
                }
            }
        }

        private void ClearTilesInside(Tilemap clouds, Rect worldRect)
        {
            BoundsInt cells = clouds.cellBounds;

            foreach (Vector3Int cell in cells.allPositionsWithin)
            {
                if (clouds.HasTile(cell) == false)
                {
                    continue;
                }

                Vector3 center = clouds.GetCellCenterWorld(cell);

                if (worldRect.Contains(new Vector2(center.x, center.y)))
                {
                    clouds.SetTile(cell, null);
                }
            }
        }

        private void CollectCloudTilemaps()
        {
            _cloudTilemaps = new List<Tilemap>();

            if (_environmentGrid == null)
            {
                return;
            }

            foreach (Tilemap tilemap in _environmentGrid.GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap.name == LockedLayerName || tilemap.name.StartsWith(CloudLayerPrefix))
                {
                    _cloudTilemaps.Add(tilemap);
                }
            }
        }
    }
}
