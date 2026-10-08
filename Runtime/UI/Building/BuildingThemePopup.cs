using System;
using _Project.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Building
{
    public class BuildingThemePopup : PopupView
    {
        [SerializeField] private BuildingThemeOptionView[] _options = Array.Empty<BuildingThemeOptionView>();
        [SerializeField] private Button _confirmButton;

        private int _selected;

        public event Action<int> Confirmed;

        public int Selected => _selected;

        protected override void OnEnable()
        {
            base.OnEnable();
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            foreach (BuildingThemeOptionView option in _options)
            {
                option.Clicked += OnOptionClicked;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);

            foreach (BuildingThemeOptionView option in _options)
            {
                option.Clicked -= OnOptionClicked;
            }
        }

        public void Initialize(BuildingThemeOptionView[] options, Button confirmButton)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _confirmButton = confirmButton;
        }

        public void Show(Sprite[] previews)
        {
            if (previews == null)
            {
                throw new ArgumentNullException(nameof(previews));
            }

            for (int i = 0; i < _options.Length; i++)
            {
                _options[i].Bind(i < previews.Length ? previews[i] : null);
            }

            Select(0);
            Show();
        }

        private void Select(int index)
        {
            _selected = index;

            foreach (BuildingThemeOptionView option in _options)
            {
                option.SetSelected(option.Index == index);
            }
        }

        private void OnOptionClicked(int index)
        {
            Select(index);
        }

        private void OnConfirmClicked()
        {
            Confirmed?.Invoke(_selected);
        }
    }
}
