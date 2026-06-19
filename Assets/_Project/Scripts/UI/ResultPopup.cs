// ResultPopup.cs
using TMPro;
using UnityEngine;

namespace AIAirHockey
{
    public class ResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private MatchManager _match;

        [Header("Colors")]
        [SerializeField] private Color _victoryColor = new Color(0.28f, 0.78f, 0.56f);
        [SerializeField] private Color _defeatColor = new Color(0.92f, 0.34f, 0.34f);

        private void OnEnable() { EventBus.OnMatchFinished += Show; }
        private void OnDisable() { EventBus.OnMatchFinished -= Show; }
        private void Awake() { _root.SetActive(false); }

        private void Show(PlayerSide winner)
        {
             _root.SetActive(true);
            bool playerWon = winner == PlayerSide.Bottom;
            _title.text = playerWon ? "VICTORY" : "DEFEAT";
            _title.color = playerWon ? _victoryColor : _defeatColor;
           
        }

        public void OnPlayAgain() => _match.RestartMatch();
        public void OnMenu() => _match.QuitToMenu();
    }
}