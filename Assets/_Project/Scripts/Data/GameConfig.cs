// GameConfig.cs
using UnityEngine;

namespace AIAirHockey
{
    // Central tuning asset. Create one asset instance and reference it.
    [CreateAssetMenu(fileName = "GameConfig", menuName = "AIAirHockey/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Rules")]
        public int goalsToWin = 7;          // first to this many goals wins
        public float goalResetDelay = 1.2f; // pause after a goal before reset

        [Header("Puck")]
        public float puckMaxSpeed = 14f;    // clamp so it never goes uncatchable
        public float puckMinSpeedAfterHit = 4f; // keep it lively
        public float puckStartSpeed = 6f;   // launch speed each round
        public float puckMass = 0.3f;
        public float puckDrag = 0.2f;       // slight slowdown over time

        [Header("Player Paddle")]
        public float playerPaddleMaxSpeed = 40f; // how fast it can follow finger

        [Header("Board (world half-extents)")]
        public float boardHalfWidth = 2.6f;   // from center to side wall
        public float boardHalfHeight = 4.8f;  // from center to top/bottom wall
    }
}