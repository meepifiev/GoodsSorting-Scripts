using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _Project.UI.Gameplay
{
    public class StarsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Transform _bumpTarget;

        public void SetStars(int stars)
        {
            _label.text = stars.ToString();
        }

        public void Bump()
        {
            if (_bumpTarget == null)
            {
                return;
            }

            _bumpTarget.DOKill();
            _bumpTarget.localScale = Vector3.one;
            _bumpTarget.DOPunchScale(Vector3.one * 0.35f, 0.28f, 6, 0.8f).SetLink(_bumpTarget.gameObject);
        }
    }
}
