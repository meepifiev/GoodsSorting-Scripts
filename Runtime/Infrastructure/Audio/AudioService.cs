using System;
using _Project.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace _Project.Infrastructure.Audio
{
    public class AudioService : IAudioService
    {
        private const float DestroyDelayPadding = 0.1f;
        private const float MutedVolumeDb = -80f;
        private const float FullVolumeDb = 0f;
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";

        private readonly GameObject _audioRoot;

        private AudioMixer _mixer;
        private AudioSource _musicSource;
        private AudioAsset _currentMusicAsset;
        private bool _sfxMuted;
        private bool _musicMuted;

        public AudioService()
        {
            _audioRoot = new GameObject(nameof(AudioService));
            Object.DontDestroyOnLoad(_audioRoot);
        }

        public void SetSfxMuted(bool muted)
        {
            _sfxMuted = muted;
            ApplyVolume(SfxVolumeParam, muted);
        }

        public void SetMusicMuted(bool muted)
        {
            _musicMuted = muted;
            ApplyVolume(MusicVolumeParam, muted);
        }

        private void ApplyVolume(string parameter, bool muted)
        {
            if (_mixer != null)
            {
                _mixer.SetFloat(parameter, muted ? MutedVolumeDb : FullVolumeDb);
            }
        }

        public void PlayOneShot(AudioAsset asset)
        {
            if (_audioRoot == null)
                return;

            if (asset.Category != AudioCategories.SFX)
                throw new InvalidOperationException();

            AudioSource source = CreateSource(asset);
            source.Play();

            Object.Destroy(source.gameObject, GetDestroyDelay(source));
        }

        public void PlayMusic(AudioAsset asset)
        {
            if (_audioRoot == null)
                return;

            if (asset.Category != AudioCategories.Music)
                throw new InvalidOperationException();

            if (asset == _currentMusicAsset && _musicSource != null && _musicSource.isPlaying)
                return;

            StopMusic();

            _musicSource = CreateSource(asset);
            _musicSource.loop = true;

            _currentMusicAsset = asset;

            _musicSource.Play();
        }

        private void StopMusic()
        {
            if (_musicSource == null)
                return;

            _musicSource.Stop();

            Object.Destroy(_musicSource.gameObject);

            _musicSource = null;
            _currentMusicAsset = null;
        }

        private AudioSource CreateSource(AudioAsset asset)
        {
            AudioClip clip = asset.GetRandomClip();

            GameObject audioObject = new GameObject(clip.name);
            audioObject.transform.SetParent(_audioRoot.transform);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.outputAudioMixerGroup = asset.OutputMixerGroup;
            source.volume = asset.Volume;
            source.pitch = asset.Pitch;
            source.playOnAwake = false;

            CaptureMixer(asset.OutputMixerGroup);

            return source;
        }

        private void CaptureMixer(UnityEngine.Audio.AudioMixerGroup group)
        {
            if (_mixer != null || group == null)
            {
                return;
            }

            _mixer = group.audioMixer;
            ApplyVolume(SfxVolumeParam, _sfxMuted);
            ApplyVolume(MusicVolumeParam, _musicMuted);
        }

        private float GetDestroyDelay(AudioSource source)
        {
            return source.clip.length / source.pitch + DestroyDelayPadding;
        }
    }
}
