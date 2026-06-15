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

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
            float margin = 0.35f; // keep paddle off the walls a touch
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