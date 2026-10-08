namespace _Project.Core.Audio
{
    public interface IAudioService
    {
        void PlayOneShot(AudioAsset asset);
        void PlayMusic(AudioAsset asset);
        void SetSfxMuted(bool muted);
        void SetMusicMuted(bool muted);
    }
}
