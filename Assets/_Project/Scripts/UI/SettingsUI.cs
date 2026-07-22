// SettingsUI.cs
using UnityEngine;
using UnityEngine.UI;

namespace AIAirHockey
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        private void OnEnable()
        {
            float musicVol = 0.7f;
            float sfxVol = 1.0f;

            if (SaveManager.Exists && SaveManager.Instance.Data != null)
            {
                var data = SaveManager.Instance.Data;
                musicVol = data.musicVolume;
                sfxVol = data.sfxVolume;
            }

            if (_musicSlider != null)
            {
                _musicSlider.SetValueWithoutNotify(musicVol);
                _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
                _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.SetValueWithoutNotify(sfxVol);
                _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
                _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }
        }

        private void OnDisable()
        {
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }

        public void OnMusicChanged(float v)
        {
            if (AudioManager.Exists)
                AudioManager.Instance.SetMusicVolume(v);
        }

        public void OnSfxChanged(float v)
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.SetSfxVolume(v);
                AudioManager.Instance.Play(SoundId.ButtonClick);
            }
        }

        public void SaveSettings()
        {
            if (SaveManager.Exists) SaveManager.Instance.Save();
        }
    }
}