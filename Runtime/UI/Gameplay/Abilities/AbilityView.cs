using System;
using _Project.Core.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Gameplay.Abilities
{
    public class AbilityView : MonoBehaviour
    {
        [SerializeField] private AbilityType _abilityType;
        [SerializeField] private Button _button;
        [SerializeField] private Image _buttonImage;
        [SerializeField] private Sprite _enabledButtonSprite;
        [SerializeField] private Sprite _disabledButtonSprite;
        [SerializeField] private GameObject _unlockedRoot;
        [SerializeField] private GameObject _lockedRoot;
        [SerializeField] private TextMeshProUGUI _lockLevelText;
        [SerializeField] private GameObject _freeObject;
        [SerializeField] private GameObject _plusObject;
        [SerializeField] private GameObject _plusIcon;
        [SerializeField] private TextMeshProUGUI _countText;

        public AbilityType AbilityType => _abilityType;

        public event Action Clicked;

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            Clicked?.Invoke();
        }

        public void SetLocked(bool locked, string lockLabel)
        {
            if (_lockedRoot != null)
            {
                _lockedRoot.SetActive(locked);
            }

            if (_unlockedRoot != null)
            {
                _unlockedRoot.SetActive(locked == false);
            }

            if (_buttonImage != null)
            {
                Sprite target = locked ? _disabledButtonSprite : _enabledButtonSprite;

                if (target != null)
                {
                    _buttonImage.sprite = target;
                }
            }

            if (locked && _lockLevelText != null)
            {
                _lockLevelText.text = lockLabel;
            }
        }

        public void SetState(int count, bool isFree)
        {
            bool hasCount = count > 0;

            if (_freeObject != null)
            {
                _freeObject.SetActive(isFree);
            }

            if (_plusObject != null)
            {
                _plusObject.SetActive(isFree == false);
            }

            if (_countText != null)
            {
                _countText.gameObject.SetActive(isFree == false && hasCount);

                if (hasCount)
                {
                    _countText.text = count.ToString();
                }
            }

            if (_plusIcon != null)
            {
                _plusIcon.SetActive(isFree == false && hasCount == false);
            }
        }
    }
}
