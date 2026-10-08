#if UNITY_EDITOR
using System;
using _Project.Core.Level;
using _Project.Features.Legacy.Levels;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Features.Level
{
    public class DevLevelSwitcher : ITickable
    {
        private const KeyCode ToggleKey = KeyCode.D;
        private const string ContainerPath = "Assets/_Project/Configs/Level/LevelContainer.asset";

        private readonly ILevelService _levelService;
        private readonly LevelContainer _levels;

        public DevLevelSwitcher(ILevelService levelService)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));

            _levels = AssetDatabase.LoadAssetAtPath<LevelContainer>(ContainerPath);
        }

        public void Tick()
        {
            if (_levels == null)
            {
                return;
            }

            if (Input.GetKeyDown(ToggleKey))
            {
                ToggleCurrentLevel();
            }
        }

        public void ToggleCurrentLevel()
        {
            int levelNumber = _levelService.CurrentLevel;
            LegacyLevelConfig config = _levels.GetLevelConfig(levelNumber);
            bool disabled = config.IsDisabled == false;

            config.SetDisabled(disabled);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("[Development] level " + levelNumber + (disabled ? " disabled" : " enabled") + " (" + config.name +
                ") — run Tools/Addressables/Levels to rebuild the manifest so it takes effect.");
        }
    }
}
#endif
