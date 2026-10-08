namespace _Project.Core.Audio
{
    public static class AudioServiceExtensions
    {
        public static void PlayOneShotSafe(this IAudioService audioService, AudioAsset asset)
        {
            if (audioService == null || asset == null)
            {
                return;
            }

            audioService.PlayOneShot(asset);
        }
    }
}
