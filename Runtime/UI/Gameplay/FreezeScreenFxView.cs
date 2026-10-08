using System;
using System.Reflection;
using _Project.Core.Time;
using _Project.Features.Cameras;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class FreezeScreenFxView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Camera _camera;
        [SerializeField] private string _animationName = "animation";
        [SerializeField] private bool _loop;
        [SerializeField] private Image _borderOverlay;
        [SerializeField] private float _borderFadeIn = 0.25f;
        [SerializeField] private float _borderFadeOut = 0.35f;

        private ITimeFreeze _timeFreeze;
        private IMainCameraProvider _cameraProvider;
        private Component _skeletonAnimation;
        private Tween _borderTween;

        [Inject]
        private void Construct(ITimeFreeze timeFreeze, IMainCameraProvider cameraProvider)
        {
            _timeFreeze = timeFreeze ?? throw new ArgumentNullException(nameof(timeFreeze));
            _cameraProvider = cameraProvider ?? throw new ArgumentNullException(nameof(cameraProvider));

            CacheSkeleton();
            SetVisible(false);
            HideBorderImmediate();
            _timeFreeze.FrozenChanged += OnFrozenChanged;
            OnFrozenChanged(_timeFreeze.IsFrozen);
        }

        private void OnDestroy()
        {
            _borderTween?.Kill();

            if (_timeFreeze != null)
            {
                _timeFreeze.FrozenChanged -= OnFrozenChanged;
            }
        }

        private void OnFrozenChanged(bool frozen)
        {
            if (frozen)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }

        private void Play()
        {
            CenterOnCamera();
            SetVisible(true);
            PlaySpine();
            ShowBorder();
        }

        private void Stop()
        {
            SetVisible(false);
            HideBorder();
        }

        private void ShowBorder()
        {
            if (_borderOverlay == null)
            {
                return;
            }

            _borderTween?.Kill();
            _borderOverlay.gameObject.SetActive(true);
            SetBorderAlpha(0f);
            _borderTween = DOTween.To(GetBorderAlpha, SetBorderAlpha, 1f, _borderFadeIn).SetEase(Ease.OutSine);
        }

        private void HideBorder()
        {
            if (_borderOverlay == null)
            {
                return;
            }

            _borderTween?.Kill();
            _borderTween = DOTween.To(GetBorderAlpha, SetBorderAlpha, 0f, _borderFadeOut)
                .SetEase(Ease.InSine)
                .OnComplete(() => _borderOverlay.gameObject.SetActive(false));
        }

        private void HideBorderImmediate()
        {
            if (_borderOverlay == null)
            {
                return;
            }

            SetBorderAlpha(0f);
            _borderOverlay.gameObject.SetActive(false);
        }

        private float GetBorderAlpha()
        {
            return _borderOverlay != null ? _borderOverlay.color.a : 0f;
        }

        private void SetBorderAlpha(float alpha)
        {
            if (_borderOverlay == null)
            {
                return;
            }

            Color color = _borderOverlay.color;
            color.a = alpha;
            _borderOverlay.color = color;
        }

        private void PlaySpine()
        {
            if (_skeletonAnimation == null)
            {
                return;
            }

            PropertyInfo stateProperty = _skeletonAnimation.GetType().GetProperty("AnimationState");
            object state = stateProperty != null ? stateProperty.GetValue(_skeletonAnimation) : null;

            if (state == null)
            {
                return;
            }

            MethodInfo setAnimation = state.GetType().GetMethod(
                "SetAnimation",
                new[] { typeof(int), typeof(string), typeof(bool) });

            if (setAnimation != null)
            {
                setAnimation.Invoke(state, new object[] { 0, _animationName, _loop });
            }
        }

        private void CenterOnCamera()
        {
            Camera camera = _camera != null ? _camera : _cameraProvider.Camera;

            if (camera == null)
            {
                return;
            }

            Vector3 cameraPosition = camera.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
        }

        private void CacheSkeleton()
        {
            GameObject source = _root != null ? _root : gameObject;

            foreach (Component component in source.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name == "SkeletonAnimation")
                {
                    _skeletonAnimation = component;
                    break;
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }
        }
    }
}
