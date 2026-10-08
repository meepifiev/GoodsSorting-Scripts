using System;
using _Project.Core.Audio;
using VContainer.Unity;

namespace _Project.UI.Menu
{
    public class MenuMusicPresenter : IInitializable
    {
        private readonly IAudioService _audioService;
        private readonly AudioAsset _music;

        public MenuMusicPresenter(IAudioService audioService, AudioAsset music)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _music = music;
        }

        public void Initialize()
        {
            if (_music != null)
            {
                _audioService.PlayMusic(_music);
            }
        }
    }
}
