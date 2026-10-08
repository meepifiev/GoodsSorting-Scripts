using System;
using UnityEngine;

namespace _Project.Features.Legacy.Levels
{
    [CreateAssetMenu(fileName = "LevelContainer", menuName = "_Project/Levels/Level Container", order = 0)]
    public class LevelContainer : ScriptableObject
    {
        [SerializeField] private LegacyLevelConfig[] _legacyLevelConfigs = Array.Empty<LegacyLevelConfig>();
        [Tooltip("Last level with its own content. Levels after it replay the loop range. 0 disables looping.")]
        [SerializeField] private int _lastUniqueLevel = 50;
        [Tooltip("First level of the replayed range.")]
        [SerializeField] private int _loopFirstLevel = 10;

        public int Count => _legacyLevelConfigs.Length;
        public int LastUniqueLevel => _lastUniqueLevel;
        public int LoopFirstLevel => _loopFirstLevel;

        public bool IsLooping => _lastUniqueLevel > 0
            && _loopFirstLevel >= 1
            && _loopFirstLevel <= _lastUniqueLevel
            && _lastUniqueLevel <= _legacyLevelConfigs.Length;

        public int ResolveContentLevel(int levelNumber)
        {
            if (IsLooping == false || levelNumber <= _lastUniqueLevel)
            {
                return levelNumber;
            }

            int span = _lastUniqueLevel - _loopFirstLevel + 1;
            return _loopFirstLevel + (levelNumber - _lastUniqueLevel - 1) % span;
        }

        public LegacyLevelConfig GetLevelConfig(int levelNumber)
        {
            int contentLevel = ResolveContentLevel(levelNumber);

            if (contentLevel < 1 || contentLevel > _legacyLevelConfigs.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            }

            LegacyLevelConfig legacyLevelConfig = _legacyLevelConfigs[contentLevel - 1];

            if (legacyLevelConfig == null)
            {
                throw new InvalidOperationException(nameof(_legacyLevelConfigs));
            }

            return legacyLevelConfig;
        }

        public bool IsPlayable(int levelNumber)
        {
            if (levelNumber < 1)
            {
                return false;
            }

            int contentLevel = ResolveContentLevel(levelNumber);

            if (contentLevel > _legacyLevelConfigs.Length)
            {
                return false;
            }

            LegacyLevelConfig config = _legacyLevelConfigs[contentLevel - 1];
            return config != null && config.IsDisabled == false;
        }

        public int FindPlayable(int levelNumber, int direction)
        {
            if (direction == 0)
            {
                return IsPlayable(levelNumber) ? levelNumber : 0;
            }

            int step = direction > 0 ? 1 : -1;
            int limit = IsLooping ? levelNumber + _legacyLevelConfigs.Length : _legacyLevelConfigs.Length;

            for (int current = levelNumber; current >= 1 && current <= limit; current += step)
            {
                if (IsPlayable(current))
                {
                    return current;
                }
            }

            return 0;
        }
    }
}
