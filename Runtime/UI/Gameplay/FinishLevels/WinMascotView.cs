using DG.Tweening;
using UnityEngine;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class WinMascotView : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _bobAmplitude = 20f;
        [SerializeField] private float _bobDuration = 1.4f;
        [SerializeField] private float _swayAngle = 4f;
        [SerializeField] private float _swayDuration = 2.2f;

        private RectTransform Target => _target != null ? _target : (RectTransform)transform;

        private Vector2 _basePos;
        private bool _captured;

        private void OnEnable()
        {
            RectTransform rt = Target;

            if (_captured == false)
            {
                _basePos = rt.anchoredPosition;
                _captured = true;
            }

            rt.DOKill();
            rt.anchoredPosition = _basePos;
            rt.localScale = Vector3.one * 0.6f;
            rt.localRotation = Quaternion.identity;

            rt.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetLink(gameObject);

            DOTween.To(
                    () => rt.anchoredPosition.y,
                    y => rt.anchoredPosition = new Vector2(_basePos.x, y),
                    _basePos.y + _bobAmplitude,
                    _bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);

            rt.DORotate(new Vector3(0f, 0f, _swayAngle), _swayDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(new Vector3(0f, 0f, -_swayAngle))
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            Target.DOKill();
        }
    }
}
