// Paddle.cs
using UnityEngine;

namespace AIAirHockey
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Paddle : MonoBehaviour
    {
        [SerializeField] protected GameConfig _config;
        [SerializeField] protected PlayerSide _side = PlayerSide.Bottom;

        protected Rigidbody2D _rb;
        public PlayerSide Side => _side;
        public Vector2 Position => _rb.position;

        private float _scaleModifier = 1.0f;
        private Vector3 _baseScale = new Vector3(0.07f, 0.07f, 0.07f);
        public float ScaleModifier => _scaleModifier;
        public Vector3 BaseScale => _baseScale;
        public float CurrentRadius => 0.35f * _scaleModifier;

        public void ResetPosition(Vector2 pos)
        {
            if (_rb != null)
            {
                _rb.position = pos;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
            transform.position = pos;
        }

        public void SetScaleModifier(float factor)
        {
            _scaleModifier = Mathf.Max(0.2f, factor);
            if (_baseScale == Vector3.zero) _baseScale = new Vector3(0.07f, 0.07f, 0.07f);
            transform.localScale = _baseScale * _scaleModifier;
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (transform.localScale.x > 0) _baseScale = transform.localScale;
        }

        // Move the kinematic paddle toward a target each physics step.
        // Clamped to this paddle's half of the board so it can't cross center.
        protected void MoveTo(Vector2 target, float maxSpeed)
        {
            target = ClampToHalf(target);
            Vector2 current = _rb.position;
            Vector2 next = Vector2.MoveTowards(current, target, maxSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(next);
        }

        // Keep paddle inside its own half and inside side walls.
        protected Vector2 ClampToHalf(Vector2 p)
        {
            if (_config == null) return p;
            float margin = CurrentRadius; // dynamic margin based on paddle radius
            float minX = -_config.boardHalfWidth + margin;
            float maxX = _config.boardHalfWidth - margin;
            p.x = Mathf.Clamp(p.x, minX, maxX);

            if (_side == PlayerSide.Bottom)
                p.y = Mathf.Clamp(p.y, -_config.boardHalfHeight + margin, -margin);
            else
                p.y = Mathf.Clamp(p.y, margin, _config.boardHalfHeight - margin);
            return p;
        }
    }
}