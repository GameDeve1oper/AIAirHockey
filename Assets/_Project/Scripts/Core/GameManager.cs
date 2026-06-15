// GameManager.cs
using UnityEngine;

namespace AIAirHockey
{
    // Holds the player's choices made in the menu so the Gameplay scene
    // knows what to build. Persists across scenes.
    public class GameManager : Singleton<GameManager>
    {
        public GameMode SelectedMode { get; private set; } = GameMode.PlayerVsBot;
        public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Medium;

        public void SetMode(GameMode mode) => SelectedMode = mode;

        public void SetDifficulty(Difficulty difficulty)
        {
            SelectedDifficulty = difficulty;
            // Remember the choice for next time.
            SaveManager.Instance.Data.lastDifficulty = (int)difficulty;
            SaveManager.Instance.Save();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            // Restore last used difficulty from save.
            SelectedDifficulty = (Difficulty)SaveManager.Instance.Data.lastDifficulty;
        }
    }
}