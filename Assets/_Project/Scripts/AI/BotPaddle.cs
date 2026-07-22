// BotPaddle.cs
using UnityEngine;

namespace AIAirHockey
{
    // Actuation layer for the bot. Asks BotBrain for a target every physics
    // step and follows it with a critically-damped SmoothDamp (fluid, no
    // overshoot, cheap on mobile). Follow smoothing is fixed and identical
    // at every difficulty -- moveSpeed and perception parameters (from
    // DifficultyProfile) change how the bot feels between tiers.
    public class BotPaddle : Paddle
    {
        [SerializeField] private Puck _puck; // assigned in Inspector

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmo = false;

        private const float FollowSmoothTime = 0.06f;

        private BotBrain _brain;
        private DifficultyProfile _profile;
        private Vector2 _smoothVelocity;
        private bool _isPlaying;

        public AIState CurrentState => _brain != null ? _brain.CurrentState : AIState.Guard;

        public void NotifyHit() => _brain?.NotifyHit();

        private void OnEnable()
        {
            EventBus.OnMatchStateChanged += HandleMatchStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnMatchStateChanged -= HandleMatchStateChanged;
        }

        public void Configure(Difficulty difficulty)
        {
            if (_config == null)
            {
                Debug.LogError("BotPaddle: GameConfig (_config) is not assigned; bot disabled.");
                return;
            }

            _profile = LoadProfile(difficulty);
            _brain = new BotBrain(_config, _profile);
            _side = PlayerSide.Top;
        }

        private DifficultyProfile LoadProfile(Difficulty d)
        {
            string path = "Difficulty/Difficulty_" + d.ToString();
            DifficultyProfile p = Resources.Load<DifficultyProfile>(path);

            if (p == null)
            {
                Debug.LogWarning("BotPaddle: missing '" + path + "', using built-in defaults.");
                p = ScriptableObject.CreateInstance<DifficultyProfile>();
                p.difficulty = d;
            }
            return p;
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            _isPlaying = state == MatchState.Playing;

            if (!_isPlaying)
            {
                _brain?.ResetToGuard();
                _smoothVelocity = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (!_isPlaying) return;
            if (_brain == null || _profile == null || _puck == null) return;

            Vector2 target = _brain.Decide(_rb.position, _puck.Position, _puck.Velocity);

            Vector2 next = Vector2.SmoothDamp(
                _rb.position,
                target,
                ref _smoothVelocity,
                FollowSmoothTime,
                _profile.moveSpeed,
                Time.fixedDeltaTime);

            next = ClampToHalf(next);
            _rb.MovePosition(next);
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmo || !Application.isPlaying) return;
            if (_brain == null || _puck == null || _rb == null) return;

            // 1. Raw prediction path & endpoint (Cyan)
            Gizmos.color = Color.cyan;
            Vector2 rawPred = _brain.DebugRawPredictionPoint;
            if (rawPred.sqrMagnitude > 0.01f)
            {
                Gizmos.DrawLine(_puck.Position, rawPred);
                Gizmos.DrawWireSphere(rawPred, 0.15f);
            }

            // 2. Filtered Target Coordinate (Yellow)
            Gizmos.color = Color.yellow;
            Vector2 currentTarget = _brain.CurrentTarget;
            Gizmos.DrawWireSphere(currentTarget, 0.25f);
            Gizmos.DrawLine(_rb.position, currentTarget);

            // 3. Actual Paddle Position (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_rb.position, 0.35f);
        }
    }
}
