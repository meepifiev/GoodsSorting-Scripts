using System;
using _Project.Core.Level;
using _Project.Core.Progress;
using _Project.Core.StateMachine;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay.Debugging
{
    public class GameplayDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        [SerializeField] private float _width = 200f;

        private ILevelService _levelService;
        private IProgressStorage _progress;
        private IGameLauncher _launcher;
        private ILevelCatalog _levels;

        [Inject]
        public void Construct(ILevelService levelService, IProgressStorage progress, IGameLauncher launcher, ILevelCatalog levels)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }

        private void OnGUI()
        {
            if (_visible == false || _levelService == null || _progress == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, _width, 320f), GUI.skin.box);
            GUILayout.Label("DEBUG   уровень: " + _progress.Level + (_levels.IsPlayable(_progress.Level) ? string.Empty : "  (удалён)"));

            if (GUILayout.Button("+1 уровень"))
            {
                ChangeLevel(1);
            }

            if (GUILayout.Button("-1 уровень"))
            {
                ChangeLevel(-1);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Выиграть"))
            {
                _levelService.Win();
            }

            if (GUILayout.Button("Проиграть (время)"))
            {
                _levelService.Lose(LevelLostReason.TimeUp);
            }

            if (GUILayout.Button("Нет места"))
            {
                _levelService.Lose(LevelLostReason.OutOfSpace);
            }

            GUILayout.EndArea();
        }

        private void ChangeLevel(int delta)
        {
            int direction = delta >= 0 ? 1 : -1;
            int next = _levels.FindPlayable(_progress.Level + direction, direction);

            if (next == 0 || next == _progress.Level)
            {
                return;
            }

            _progress.Level = next;
            _progress.Save();
            _launcher.StartGame(true);
        }
    }
}
