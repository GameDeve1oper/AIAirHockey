// DifficultyProfile.cs
using UnityEngine;

namespace AIAirHockey
{
    // Per-difficulty tuning for the bot. One asset per tier lives in
    // Resources/Difficulty named Difficulty_<Name> (Easy/Medium/Hard/HumanLike).
    // Every field below is actually consumed by BotBrain/BotPaddle.
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "AIAirHockey/DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Label only. The bot loads profiles by file name (Difficulty_<Name>).")]
        public Difficulty difficulty = Difficulty.Medium;

        [Header("Movement")]
        [Tooltip("Max speed the bot paddle can move (world units / sec).")]
        public float moveSpeed = 12f;
        [Tooltip("SmoothDamp follow time (sec). Lower = snappier/harder, " +
                 "higher = floatier/easier.")]
        public float followSmoothTime = 0.12f;

        [Header("Reaction")]
        [Tooltip("Perception lag (sec). The bot reacts to a smoothed view of the " +
                 "puck. Higher = laggier/easier, lower = sharper/harder.")]
        public float reactionTime = 0.15f;

        [Header("Prediction")]
        [Tooltip("Anticipation horizon (sec). How far ahead the bot leads the puck " +
                 "and foresees bank shots. 0 = react to current position only.")]
        public float predictionTime = 0.4f;

        [Header("Tactics")]
        [Range(0f, 1f)]
        [Tooltip("0 = hangs back and defends; 1 = pushes forward to attack and " +
                 "strikes harder/wider angles.")]
        public float aggression = 0.5f;
        [Tooltip("World-Y the bot guards around (its goal is above this line).")]
        public float defenseLineY = 3.4f;

        [Header("Imperfection (human feel)")]
        [Tooltip("Aim error in world units. Sampled-and-held, then eased (not " +
                 "per-frame jitter). 0 = perfect aim.")]
        public float aimError = 0.3f;
        [Range(0f, 1f)]
        [Tooltip("Chance per re-aim to exaggerate the error (a human 'misjudge').")]
        public float mistakeChance = 0.1f;

        [Header("Vertical Reach (world-Y target band)")]
        [Tooltip("Closest to center (smallest Y) the bot will advance to attack.")]
        public float minY = 0.3f;
        [Tooltip("Deepest toward its own goal the target may go. ClampToHalf still " +
                 "applies the hard half-board limit on top of this.")]
        public float maxY = 7.0f;
    }
}
