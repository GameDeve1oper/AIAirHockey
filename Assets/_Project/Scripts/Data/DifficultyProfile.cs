// DifficultyProfile.cs
using UnityEngine;

namespace AIAirHockey
{
    // Per-difficulty tuning for the bot.
    //
    // Difficulty is deliberately driven by a SINGLE knob: moveSpeed. The
    // bot's behaviour (when it follows, guards, recovers, or escapes a
    // corner) is identical at every tier -- only how fast it can move
    // changes. That keeps every difficulty feeling like the same opponent,
    // just turned up or down, instead of bolting on cheap tricks like
    // slower reactions or deliberately bad aim.
    //
    // Profiles live in Resources/Difficulty, named Difficulty_<Name>
    // (e.g. Difficulty_Easy, Difficulty_Medium, Difficulty_Hard,
    // Difficulty_HumanLike), and are loaded by BotPaddle.Configure.
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "AIAirHockey/DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Label only. The bot loads profiles by file name (Difficulty_<Name>).")]
        public Difficulty difficulty = Difficulty.Medium;

        [Header("Movement")]
        [Tooltip("Max speed the bot paddle can move, in world units/sec. " +
                 "This is the ONLY thing that should differ between tiers " +
                 "-- e.g. Easy ~6, Medium ~10, Hard ~16, HumanLike ~12.")]
        public float moveSpeed = 12f;
    }
}