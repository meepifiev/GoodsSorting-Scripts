using DG.Tweening;
using UnityEngine;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class WinEntranceAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform[] _elements;
        [SerializeField] private float _stagger = 0.11f;
        [SerializeField] private float _duration = 0.34f;

        public void Play()
        {
            if (_elements == null)
            {
                return;
            }

            float delay = 0f;

            foreach (RectTransform element in _elements)
            {
                if (element == null)
                {
                    continue;
                }

                element.DOKill();
                element.localScale = Vector3.zero;
                element.DOScale(Vector3.one, _duration).SetEase(Ease.OutBack).SetDelay(delay).SetLink(element.gameObject);
                delay += _stagger;
            }
        }
    }
}
