// Puck.cs
using UnityEngine;

namespace AIAirHockey
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Puck : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        private Rigidbody2D _rb;
        public Rigidbody2D Body => _rb;
        public Vector2 Position => _rb.position;
        public Vector2 Velocity => _rb.linearVelocity;
        
        private void Awake()
        {
            EnsureInit();
        }

         // Safe one-time init. Works no matter the script execution order.
        private void EnsureInit()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            EnsureInit();
            if (_config != null)
            {
                _rb.mass = _config.puckMass;
                _rb.linearDamping = _config.puckDrag;
            }
        }

        // Called by MatchManager to place and launch the puck each round.
        public void ResetPuck(Vector2 position, Vector2 launchDirection)
        {
            EnsureInit(); // guard against being called before Awake
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
            if (speed > _config.puckMaxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * _config.puckMaxSpeed;
            else if (speed > 0.01f && speed < _config.puckMinSpeedAfterHit)
                _rb.linearVelocity = _rb.linearVelocity.normalized * _config.puckMinSpeedAfterHit;
        }

        // Fire impact events for feel + audio on every collision.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 contact = collision.GetContact(0).point;
            EventBus.RaisePuckImpact(contact);

            int layer = collision.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Paddle"))
                AudioManager.Instance.Play(SoundId.PaddleHit);
            else if (layer == LayerMask.NameToLayer("Wall"))
                AudioManager.Instance.Play(SoundId.WallHit);
        }
    }
}