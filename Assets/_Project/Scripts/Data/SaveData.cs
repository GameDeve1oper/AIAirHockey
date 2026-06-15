// SaveData.cs
using System;

namespace AIAirHockey
{
    // The full saved game state, serialized to JSON on disk.
    // [Serializable] lets JsonUtility convert it to/from text.
    [Serializable]
    public class SaveData
    {
        // Audio settings (0..1).
        public float musicVolume = 0.7f;
        public float sfxVolume = 1.0f;

        // Last difficulty the player chose (stored as int = enum index).
        public int lastDifficulty = (int)Difficulty.Medium;

        // Statistics.
        public int matchesPlayed = 0;
        public int matchesWon = 0;
        public int goalsScored = 0;
        public int goalsConceded = 0;
    }
}