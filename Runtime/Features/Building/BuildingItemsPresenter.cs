using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Core.Building;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Features.Building
{
    public class BuildingItemsPresenter : IDisposable
    {
        private const float RevealMargin = 3f;
        private const float CameraBoundsMargin = 4f;
        private const float RemoveDuration = 0.25f;
        private const float AppearDuration = 0.45f;
        private const float AppearDelayStep = 0.08f;
        private const float FxHeightOffset = 0.4f;
        private const float SameSpotTolerance = 0.35f;

        private class SpawnedItem
        {
            public int Area;
            public BuildingTaskRef Task;
            public bool Decor;
            public bool After;
            public BuildingItemView View;
            public IsoBox Box;
            public int Layer;
        }

        private readonly IBuildingService _service;
        private readonly BuildingItemCatalog _catalog;
        private readonly BuildingItemProvider _provider;
        private readonly BuildingWorldView _world;
        private readonly IsoProjection _projection;
        private readonly IsoLayout _layout;
        private readonly BuildingAreaBounds _areaBounds;
        private readonly BuildingItemAppearFx _appearFx;
        private readonly List<SpawnedItem> _items = new List<SpawnedItem>();
        private readonly List<IsoBox> _boxes = new List<IsoBox>();
        private readonly List<int> _layers = new List<int>();

        public BuildingItemsPresenter(
            IBuildingService service,
            BuildingItemCatalog catalog,
            BuildingItemProvider provider,
            BuildingWorldView world,
            IsoProjection projection,
            IsoLayout layout,
            BuildingAreaBounds areaBounds,
            BuildingItemAppearFx appearFx)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _areaBounds = areaBounds ?? throw new ArgumentNullException(nameof(areaBounds));
            _appearFx = appearFx;
        }

        public async UniTask LoadAndBuildAsync(CancellationToken cancellationToken)
        {
            await _provider.LoadAsync(cancellationToken);

            RevealUnlockedAreas();
            Rebuild();
            _service.TaskBuilt += OnTaskBuilt;
            _service.DayCompleted += OnDayCompleted;
        }

        public void Dispose()
        {
            _service.TaskBuilt -= OnTaskBuilt;
            _service.DayCompleted -= OnDayCompleted;
        }

        public Vector2 GetTaskFocusPoint(BuildingTaskRef task, bool after)
        {
            BuildingTaskConfig config = _service.GetConfig(task);
            BuildingItemPlacement[] preferred = after ? config.ItemsAfter : config.ItemsBefore;
            BuildingItemPlacement[] fallback = after ? config.ItemsBefore : config.ItemsAfter;
            BuildingItemPlacement[] placements = preferred.Length > 0 ? preferred : fallback;
            Vector2 sum = Vector2.zero;
            int count = 0;

            foreach (BuildingItemPlacement placement in placements)
            {
                if (_catalog.Contains(placement.ItemId) == false)
                {
                    continue;
                }

                Vector3 size = _catalog.Get(placement.ItemId).IsoSize;

                if (placement.Flipped)
                {
                    size = new Vector3(size.y, size.x, size.z);
                }

                sum += _projection.IsoToScreen(new IsoBox(placement.IsoPosition, size).Center);
                count++;
            }

            if (count == 0)
            {
                return _world.CameraController != null
                    ? (Vector2)_world.CameraController.transform.position
                    : Vector2.zero;
            }

            return sum / count;
        }

        private void RevealUnlockedAreas()
        {
            bool any = false;
            Rect union = default;

            for (int areaIndex = 0; areaIndex < _service.AreaCount; areaIndex++)
            {
                if (_service.IsAreaUnlocked(areaIndex) == false)
                {
                    continue;
                }

                BuildingAreaConfig area = _service.GetArea(areaIndex);
                _world.RevealArea(_areaBounds.Compute(area, RevealMargin));
                Rect cameraRect = _areaBounds.Compute(area, CameraBoundsMargin);
                union = any ? _areaBounds.Union(union, cameraRect) : cameraRect;
                any = true;
            }

            if (any && _world.CameraController != null)
            {
                _world.CameraController.SetWorldBounds(union);
            }
        }

        private void Rebuild()
        {
            _items.Clear();
            ClearItemsRoot();

            for (int areaIndex = 0; areaIndex < _service.AreaCount; areaIndex++)
            {
                SpawnArea(areaIndex);
            }

            ApplyDepth();
        }

        private void SpawnArea(int areaIndex)
        {
            BuildingAreaConfig area = _service.GetArea(areaIndex);
            bool completed = _service.IsAreaCompleted(areaIndex);
            bool current = areaIndex == _service.CurrentAreaIndex;
            int currentDay = current ? _service.CurrentDay : -1;

            SpawnPlacements(areaIndex, default, true, completed ? area.DecorAfter : area.DecorBefore, completed, 0);

            for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
            {
                BuildingDayConfig day = area.GetDay(dayIndex);

                for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                {
                    BuildingTaskRef task = new BuildingTaskRef(dayIndex, taskIndex);
                    BuildingTaskConfig config = day.GetTask(taskIndex);

                    if (_service.IsTaskDone(areaIndex, task))
                    {
                        SpawnPlacements(areaIndex, task, false, config.ItemsAfter, true, _service.GetTheme(areaIndex, task));
                        continue;
                    }

                    if (dayIndex <= currentDay)
                    {
                        SpawnPlacements(areaIndex, task, false, config.ItemsBefore, false, 0);
                        continue;
                    }

                    SpawnPlacements(areaIndex, task, false, CollectBrokenPlacements(area, dayIndex, config.ItemsBefore, false), false, 0);
                }
            }
        }

        private void ClearItemsRoot()
        {
            Transform root = _world.ItemsRoot;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        private List<BuildingItemPlacement> CollectBrokenPlacements(
            BuildingAreaConfig area,
            int dayIndex,
            BuildingItemPlacement[] placements,
            bool onEarlierSpot)
        {
            List<BuildingItemPlacement> selected = new List<BuildingItemPlacement>();

            foreach (BuildingItemPlacement placement in placements)
            {
                if (OccupiesEarlierSpot(area, dayIndex, placement) == onEarlierSpot)
                {
                    selected.Add(placement);
                }
            }

            return selected;
        }

        private bool OccupiesEarlierSpot(BuildingAreaConfig area, int dayIndex, BuildingItemPlacement placement)
        {
            for (int earlierDay = 0; earlierDay < dayIndex; earlierDay++)
            {
                BuildingDayConfig day = area.GetDay(earlierDay);

                for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                {
                    BuildingTaskConfig config = day.GetTask(taskIndex);

                    if (ContainsSpot(config.ItemsBefore, placement.IsoPosition)
                        || ContainsSpot(config.ItemsAfter, placement.IsoPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ContainsSpot(BuildingItemPlacement[] placements, Vector3 isoPosition)
        {
            foreach (BuildingItemPlacement placement in placements)
            {
                Vector3 delta = placement.IsoPosition - isoPosition;

                if (Mathf.Abs(delta.x) <= SameSpotTolerance
                    && Mathf.Abs(delta.y) <= SameSpotTolerance
                    && Mathf.Abs(delta.z) <= SameSpotTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnPlacements(
            int areaIndex,
            BuildingTaskRef task,
            bool decor,
            IReadOnlyList<BuildingItemPlacement> placements,
            bool after,
            int theme)
        {
            foreach (BuildingItemPlacement placement in placements)
            {
                if (_catalog.Contains(placement.ItemId) == false || _provider.Contains(placement.ItemId) == false)
                {
                    continue;
                }

                BuildingItemView view = UnityEngine.Object.Instantiate(_provider.Get(placement.ItemId), _world.ItemsRoot);
                view.SetTheme(theme);
                BuildingItemOverrides.Apply(view, placement.ChildOverrides);
                view.SetFlipped(placement.Flipped);

                _items.Add(new SpawnedItem
                {
                    Area = areaIndex,
                    Task = task,
                    Decor = decor,
                    After = after,
                    View = view,
                    Box = new IsoBox(placement.IsoPosition, view.FlippedIsoSize),
                    Layer = placement.SortingLayer
                });
            }
        }

        private void ApplyDepth()
        {
            _boxes.Clear();
            _layers.Clear();

            foreach (SpawnedItem item in _items)
            {
                _boxes.Add(item.Box);
                _layers.Add(item.Layer);
            }

            float[] depths = _layout.ComputeDepths(_boxes, _layers);

            for (int i = 0; i < _items.Count; i++)
            {
                SpawnedItem item = _items[i];
                item.View.SetScreenPosition(_projection.IsoToScreen(item.Box.Min), depths[i]);
            }
        }

        private void OnTaskBuilt(BuildingTaskRef task)
        {
            int areaIndex = _service.CurrentAreaIndex;
            RemoveItems(item => item.Area == areaIndex && item.Decor == false && item.After == false && item.Task.Equals(task));

            int firstNew = _items.Count;
            SpawnPlacements(areaIndex, task, false, _service.GetConfig(task).ItemsAfter, true, _service.GetTheme(task));
            ApplyDepth();
            AnimateAppearFrom(firstNew);
        }

        private void OnDayCompleted(BuildingDayResult result)
        {
            if (result.AreaCompleted)
            {
                SwapDecor(result.AreaIndex);
                RevealUnlockedAreas();
                return;
            }

            BuildingAreaConfig area = _service.GetArea(result.AreaIndex);

            if (result.NextDay >= area.DayCount)
            {
                return;
            }

            int firstNew = _items.Count;
            BuildingDayConfig day = area.GetDay(result.NextDay);

            for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
            {
                BuildingTaskRef task = new BuildingTaskRef(result.NextDay, taskIndex);
                SpawnPlacements(result.AreaIndex, task, false, CollectBrokenPlacements(area, result.NextDay, day.GetTask(taskIndex).ItemsBefore, true), false, 0);
            }

            if (_items.Count == firstNew)
            {
                return;
            }

            ApplyDepth();
            AnimateAppearFrom(firstNew);
        }

        private void SwapDecor(int areaIndex)
        {
            BuildingAreaConfig area = _service.GetArea(areaIndex);
            RemoveItems(item => item.Area == areaIndex && item.Decor && item.After == false);

            int firstNew = _items.Count;
            SpawnPlacements(areaIndex, default, true, area.DecorAfter, true, 0);
            ApplyDepth();
            AnimateAppearFrom(firstNew);
        }

        private void RemoveItems(Predicate<SpawnedItem> match)
        {
            List<SpawnedItem> removed = _items.FindAll(match);

            foreach (SpawnedItem item in removed)
            {
                _items.Remove(item);
                AnimateRemove(item.View);
            }
        }

        private void AnimateAppearFrom(int firstNew)
        {
            for (int i = firstNew; i < _items.Count; i++)
            {
                AnimateAppear(_items[i].View, (i - firstNew) * AppearDelayStep);
            }
        }

        private void AnimateRemove(BuildingItemView view)
        {
            if (view == null)
            {
                return;
            }

            Transform visual = view.VisualRoot != null ? view.VisualRoot : view.transform;
            visual.DOScale(Vector3.zero, RemoveDuration)
                .SetEase(Ease.InBack)
                .SetLink(view.gameObject)
                .OnComplete(() => UnityEngine.Object.Destroy(view.gameObject));
        }

        private void AnimateAppear(BuildingItemView view, float delay)
        {
            Transform visual = view.VisualRoot != null ? view.VisualRoot : view.transform;
            Vector3 targetScale = visual.localScale;
            visual.localScale = Vector3.zero;
            visual.DOScale(targetScale, AppearDuration)
                .SetEase(Ease.OutBack)
                .SetDelay(delay)
                .SetLink(view.gameObject)
                .OnStart(() => PlayFx(view));
        }

        private void PlayFx(BuildingItemView view)
        {
            if (_appearFx == null || view == null)
            {
                return;
            }

            Vector3 position = view.transform.position;
            position.y += FxHeightOffset;
            position.z = _appearFx.transform.position.z;
            _appearFx.Play(position);
        }
    }
}
