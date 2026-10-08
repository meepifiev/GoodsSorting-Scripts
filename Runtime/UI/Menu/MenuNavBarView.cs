using System;
using _Project.Core.Menu;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuNavBarView : MonoBehaviour
    {
        private const float BarWidth = 1080f;
        private const float ActiveCellWidth = 360f;
        private const float SlideSpeed = 10f;

        [SerializeField] private MenuNavButton[] _buttons;
        [SerializeField] private RectTransform _selectionFrame;

        private float[] _targetX;
        private float _frameTargetX;

        private IMenuNavigator _navigator;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void Awake()
        {
            _targetX = new float[_buttons.Length];
        }

        private void OnEnable()
        {
            foreach (MenuNavButton button in _buttons)
            {
                button.Clicked += OnButtonClicked;
            }

            _navigator.TabChanged += OnTabChanged;
            Redraw(_navigator.CurrentTab, true);
        }

        private void OnDisable()
        {
            foreach (MenuNavButton button in _buttons)
            {
                button.Clicked -= OnButtonClicked;
            }

            _navigator.TabChanged -= OnTabChanged;
        }

        private void Update()
        {
            float step = SlideSpeed * Time.deltaTime;

            for (int i = 0; i < _buttons.Length; i++)
            {
                RectTransform buttonRect = (RectTransform)_buttons[i].transform;
                Vector2 position = buttonRect.anchoredPosition;
                position.x = Mathf.Lerp(position.x, _targetX[i], step);
                buttonRect.anchoredPosition = position;
            }

            if (_selectionFrame != null)
            {
                Vector2 framePosition = _selectionFrame.anchoredPosition;
                framePosition.x = Mathf.Lerp(framePosition.x, _frameTargetX, step);
                _selectionFrame.anchoredPosition = framePosition;
            }
        }

        private void Redraw(MenuTab tab, bool instant)
        {
            float inactiveCellWidth = (BarWidth - ActiveCellWidth) / (_buttons.Length - 1);
            float cursor = -BarWidth / 2f;

            for (int i = 0; i < _buttons.Length; i++)
            {
                MenuNavButton button = _buttons[i];
                bool isSelected = button.Tab == tab;
                float cellWidth = isSelected ? ActiveCellWidth : inactiveCellWidth;
                float center = cursor + cellWidth / 2f;
                cursor += cellWidth;

                _targetX[i] = center;
                button.SetSelected(isSelected, instant);

                if (isSelected)
                {
                    _frameTargetX = center;
                }

                if (instant)
                {
                    RectTransform buttonRect = (RectTransform)button.transform;
                    Vector2 position = buttonRect.anchoredPosition;
                    position.x = center;
                    buttonRect.anchoredPosition = position;
                }
            }

            if (instant && _selectionFrame != null)
            {
                Vector2 framePosition = _selectionFrame.anchoredPosition;
                framePosition.x = _frameTargetX;
                _selectionFrame.anchoredPosition = framePosition;
            }
        }

#if UNITY_EDITOR
        public void EditorPreviewSelect(MenuTab tab)
        {
            if (_targetX == null || _targetX.Length != _buttons.Length)
            {
                _targetX = new float[_buttons.Length];
            }

            Redraw(tab, true);
        }
#endif

        private void OnButtonClicked(MenuTab tab)
        {
            _navigator.GoToTab(tab);
        }

        private void OnTabChanged(MenuTab tab)
        {
            Redraw(tab, false);
        }
    }
}
