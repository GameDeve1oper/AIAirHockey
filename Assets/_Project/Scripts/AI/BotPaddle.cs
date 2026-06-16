// BotPaddle.cs
using UnityEngine;

namespace AIAirHockey
{
    public class BotPaddle : Paddle
    {
        [SerializeField] private Puck _puck; // assigned in Inspector

        private BotBrain _brain;
        private DifficultyProfile _profile;
        private Vector2 _velocityTarget;

        // Called by MatchManager.SetupForMode with the chosen difficulty.
        public void Configure(Difficulty difficulty)
        {
            _profile = LoadProfile(difficulty);
            _brain = new BotBrain(_profile, _config);
            _side = PlayerSide.Top;
        }

        private DifficultyProfile LoadProfile(Difficulty d)
        {
            // Profiles live in Resources/Difficulty named Difficulty_<Name>.
            string name = "Difficulty/Difficulty_" + d.ToString();
            DifficultyProfile p = Resources.Load<DifficultyProfile>(name);
            if (p == null)
                Debug.LogError("Missing DifficultyProfile resource: " + name);
            return p;
        }

        private void FixedUpdate()
        {
            if (_brain == null || _puck == null) return;

            Vector2 target = _brain.Decide(
                _rb.position, _puck.Position, _puck.Velocity, Time.fixedDeltaTime);

            // Smooth move toward target (both X and Y now).
            float maxStep = _profile.moveSpeed * Time.fixedDeltaTime;
            Vector2 next = Vector2.MoveTowards(_rb.position, target, maxStep);
            next = ClampToHalf(next);
            _rb.MovePosition(next);
        }
    }
}