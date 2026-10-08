using System;
using System.Reflection;
using _Project.Core.Time;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class TimeFreezeView : MonoBehaviour
    {
        [SerializeField] private GameObject _filter;
        [SerializeField] private FrostTimerFlipbook _frostBox;
        [SerializeField] private GameObject _fxParticles;

        private ITimeFreeze _timeFreeze;
        private Component _uiParticle;
        private MethodInfo _play;
        private MethodInfo _stop;
        private bool _hasState;
        private bool _frozenState;

        [Inject]
        private void Construct(ITimeFreeze timeFreeze)
        {
            _timeFreeze = timeFreeze ?? throw new ArgumentNullException(nameof(timeFreeze));

            CacheParticle();
            SetParticlesVisible(false);
            _timeFreeze.FrozenChanged += OnFrozenChanged;
            OnFrozenChanged(_timeFreeze.IsFrozen);
        }

        private void OnDestroy()
        {
            if (_timeFreeze != null)
            {
                _timeFreeze.FrozenChanged -= OnFrozenChanged;
            }
        }

        private void OnFrozenChanged(bool frozen)
        {
            if (_filter != null)
            {
                _filter.SetActive(frozen);
            }

            bool changed = _hasState == false ? frozen : frozen != _frozenState;

            if (changed)
            {
                if (frozen)
                {
                    Freeze();
                }
                else
                {
                    Thaw();
                }
            }

            _hasState = true;
            _frozenState = frozen;
        }

        private void Freeze()
        {
            if (_frostBox != null)
            {
                _frostBox.PlayFreeze();
            }

            SetParticlesVisible(true);

            if (_uiParticle != null && _play != null)
            {
                _play.Invoke(_uiParticle, null);
            }
        }

        private void Thaw()
        {
            if (_frostBox != null)
            {
                _frostBox.PlayThaw();
            }

            if (_uiParticle != null && _stop != null)
            {
                _stop.Invoke(_uiParticle, null);
            }

            SetParticlesVisible(false);
        }

        private void CacheParticle()
        {
            if (_fxParticles == null)
            {
                return;
            }

            foreach (Component component in _fxParticles.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name == "UIParticle")
                {
                    _uiParticle = component;
                    break;
                }
            }

            if (_uiParticle == null)
            {
                return;
            }

            Type type = _uiParticle.GetType();
            _play = type.GetMethod("Play", Type.EmptyTypes);
            _stop = type.GetMethod("Stop", Type.EmptyTypes);
        }

        private void SetParticlesVisible(bool visible)
        {
            if (_fxParticles != null)
            {
                _fxParticles.SetActive(visible);
            }
        }
    }
}
