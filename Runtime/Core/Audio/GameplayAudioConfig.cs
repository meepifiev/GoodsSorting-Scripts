using _Project.Core.Abilities;
using UnityEngine;

namespace _Project.Core.Audio
{
    [CreateAssetMenu(fileName = "GameplayAudioConfig", menuName = "_Project/Audio/Gameplay Audio Config", order = 1)]
    public class GameplayAudioConfig : ScriptableObject
    {
        [Header("Items (drag)")]
        [SerializeField] private AudioAsset _itemPickUp;
        [SerializeField] private AudioAsset _itemPutDown;

        [Header("Match / Combo")]
        [SerializeField] private AudioAsset _match;
        [Tooltip("Escalating match sounds by combo level. Index 0 = combo 2, index 1 = combo 3, ...")]
        [SerializeField] private AudioAsset[] _matchCombo;
        [SerializeField] private AudioAsset _comboLost;

        [Header("Stars")]
        [SerializeField] private AudioAsset _starReceived;

        [Header("Abilities")]
        [SerializeField] private AudioAsset _abilityHammer;
        [SerializeField] private AudioAsset _abilityFreeze;
        [SerializeField] private AudioAsset _abilitySwap;
        [SerializeField] private AudioAsset _abilityReplace;

        [Header("Timer / Level")]
        [SerializeField] private AudioAsset _timerWarning;
        [SerializeField] private AudioAsset _levelWon;
        [SerializeField] private AudioAsset _levelLost;

        [Header("UI")]
        [SerializeField] private AudioAsset _uiClick;

        [Header("Music")]
        [SerializeField] private AudioAsset _music;

        public AudioAsset ItemPickUp => _itemPickUp;
        public AudioAsset ItemPutDown => _itemPutDown;
        public AudioAsset ComboLost => _comboLost;
        public AudioAsset StarReceived => _starReceived;
        public AudioAsset TimerWarning => _timerWarning;
        public AudioAsset LevelWon => _levelWon;
        public AudioAsset LevelLost => _levelLost;
        public AudioAsset UiClick => _uiClick;
        public AudioAsset Music => _music;

        public AudioAsset GetMatchSound(int combo)
        {
            if (combo <= 1 || _matchCombo == null || _matchCombo.Length == 0)
            {
                return _match;
            }

            int index = Mathf.Clamp(combo - 2, 0, _matchCombo.Length - 1);
            return _matchCombo[index] != null ? _matchCombo[index] : _match;
        }

        public AudioAsset GetAbilitySound(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Crash:
                    return _abilityHammer;
                case AbilityType.Freeze:
                    return _abilityFreeze;
                case AbilityType.Swap:
                    return _abilitySwap;
                case AbilityType.Replace:
                    return _abilityReplace;
                default:
                    return null;
            }
        }
    }
}
