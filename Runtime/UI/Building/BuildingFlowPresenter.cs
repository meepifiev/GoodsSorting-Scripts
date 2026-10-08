using System;
using System.Collections.Generic;
using _Project.Core.Building;
using _Project.Core.Lives;
using _Project.Core.StateMachine;
using _Project.Features.Building;
using _Project.UI.Common;
using UnityEngine;

namespace _Project.UI.Building
{
    public class BuildingFlowPresenter : IDisposable
    {
        private const float FocusDuration = 0.6f;
        private const int ThemeOptionCount = 3;

        private readonly IBuildingService _service;
        private readonly BuildingItemProvider _provider;
        private readonly BuildingHudView _hud;
        private readonly BuildingThemePopup _themePopup;
        private readonly BuildingNewDayBanner _newDayBanner;
        private readonly BuildingTaskMarkersView _markers;
        private readonly List<BuildingTaskMarkerData> _markerData = new List<BuildingTaskMarkerData>();
        private readonly BuildingItemsPresenter _items;
        private readonly BuildingCameraController _camera;
        private readonly IGameLauncher _launcher;
        private readonly ILivesService _lives;
        private readonly OutOfLivesPopup _outOfLivesPopup;

        private BuildingTaskRef _pendingTask;

        public BuildingFlowPresenter(
            IBuildingService service,
            BuildingItemProvider provider,
            BuildingHudView hud,
            BuildingThemePopup themePopup,
            BuildingNewDayBanner newDayBanner,
            BuildingTaskMarkersView markers,
            BuildingItemsPresenter items,
            BuildingCameraController camera,
            IGameLauncher launcher,
            ILivesService lives,
            OutOfLivesPopup outOfLivesPopup)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _themePopup = themePopup ?? throw new ArgumentNullException(nameof(themePopup));
            _newDayBanner = newDayBanner ?? throw new ArgumentNullException(nameof(newDayBanner));
            _markers = markers ?? throw new ArgumentNullException(nameof(markers));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _outOfLivesPopup = outOfLivesPopup ?? throw new ArgumentNullException(nameof(outOfLivesPopup));
        }

        public void Build()
        {
            _hud.ExitClicked += OnExitClicked;
            _themePopup.Closed += OnThemePopupClosed;
            _themePopup.Confirmed += OnThemeConfirmed;
            _markers.BuildRequested += OnBuildRequested;
            _service.DayCompleted += OnDayCompleted;
            _service.Changed += RefreshMarkers;

            _themePopup.Hide();
            RefreshMarkers();
            _camera.ResetView(_markerData.Count > 0 ? _markerData[0].Position : (Vector2)_camera.transform.position);
            UpdateCameraInput();
        }

        public void Dispose()
        {
            _hud.ExitClicked -= OnExitClicked;
            _themePopup.Closed -= OnThemePopupClosed;
            _themePopup.Confirmed -= OnThemeConfirmed;
            _markers.BuildRequested -= OnBuildRequested;
            _service.DayCompleted -= OnDayCompleted;
            _service.Changed -= RefreshMarkers;
        }

        private void UpdateCameraInput()
        {
            bool anyPopup = _themePopup.IsShown;
            _camera.SetInputEnabled(anyPopup == false);
            _markers.SetInteractable(anyPopup == false);
        }

        private void RefreshMarkers()
        {
            _markerData.Clear();

            foreach (BuildingTaskRef task in _service.GetCurrentDayTasks())
            {
                if (_service.GetState(task) == BuildingTaskState.Available)
                {
                    _markerData.Add(new BuildingTaskMarkerData(task, _items.GetTaskFocusPoint(task, false), _service.GetCost(task)));
                    break;
                }
            }

            _markers.Show(_markerData);
        }

        private void Build(BuildingTaskRef task, int theme)
        {
            if (_service.TryBuild(task, theme) == false)
            {
                return;
            }

            UpdateCameraInput();

            if (_markerData.Count > 0)
            {
                _camera.TravelTo(_markerData[0].Position);
                return;
            }

            _camera.MoveTo(_items.GetTaskFocusPoint(task, true), FocusDuration);
        }

        private Sprite[] CollectThemePreviews(BuildingTaskRef task)
        {
            foreach (BuildingItemPlacement placement in _service.GetConfig(task).ItemsAfter)
            {
                if (_provider.Contains(placement.ItemId) == false)
                {
                    continue;
                }

                BuildingItemView prefab = _provider.Get(placement.ItemId);

                if (prefab.ThemeCount <= 1)
                {
                    continue;
                }

                Sprite[] previews = new Sprite[Mathf.Min(prefab.ThemeCount, ThemeOptionCount)];

                for (int i = 0; i < previews.Length; i++)
                {
                    previews[i] = prefab.GetThemeSprite(i);
                }

                return previews;
            }

            return Array.Empty<Sprite>();
        }

        private void OnExitClicked()
        {
            _launcher.GoToMenu();
        }

        private void OnBuildRequested(BuildingTaskRef task)
        {
            if (_service.CanAfford(task) == false)
            {
                if (_lives.TrySpendLife())
                {
                    _launcher.StartGame();
                    return;
                }

                _outOfLivesPopup.Show(OnLivesGranted);
                return;
            }

            Sprite[] previews = CollectThemePreviews(task);

            if (previews.Length > 1)
            {
                _pendingTask = task;
                _themePopup.Show(previews);
                UpdateCameraInput();
                return;
            }

            Build(task, 0);
        }

        private void OnLivesGranted()
        {
            if (_lives.TrySpendLife())
            {
                _launcher.StartGame();
            }
        }

        private void OnThemePopupClosed()
        {
            _themePopup.Hide();
            UpdateCameraInput();
        }

        private void OnThemeConfirmed(int theme)
        {
            _themePopup.Hide();
            Build(_pendingTask, theme);
        }

        private void OnDayCompleted(BuildingDayResult result)
        {
            _newDayBanner.Show(result);
        }
    }
}
