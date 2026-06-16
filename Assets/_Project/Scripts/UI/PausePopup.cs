// PausePopup.cs
using UnityEngine;

namespace AIAirHockey
{
    public class PausePopup : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private MatchManager _match;

        public void OnResume()
        {
            _uiManager.HidePause();
            _match.ResumeMatch();
        }
        public void OnRestart() => _match.RestartMatch();
        public void OnQuit() => _match.QuitToMenu();
    }
}