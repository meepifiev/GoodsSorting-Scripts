using System;
using System.Collections.Generic;
using _Project.Core.Menu;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuTabHubView : MonoBehaviour
    {
        private const float SlideSpeed = 12f;

        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private MenuTab[] _tabOrder;

        private readonly List<RectTransform> _panels = new List<RectTransform>();

        private IMenuNavigator _navigator;
        private MenuTab _currentTab;
        private float _targetX;
        private bool _snapped;
        private float _laidOutWidth;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            _navigator.TabChanged += OnTabChanged;
            _currentTab = _navigator.CurrentTab;
            _snapped = false;
        }

        private void OnDisable()
        {
            _navigator.TabChanged -= OnTabChanged;
        }

        private void Update()
        {
            float width = _viewport.rect.width;

            if (width <= 0f)
            {
                return;
            }

            if (Mathf.Abs(width - _laidOutWidth) > 0.5f)
            {
                LayoutPanels(width);
                SnapTo(_currentTab);
                _snapped = true;
                return;
            }

            if (_snapped == false)
            {
                SnapTo(_currentTab);
                _snapped = true;
                return;
            }

            Vector2 position = _content.anchoredPosition;
            position.x = Mathf.Lerp(position.x, _targetX, SlideSpeed * Time.deltaTime);
            _content.anchoredPosition = position;
        }

        private void LayoutPanels(float width)
        {
            _panels.Clear();

            for (int i = 0; i < _content.childCount; i++)
            {
                if (_content.GetChild(i) is RectTransform panel)
                {
                    _panels.Add(panel);
                }
            }

            _panels.Sort(CompareByPosition);

            for (int i = 0; i < _panels.Count; i++)
            {
                RectTransform panel = _panels[i];
                panel.anchorMin = new Vector2(0f, 0f);
                panel.anchorMax = new Vector2(0f, 1f);
                panel.pivot = new Vector2(0f, 0.5f);
                panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);
                panel.anchoredPosition = new Vector2(i * width, panel.anchoredPosition.y);
            }

            _content.sizeDelta = new Vector2(_panels.Count * width, _content.sizeDelta.y);
            _laidOutWidth = width;
        }

        private int CompareByPosition(RectTransform first, RectTransform second)
        {
            return first.anchoredPosition.x.CompareTo(second.anchoredPosition.x);
        }

        private void SnapTo(MenuTab tab)
        {
            _targetX = TargetPositionFor(tab);
            Vector2 position = _content.anchoredPosition;
            position.x = _targetX;
            _content.anchoredPosition = position;
        }

        private void MoveTo(MenuTab tab)
        {
            _targetX = TargetPositionFor(tab);
        }

        private float TargetPositionFor(MenuTab tab)
        {
            int index = Mathf.Max(IndexOf(tab), 0);
            return -index * _viewport.rect.width;
        }

        private int IndexOf(MenuTab tab)
        {
            for (int i = 0; i < _tabOrder.Length; i++)
            {
                if (_tabOrder[i] == tab)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnTabChanged(MenuTab tab)
        {
            if (IndexOf(tab) < 0)
            {
                return;
            }

            _currentTab = tab;

            if (_snapped)
            {
                MoveTo(tab);
            }
        }
    }
}
