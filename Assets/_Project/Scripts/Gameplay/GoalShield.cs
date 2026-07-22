// GoalShield.cs
using UnityEngine;

namespace AIAirHockey
{
    [RequireComponent(typeof(Collider2D))]
    public class GoalShield : MonoBehaviour
    {
        [SerializeField] private PlayerSide _side;
        [SerializeField] private float _maxDuration = 10.0f;

        private float _spawnTime;

        public PlayerSide Side => _side;

        public void Initialize(PlayerSide side, float maxDuration = 10.0f)
        {
            _side = side;
            _maxDuration = maxDuration;
            _spawnTime = Time.time;
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void Update()
        {
            // Safeguard #4: Hard-expire at maxDuration (10s) regardless of hit state
            if (Time.time - _spawnTime >= _maxDuration)
            {
                BreakShield(absorbedHit: false);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var puck = collision.gameObject.GetComponent<Puck>();
            if (puck != null)
            {
                BreakShield(absorbedHit: true);
            }
        }

        private void BreakShield(bool absorbedHit)
        {
            if (PowerUpManager.Exists)
            {
                PowerUpManager.Instance.NotifyShieldExpired(_side, absorbedHit);
            }

            if (PoolManager.Exists)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
