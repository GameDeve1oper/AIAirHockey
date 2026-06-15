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
            if (_brain == null) { Debug.LogWarning("BotPaddle: _brain is NULL (Configure not called or profile missing)"); return; }
            if (_puck == null) { Debug.LogWarning("BotPaddle: _puck is NULL (assign Puck in Inspector)"); return; }

            Vector2 target = _brain.Decide(
                _rb.position, _puck.Position, _puck.Velocity, Time.fixedDeltaTime);
            Vector2 desired = (target - _rb.position);
            float maxStep = _profile.moveSpeed * Time.fixedDeltaTime;
            Vector2 step = Vector2.ClampMagnitude(desired, maxStep);
            Vector2 next = ClampToHalf(_rb.position + step);
            _rb.MovePosition(next);
        }

    }
}