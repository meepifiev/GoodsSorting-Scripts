using System;
using _Project.Core.Building;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Building
{
    public class BuildingTaskMarkerView : MonoBehaviour
    {
        private const float PulseScale = 1.1f;
        private const float PulseDuration = 0.55f;

        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _costLabel;

        private BuildingTaskRef _task;
        private Tween _pulse;

        public event Action<BuildingTaskRef> Clicked;

        public BuildingTaskRef Task => _task;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
            transform.localScale = Vector3.one;
            _pulse = transform.DOScale(PulseScale, PulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
            _pulse?.Kill();
            _pulse = null;
            transform.localScale = Vector3.one;
        }

        public void Initialize(Button button, TextMeshProUGUI costLabel)
        {
            _button = button ?? throw new ArgumentNullException(nameof(button));
            _costLabel = costLabel ?? throw new ArgumentNullException(nameof(costLabel));
        }

        public void Bind(BuildingTaskRef task, Vector2 worldPosition, int cost)
        {
            _task = task;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _costLabel.text = cost.ToString();
        }

        private void OnClicked()
        {
            Clicked?.Invoke(_task);
        }
    }
}
