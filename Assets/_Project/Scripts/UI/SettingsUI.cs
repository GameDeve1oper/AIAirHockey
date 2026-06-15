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
            var data = SaveManager.Instance.Data;
            _musicSlider.SetValueWithoutNotify(data.musicVolume);
            _sfxSlider.SetValueWithoutNotify(data.sfxVolume);
        }

        public void OnMusicChanged(float v) => AudioManager.Instance.SetMusicVolume(v);
        public void OnSfxChanged(float v)
        {
            AudioManager.Instance.SetSfxVolume(v);
            AudioManager.Instance.Play(SoundId.ButtonClick); // audible preview
        }
        public void SaveSettings() => SaveManager.Instance.Save();
    }
}