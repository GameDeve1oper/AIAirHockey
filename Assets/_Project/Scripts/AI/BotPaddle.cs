// BotPaddle.cs
using UnityEngine;

namespace AIAirHockey
{
    // Actuation layer for the bot. Asks BotBrain for a target every physics
    // step and follows it with a critically-damped SmoothDamp (fluid, no
    // overshoot, cheap on mobile). Follow smoothing is fixed and identical
    // at every difficulty -- only moveSpeed (from DifficultyProfile)
    // changes how the bot feels between tiers.
    //
    // Only moves while MatchState.Playing. During Countdown/GoalScored/
    // Paused/etc. it sits completely still -- and forces BotBrain back to
    // Guard each time -- so it never carries a stale Follow/Recover/Corner
    // target into the next round's frozen, teleported puck.
    public class BotPaddle : Paddle
    {
        [SerializeField] private Puck _puck; // assigned in Inspector

        // Shared by every difficulty tier on purpose -- see DifficultyProfile.
        private const float FollowSmoothTime = 0.06f;

        private BotBrain _brain;
        private DifficultyProfile _profile;
        private Vector2 _smoothVelocity;
        private bool _isPlaying;

        // Exposed for debugging (e.g. an on-screen label or gizmo).
        public AIState CurrentState => _brain != null ? _brain.CurrentState : AIState.Guard;

        private void OnEnable()
        {
            EventBus.OnMatchStateChanged += HandleMatchStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnMatchStateChanged -= HandleMatchStateChanged;
        }

        // Called by MatchManager.SetupForMode with the chosen difficulty.
        public void Configure(Difficulty difficulty)
        {
            if (_config == null)
            {
                Debug.LogError("BotPaddle: GameConfig (_config) is not assigned; bot disabled.");
                return;
            }

            _profile = LoadProfile(difficulty);
            _brain = new BotBrain(_config);
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
                // still runs. (Field initializers supply a sane default.)
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
                // Round isn't live (countdown ticking, goal just scored,
                // paused, etc.) -- nobody needs to move. Clear any stale
                // state/velocity now so the moment Playing resumes we
                // start clean instead of snapping in from wherever the
                // last goal left us.
                _brain?.ResetToGuard();
                _smoothVelocity = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (!_isPlaying) return; // frozen during countdown / goal pause / paused / etc.
            if (_brain == null || _profile == null || _puck == null) return;

            Vector2 target = _brain.Decide(_rb.position, _puck.Position);

            // Critically-damped follow: smooth and fluid. Smooth time is
            // fixed; only max speed comes from the difficulty profile.
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
    }
}