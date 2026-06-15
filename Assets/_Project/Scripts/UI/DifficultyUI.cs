// DifficultyUI.cs
using UnityEngine;

namespace AIAirHockey
{
    public class DifficultyUI : MonoBehaviour
    {
        [SerializeField] private MainMenuUI _menu;

        public void OnEasy()   => Choose(Difficulty.Easy);
        public void OnMedium() => Choose(Difficulty.Medium);
        public void OnHard()   => Choose(Difficulty.Hard);
        public void OnHuman()  => Choose(Difficulty.HumanLike);

        private void Choose(Difficulty d)
        {
            GameManager.Instance.SetDifficulty(d);
            _menu.StartBotGame();
        }
    }
}