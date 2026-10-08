using System;
using _Project.Core.Audio;
using PrimeGames.SDK;
using VContainer.Unity;

namespace _Project.Infrastructure.Audio
{
    public class SoundSettingsService : ISoundSettings, IInitializable
    {
        private const string MusicKey = "music_enabled";
        private const string SfxKey = "sfx_enabled";

        private readonly IAudioService _audioService;

        private bool _loaded;

        public SoundSettingsService(IAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public event Action Changed;

        public void Initialize()
        {
            PrimeSDK.WaitForProviders(() =>
            {
                EnsureLoaded();
                Apply();
            });
        }

        public void ToggleMusic()
        {
            EnsureLoaded();
            MusicEnabled = !MusicEnabled;
            Persist();
            Apply();
            Changed?.Invoke();
        }

        public void ToggleSfx()
        {
            EnsureLoaded();
            SfxEnabled = !SfxEnabled;
            Persist();
            Apply();
            Changed?.Invoke();
        }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            try
            {
                MusicEnabled = PrimeSDK.Data.GetInt(MusicKey, 1) != 0;
                SfxEnabled = PrimeSDK.Data.GetInt(SfxKey, 1) != 0;
            }
            catch (Exception)
            {
            }
        }

        private void Apply()
        {
            _audioService.SetMusicMuted(MusicEnabled == false);
            _audioService.SetSfxMuted(SfxEnabled == false);
        }

        private void Persist()
        {
            try
            {
                PrimeSDK.Data.SetInt(MusicKey, MusicEnabled ? 1 : 0, important: false);
                PrimeSDK.Data.SetInt(SfxKey, SfxEnabled ? 1 : 0, important: false);
                PrimeSDK.Data.Save();
            }
            catch (Exception)
            {
            }
        }
    }
}
