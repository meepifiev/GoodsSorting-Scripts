using System;
using _Project.Core.Menu;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuTabElementView : MonoBehaviour
    {
        [SerializeField] private GameObject _element;
        [SerializeField] private MenuTab _visibleTab;

        private IMenuNavigator _navigator;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            _navigator.TabChanged += OnTabChanged;
            Redraw(_navigator.CurrentTab);
        }

        private void OnDisable()
        {
            _navigator.TabChanged -= OnTabChanged;
        }

        private void Redraw(MenuTab tab)
        {
            if (_element != null)
            {
                _element.SetActive(tab == _visibleTab);
            }
        }

        private void OnTabChanged(MenuTab tab)
        {
            Redraw(tab);
        }
    }
}
