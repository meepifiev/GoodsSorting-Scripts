using System;
using _Project.Core.Menu;
using Spine.Unity;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class NavSpineView : MonoBehaviour
    {
        [SerializeField] private MenuTab[] _tabs;
        [SerializeField] private SkeletonGraphic[] _skeletons;
        [SerializeField] private string[] _icons;

        private IMenuNavigator _navigator;
        private int _activeIndex = -1;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            _navigator.TabChanged += OnTabChanged;
        }

        private void OnDisable()
        {
            _navigator.TabChanged -= OnTabChanged;
        }

        private void Start()
        {
            _activeIndex = IndexOf(_navigator.CurrentTab);

            for (int i = 0; i < _skeletons.Length; i++)
            {
                PlayIdle(i, i == _activeIndex);
            }
        }

        private int IndexOf(MenuTab tab)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == tab)
                {
                    return i;
                }
            }

            return -1;
        }

        private void PlayIdle(int index, bool isActive)
        {
            SkeletonGraphic skeleton = _skeletons[index];

            if (skeleton == null || skeleton.AnimationState == null)
            {
                return;
            }

            string idle = _icons[index] + (isActive ? "_OnIdle" : "_OffIdle");
            skeleton.AnimationState.SetAnimation(0, idle, true);
        }

        private void PlayTransition(int index, bool isActive)
        {
            SkeletonGraphic skeleton = _skeletons[index];

            if (skeleton == null || skeleton.AnimationState == null)
            {
                return;
            }

            string enter = _icons[index] + (isActive ? "_On" : "_Off");
            string idle = _icons[index] + (isActive ? "_OnIdle" : "_OffIdle");
            skeleton.AnimationState.SetAnimation(0, enter, false);
            skeleton.AnimationState.AddAnimation(0, idle, true, 0f);
        }

        private void OnTabChanged(MenuTab tab)
        {
            int next = IndexOf(tab);

            if (next == _activeIndex)
            {
                return;
            }

            if (_activeIndex >= 0 && _activeIndex < _skeletons.Length)
            {
                PlayTransition(_activeIndex, false);
            }

            if (next >= 0)
            {
                PlayTransition(next, true);
            }

            _activeIndex = next;
        }
    }
}
