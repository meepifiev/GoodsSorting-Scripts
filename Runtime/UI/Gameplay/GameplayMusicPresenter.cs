using System;
using _Project.Core.Audio;
using VContainer.Unity;

namespace _Project.UI.Gameplay
{
    public class GameplayMusicPresenter : IInitializable
    {
        private readonly IAudioService _audioService;
        private readonly GameplayAudioConfig _config;

        public GameplayMusicPresenter(IAudioService audioService, GameplayAudioConfig config)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Initialize()
        {
            if (_config.Music != null)
            {
                _audioService.PlayMusic(_config.Music);
            }
        }
    }
}
