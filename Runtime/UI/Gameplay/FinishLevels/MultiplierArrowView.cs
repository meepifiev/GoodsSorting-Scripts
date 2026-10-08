using DG.Tweening;
using UnityEngine;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class MultiplierArrowView : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private RectTransform[] _slots;
        [SerializeField] private int[] _multipliers = { 2, 3, 5, 3, 2 };
        [SerializeField] private float _sweep = 245f;
        [SerializeField] private float _duration = 1.1f;

        private Tween _tween;

        public bool IsRunning => _tween != null && _tween.IsActive();

        public int CurrentMultiplier
        {
            get
            {
                if (_slots == null || _slots.Length == 0 || _arrow == null)
                {
                    return 1;
                }

                float arrowX = _arrow.anchoredPosition.x;
                int best = 0;
                float bestDistance = float.MaxValue;

                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == null)
                    {
                        continue;
                    }

                    float distance = Mathf.Abs(_slots[i].anchoredPosition.x - arrowX);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }

                return (_multipliers != null && best < _multipliers.Length) ? _multipliers[best] : 1;
            }
        }

        public void StartOscillation()
        {
            if (_arrow == null)
            {
                return;
            }

            _arrow.DOKill();
            _arrow.anchoredPosition = new Vector2(-_sweep, _arrow.anchoredPosition.y);

            _tween = DOTween.To(
                    () => _arrow.anchoredPosition.x,
                    x => _arrow.anchoredPosition = new Vector2(x, _arrow.anchoredPosition.y),
                    _sweep,
                    _duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        public void Stop()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
