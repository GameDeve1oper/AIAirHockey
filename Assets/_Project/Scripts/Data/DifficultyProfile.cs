// DifficultyProfile.cs
using UnityEngine;

namespace AIAirHockey
{
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "AIAirHockey/DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        public Difficulty difficulty = Difficulty.Medium;

        [Header("Movement")]
        [Tooltip("Max speed the bot paddle can move.")]
        public float moveSpeed = 12f;
        [Tooltip("Higher = snappier following; lower = sluggish.")]
        public float acceleration = 25f;

        [Header("Reaction")]
        [Tooltip("Seconds of delay before the bot reacts to puck changes.")]
        public float reactionTime = 0.15f;

        [Header("Prediction")]
        [Tooltip("How far ahead (seconds) the bot predicts the puck. 0 = no prediction.")]
        public float predictionTime = 0.4f;
        [Tooltip("0 = perfect aim, higher = more random aiming error (world units).")]
        public float aimError = 0.3f;

        [Header("Behavior")]
        [Tooltip("Chance (0..1) per decision to make a deliberate mistake.")]
        [Range(0f,1f)] public float mistakeChance = 0.1f;
        [Tooltip("How aggressively the bot pushes forward to attack (0..1).")]
        [Range(0f,1f)] public float aggression = 0.5f;
        [Tooltip("Y position (world) the bot defends around when idle.")]
        public float defenseLineY = 3.4f;
    }
}