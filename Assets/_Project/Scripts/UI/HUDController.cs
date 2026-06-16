// HUDController.cs
using TMPro;
using UnityEngine;

namespace AIAirHockey
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _topScore;
        [SerializeField] private TMP_Text _bottomScore;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private MatchManager _match;

        private void OnEnable()
        {
            EventBus.OnScoreChanged += UpdateScore;
        }
        private void OnDisable()
        {
            EventBus.OnScoreChanged -= UpdateScore;
        }

        private void Start()
        {
            UpdateScore(0, 0);
        }

        private void UpdateScore(int bottom, int top)
        {
            _bottomScore.text = bottom.ToString();
            _topScore.text = top.ToString();
        }

        // Hooked to the pause button.
        public void OnPausePressed()
        {
            _match.PauseMatch();
            _uiManager.ShowPause();
        }
    }
}