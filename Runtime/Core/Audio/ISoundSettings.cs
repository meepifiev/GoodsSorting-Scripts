using System;

namespace _Project.Core.Audio
{
    public interface ISoundSettings
    {
        bool MusicEnabled { get; }
        bool SfxEnabled { get; }
        event Action Changed;

        void ToggleMusic();
        void ToggleSfx();
    }
}
