// UIManager.cs
using UnityEngine;

namespace AIAirHockey
{
    // Lives in the Gameplay scene (NOT a persistent singleton, because
    // each Gameplay load gets fresh UI). Coordinates the in-game popups.
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePopup;
        [SerializeField] private GameObject _resultPopup;

        private void Awake()
        {
            _pausePopup.SetActive(false);
            _resultPopup.SetActive(false);
        }

        public void ShowPause() => _pausePopup.SetActive(true);
        public void HidePause() => _pausePopup.SetActive(false);
        public void ShowResult() => _resultPopup.SetActive(true);
    }
}