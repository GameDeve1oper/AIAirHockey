// Puck.cs
using UnityEngine;

namespace AIAirHockey
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Puck : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        private Rigidbody2D _rb;
        private CircleCollider2D _collider;
        private Renderer _renderer; // works for SpriteRenderer/MeshRenderer alike
        public Rigidbody2D Body => _rb;
        public Vector2 Position => _rb.position;
        public Vector2 Velocity => _rb.linearVelocity;
        
        public PlayerSide LastHitter { get; private set; } = PlayerSide.Bottom;
        private float _maxSpeedMultiplier = 1.0f;

        public void SetMaxSpeedMultiplier(float mult)
        {
            _maxSpeedMultiplier = Mathf.Max(0.1f, mult);
        }

        private void Awake()
        {
            EnsureInit();
        }

         // Safe one-time init. Works no matter the script execution order.
        private void EnsureInit()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<CircleCollider2D>();
            if (_renderer == null) _renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        }

        // Show/hide the puck visually without disabling the GameObject
        // (so its scripts/physics state stay intact for the next reset).
        // Used so the puck disappears the instant a goal is scored,
        // instead of visibly bouncing off the back of the net for the
        // rest of the goalResetDelay.
        public void SetVisible(bool visible)
        {
            EnsureInit();
            if (_renderer != null) _renderer.enabled = visible;
        }

        private void Start()
        {
            EnsureInit();
            if (_config != null)
            {
                _rb.mass = _config.puckMass;
                _rb.linearDamping = _config.puckDrag;
            }

            // IMPORTANT: Fast paddles (player paddle can hit 40 u/s) can
            // tunnel a normal discrete-collision puck through a wall in a
            // single physics step, especially in corners where two walls
            // must be resolved at once. Continuous Collision Detection
            // makes Unity sweep the puck's path each step instead of just
            // checking its start/end position, which is what actually
            // stops corner-ejection at the source.
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Called by MatchManager to place and launch the puck each round.
        public void ResetPuck(Vector2 position, Vector2 launchDirection)
        {
            EnsureInit(); // guard against being called before Awake
            SetVisible(true); // undo the hide-on-goal from HandleGoalScored
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.position = position;
            transform.position = position;
            if (launchDirection.sqrMagnitude > 0.0001f && _config != null)
                _rb.linearVelocity = launchDirection.normalized * _config.puckStartSpeed;
        }

        // Freeze the puck (used during countdown / goal pause).
        public void Freeze()
        {
            EnsureInit();
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }

        public void Unfreeze()
        {
            EnsureInit();
            _rb.simulated = true;
        }

        // Keep speed in a playable band every physics step.
        private void FixedUpdate()
        {
            if (!_rb.simulated) return;
            float speed = _rb.linearVelocity.magnitude;
            float effectiveMaxSpeed = (_config != null ? _config.puckMaxSpeed : 14f) * _maxSpeedMultiplier;
            if (speed > effectiveMaxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * effectiveMaxSpeed;
            else if (speed > 0.01f && _config != null && speed < _config.puckMinSpeedAfterHit)
                _rb.linearVelocity = _rb.linearVelocity.normalized * _config.puckMinSpeedAfterHit;

            ClampInsideBoard();
        }

        // Safety net: even with Continuous Collision Detection enabled,
        // guarantee the puck can never end up outside the playfield.
        // This catches corner double-collisions, high Time.timeScale,
        // or any future change that reintroduces tunneling. We clamp
        // position AND zero/reflect the offending velocity component so
        // it doesn't just get re-pushed out again next frame.
        private void ClampInsideBoard()
        {
            if (_config == null) return;

            // GameConfig.puckRadius is the single source of truth used by
            // the AI's corner/recover math too (see BotBrain). Prefer it
            // here so both systems always agree on the puck's size; fall
            // back to the live collider only if it's missing.
            float radius = _config.puckRadius > 0f
                ? _config.puckRadius
                : (_collider != null ? _collider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y) : 0.3f);

#if UNITY_EDITOR
            if (_collider != null)
            {
                float actual = _collider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
                if (Mathf.Abs(actual - _config.puckRadius) > 0.02f)
                    Debug.LogWarning($"Puck: GameConfig.puckRadius ({_config.puckRadius}) doesn't match " +
                                      $"the actual collider radius ({actual}). AI predictions and wall " +
                                      $"clamping may be slightly off near corners. Update GameConfig to match.");
            }
#endif

            float limitX = _config.boardHalfWidth - radius;
            float limitY = _config.boardHalfHeight - radius;

            Vector2 pos = _rb.position;
            Vector2 vel = _rb.linearVelocity;
            bool corrected = false;

            if (pos.x > limitX) { pos.x = limitX; vel.x = -Mathf.Abs(vel.x); corrected = true; }
            else if (pos.x < -limitX) { pos.x = -limitX; vel.x = Mathf.Abs(vel.x); corrected = true; }

            // BUG FIX: only clamp the Y edges where there's actually a
            // SOLID wall. The top/bottom edges have a gap (the goal mouth)
            // that the puck must be allowed to pass through freely so the
            // Goal trigger can register the score. Previously this clamp
            // treated the whole top/bottom edge as solid, so the puck got
            // bounced back even when it was lined up with the open net —
            // it only "scored" on fast shots that crossed the goal
            // trigger before this clamp ran in the same/next frame.
            bool inGoalGapX = Mathf.Abs(pos.x) <= _config.goalHalfWidth - radius;

            if (!inGoalGapX)
            {
                if (pos.y > limitY) { pos.y = limitY; vel.y = -Mathf.Abs(vel.y); corrected = true; }
                else if (pos.y < -limitY) { pos.y = -limitY; vel.y = Mathf.Abs(vel.y); corrected = true; }
            }
            // If inGoalGapX is true, we deliberately do nothing on Y here —
            // let the puck sail through into the goal trigger zone.

            if (corrected)
            {
                _rb.position = pos;
                _rb.linearVelocity = vel;
            }
        }

        // Fire impact events for feel + audio on every collision.
    private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 contact = collision.GetContact(0).point;
            EventBus.RaisePuckImpact(contact);

            int layer = collision.gameObject.layer;
            float speed = _rb.linearVelocity.magnitude;

            if (layer == LayerMask.NameToLayer("Paddle"))
            {
                var paddleComponent = collision.gameObject.GetComponent<Paddle>();
                if (paddleComponent != null) LastHitter = paddleComponent.Side;

                AudioManager.Instance.Play(SoundId.PaddleHit);
                // Feel: punch + flash the paddle that was hit.
                var punch = collision.gameObject.GetComponent<ScalePunch>();
                if (punch != null) punch.Punch();
                var flash = collision.gameObject.GetComponent<FlashEffect>();
                if (flash != null) flash.Flash();
                // Tell the bot it just struck the puck, so BotBrain can
                // pull back (RECOIL) instead of re-closing to point-blank
                // range next frame. No-op for the player's own paddle.
                var bot = collision.gameObject.GetComponent<BotPaddle>();
                if (bot != null) bot.NotifyHit();
                // Hard hits add hit-stop + a little shake.
                if (speed > _config.puckMaxSpeed * 0.7f)
                {
                    if (HitStop.Instance != null) HitStop.Instance.Stop(0.04f);
                    if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.12f, 0.15f);
                }
            }
            else if (layer == LayerMask.NameToLayer("Wall"))
            {
                AudioManager.Instance.Play(SoundId.WallHit);
            }
        }
    }
}