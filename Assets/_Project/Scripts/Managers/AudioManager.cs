// AudioManager.cs
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AIAirHockey
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Serializable]
        public class SoundEntry
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f,1f)] public float volume = 1f;
        }

        [Header("Mixer")]
        [SerializeField] private AudioMixer _mixer;            // assigned in Inspector
        [SerializeField] private string _musicParam = "MusicVolume";
        [SerializeField] private string _sfxParam = "SFXVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource _musicSource;     // looping music
        [SerializeField] private AudioSource _sfxSource;       // one-shot SFX

        [Header("Clips")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _gameMusic;
        [SerializeField] private SoundEntry[] _sounds;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            ApplySavedVolumes();
        }

        // Reads saved volumes and pushes them to the mixer.
        public void ApplySavedVolumes()
        {
            var data = SaveManager.Instance.Data;
            SetMusicVolume(data.musicVolume);
            SetSfxVolume(data.sfxVolume);
        }

        // Volume 0..1 converted to decibels for the mixer.
        public void SetMusicVolume(float v01)
        {
            SaveManager.Instance.Data.musicVolume = v01;
            _mixer.SetFloat(_musicParam, LinearToDb(v01));
        }

        public void SetSfxVolume(float v01)
        {
            SaveManager.Instance.Data.sfxVolume = v01;
            _mixer.SetFloat(_sfxParam, LinearToDb(v01));
        }

        private float LinearToDb(float v)
        {
            // Avoid log(0). At 0 volume, push fully silent (-80 dB).
            return v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
        }

        public void PlayMenuMusic() => PlayMusic(_menuMusic);
        public void PlayGameMusic() => PlayMusic(_gameMusic);

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            _musicSource.clip = clip;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        // Plays a sound effect by id.
        public void Play(SoundId id)
        {
            foreach (var s in _sounds)
            {
                if (s.id == id && s.clip != null)
                {
                    _sfxSource.PlayOneShot(s.clip, s.volume);
                    return;
                }
            }
        }
    }
}