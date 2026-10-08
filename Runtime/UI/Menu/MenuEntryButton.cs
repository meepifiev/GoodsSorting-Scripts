using System;
using _Project.Core.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Menu
{
    public class MenuEntryButton : MonoBehaviour
    {
        [SerializeField] private MenuPopup _popup;
        [SerializeField] private Button _button;

        public event Action<MenuPopup> Clicked;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(_popup);
        }
    }
}
