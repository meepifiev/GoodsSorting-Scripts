using System;
using _Project.Core.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Menu
{
    public class MenuNavButton : MonoBehaviour
    {
        private const float SelectedIconScale = 0.45f;
        private const float UnselectedIconScale = 0.325f;
        private const float SelectedIconHeight = 80f;
        private const float UnselectedIconHeight = 0f;
        private const float AnimationSpeed = 10f;

        [SerializeField] private MenuTab _tab;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private GameObject _label;
        [SerializeField] private RectTransform _icon;

        private float _targetScale = UnselectedIconScale;
        private float _targetHeight = UnselectedIconHeight;

        public event Action<MenuTab> Clicked;

        public MenuTab Tab => _tab;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void Update()
        {
            if (_icon == null)
            {
                return;
            }

            float step = AnimationSpeed * Time.deltaTime;
            float scale = Mathf.Lerp(_icon.localScale.x, _targetScale, step);
            float height = Mathf.Lerp(_icon.anchoredPosition.y, _targetHeight, step);
            _icon.localScale = new Vector3(scale, scale, 1f);
            _icon.anchoredPosition = new Vector2(_icon.anchoredPosition.x, height);
        }

        public void SetSelected(bool isSelected, bool instant)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(isSelected);
            }

            if (_label != null)
            {
                _label.SetActive(isSelected);
            }

            _targetScale = isSelected ? SelectedIconScale : UnselectedIconScale;
            _targetHeight = isSelected ? SelectedIconHeight : UnselectedIconHeight;

            if (instant && _icon != null)
            {
                _icon.localScale = new Vector3(_targetScale, _targetScale, 1f);
                _icon.anchoredPosition = new Vector2(_icon.anchoredPosition.x, _targetHeight);
            }
        }

        private void OnButtonClicked()
        {
            Clicked?.Invoke(_tab);
        }
    }
}
