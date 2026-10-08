using System;
using System.Collections.Generic;
using _Project.Core.Building;
using UnityEngine;

namespace _Project.UI.Building
{
    public readonly struct BuildingTaskMarkerData
    {
        public readonly BuildingTaskRef Task;
        public readonly Vector2 Position;
        public readonly int Cost;

        public BuildingTaskMarkerData(BuildingTaskRef task, Vector2 position, int cost)
        {
            Task = task;
            Position = position;
            Cost = cost;
        }
    }

    public class BuildingTaskMarkersView : MonoBehaviour
    {
        [SerializeField] private BuildingTaskMarkerView _template;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _heightOffset = 1.2f;

        private readonly List<BuildingTaskMarkerView> _markers = new List<BuildingTaskMarkerView>();

        public event Action<BuildingTaskRef> BuildRequested;

        private void OnDestroy()
        {
            foreach (BuildingTaskMarkerView marker in _markers)
            {
                if (marker != null)
                {
                    marker.Clicked -= OnMarkerClicked;
                }
            }
        }

        public void Initialize(BuildingTaskMarkerView template, CanvasGroup canvasGroup, float heightOffset)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _canvasGroup = canvasGroup ?? throw new ArgumentNullException(nameof(canvasGroup));
            _heightOffset = heightOffset;
        }

        public void Show(IReadOnlyList<BuildingTaskMarkerData> markers)
        {
            if (markers == null)
            {
                throw new ArgumentNullException(nameof(markers));
            }

            while (_markers.Count < markers.Count)
            {
                _markers.Add(CreateMarker());
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                bool used = i < markers.Count;
                _markers[i].gameObject.SetActive(used);

                if (used)
                {
                    BuildingTaskMarkerData data = markers[i];
                    _markers[i].Bind(data.Task, data.Position + Vector2.up * _heightOffset, data.Cost);
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        private BuildingTaskMarkerView CreateMarker()
        {
            BuildingTaskMarkerView marker = Instantiate(_template, transform);
            marker.name = _template.name.Replace("Template", string.Empty);
            marker.Clicked += OnMarkerClicked;
            return marker;
        }

        private void OnMarkerClicked(BuildingTaskRef task)
        {
            BuildRequested?.Invoke(task);
        }
    }
}
