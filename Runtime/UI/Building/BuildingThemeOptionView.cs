using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Building
{
    public class BuildingThemeOptionView : MonoBehaviour
    {
        [SerializeField] private int _index;
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _preview;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _selectedSprite;

        private Vector2 _baseSize;
        private bool _baseCaptured;

        public event Action<int> Clicked;

        public int Index => _index;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        public void Initialize(int index, Button button, Image frame, Image preview, Sprite normalSprite, Sprite selectedSprite)
        {
            _index = index;
            _button = button;
            _frame = frame;
            _preview = preview;
            _normalSprite = normalSprite;
            _selectedSprite = selectedSprite;
        }

        public void Bind(Sprite preview)
        {
            _preview.sprite = preview;
            _preview.enabled = preview != null;
            gameObject.SetActive(preview != null);
        }

        public void SetSelected(bool selected)
        {
            if (_frame == null)
            {
                return;
            }

            CaptureBaseSize();

            _frame.sprite = selected ? _selectedSprite : _normalSprite;

            if (_baseCaptured && _normalSprite != null && _selectedSprite != null && _normalSprite.rect.width > 0f)
            {
                float ratio = selected ? _selectedSprite.rect.width / _normalSprite.rect.width : 1f;
                _frame.rectTransform.sizeDelta = _baseSize * ratio;
            }
        }

        private void CaptureBaseSize()
        {
            if (_baseCaptured || _frame == null)
            {
                return;
            }

            _baseSize = _frame.rectTransform.sizeDelta;
            _baseCaptured = true;
        }

        private void OnClicked()
        {
            Clicked?.Invoke(_index);
        }
    }
}
