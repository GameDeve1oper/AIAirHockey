// BotPaddle.cs
using UnityEngine;

namespace AIAirHockey
{
    // Actuation layer for the bot. Asks BotBrain for a target each physics
    // step and follows it with a critically-damped SmoothDamp (fluid, no
    // overshoot, cheap on mobile). Difficulty drives both the follow
    // smoothness and the max speed, so higher tiers feel snappier.
    public class BotPaddle : Paddle
    {
        [SerializeField] private Puck _puck; // assigned in Inspector (reference preserved)

        private BotBrain _brain;
        private DifficultyProfile _profile;
        private Vector2 _smoothVelocity;

        // Called by MatchManager.SetupForMode with the chosen difficulty.
        public void Configure(Difficulty difficulty)
        {
            if (_config == null)
            {
                Debug.LogError("BotPaddle: GameConfig (_config) is not assigned; bot disabled.");
                return;
            }

            _profile = LoadProfile(difficulty);
            _brain = new BotBrain(_profile, _config);
            _side = PlayerSide.Top;
        }

        private DifficultyProfile LoadProfile(Difficulty d)
        {
            // Profiles live in Resources/Difficulty named Difficulty_<Name>.
            string path = "Difficulty/Difficulty_" + d.ToString();
            DifficultyProfile p = Resources.Load<DifficultyProfile>(path);

            if (p == null)
            {
                // Graceful fallback instead of crashing when the asset is
                // missing: build a default profile in memory so the match
                // still runs. (Field initializers supply sane defaults.)
                Debug.LogWarning("BotPaddle: missing '" + path + "', using built-in defaults.");
                p = ScriptableObject.CreateInstance<DifficultyProfile>();
                p.difficulty = d;
            }
            return p;
        }

        private void FixedUpdate()
        {
            if (_brain == null || _profile == null || _puck == null) return;

            Vector2 target = _brain.Decide(
                _rb.position, _puck.Position, _puck.Velocity, Time.fixedDeltaTime);

            // Critically-damped follow: smooth and fluid. smoothTime and max
            // speed both come from the difficulty profile.
            Vector2 next = Vector2.SmoothDamp(
                _rb.position,
                target,
                ref _smoothVelocity,
                Mathf.Max(_profile.followSmoothTime, 0.0001f),
                _profile.moveSpeed,
                Time.fixedDeltaTime);

            next = ClampToHalf(next);
            _rb.MovePosition(next);
        }
    }
}
