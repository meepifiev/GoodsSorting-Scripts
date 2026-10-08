using System;
using _Project.Core.Progress;
using VContainer.Unity;

namespace _Project.UI.Gameplay
{
    public class LevelCounterPresenter : IInitializable
    {
        private readonly IProgressStorage _progressStorage;
        private readonly LevelCounterView _view;

        public LevelCounterPresenter(IProgressStorage progressStorage, LevelCounterView view)
        {
            _progressStorage = progressStorage ?? throw new ArgumentNullException(nameof(progressStorage));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            _view.SetLevel(_progressStorage.Level);
        }
    }
}
