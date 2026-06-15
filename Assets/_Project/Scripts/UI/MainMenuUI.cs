// MainMenuUI.cs
using UnityEngine;

namespace AIAirHockey
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject _mainButtons;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _difficultyPanel;

        private void Start()
        {
            ShowMain();
            if (AudioManager.Exists) AudioManager.Instance.PlayMenuMusic();
        }

        // Player picked 'Play vs Bot' -> show difficulty selection first.
        public void OnPlayBot()
        {
            GameManager.Instance.SetMode(GameMode.PlayerVsBot);
            ShowDifficulty();
        }

        // Player picked '2 Players' -> go straight to gameplay.
        public void OnPlayPvP()
        {
            GameManager.Instance.SetMode(GameMode.PlayerVsPlayer);
            SceneLoader.Instance.Load(SceneLoader.Gameplay);
        }

        public void OnSettings() => ShowSettings();
        public void OnBackToMain() => ShowMain();

        // Called by DifficultyUI after a difficulty is chosen.
        public void StartBotGame() => SceneLoader.Instance.Load(SceneLoader.Gameplay);

        private void ShowMain()
        {
            _mainButtons.SetActive(true);
            _settingsPanel.SetActive(false);
            _difficultyPanel.SetActive(false);
        }
        private void ShowSettings()
        {
            _mainButtons.SetActive(false);
            _settingsPanel.SetActive(true);
            _difficultyPanel.SetActive(false);
        }
        private void ShowDifficulty()
        {
            _mainButtons.SetActive(false);
            _settingsPanel.SetActive(false);
            _difficultyPanel.SetActive(true);
        }
    }
}