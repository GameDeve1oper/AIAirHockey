// PowerUpItem.cs
using UnityEngine;

namespace AIAirHockey
{
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpItem : MonoBehaviour
    {
        [SerializeField] private PowerUpType _type;
        [SerializeField] private float _lifetime = 8.0f;

        private SpriteRenderer _spriteRenderer;
        private Renderer _renderer;
        private Collider2D _collider;
        private float _spawnTime;
        private bool _isCollected;

        public PowerUpType Type => _type;
        public Vector2 Position => transform.position;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider2D>();
            if (_spriteRenderer != null) _spriteRenderer.sortingOrder = 10;
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
            _isCollected = false;
            if (_collider != null) _collider.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            else if (_renderer != null) _renderer.enabled = true;
        }

        public void Configure(PowerUpType type, float lifetime, Sprite sprite)
        {
            _type = type;
            _lifetime = lifetime;
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.enabled = true;
                _spriteRenderer.sortingOrder = 100;
                _spriteRenderer.color = Color.white;
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            // Simple visual animation: gentle rotation and pulsing scale
            transform.Rotate(0f, 0f, 45f * Time.deltaTime);
            float scale = 1.0f + Mathf.Sin(Time.time * 4f) * 0.1f;
            transform.localScale = new Vector3(scale, scale, 1.0f);

            // Last 2 seconds blink warning before auto-despawn
            float age = Time.time - _spawnTime;
            float remaining = _lifetime - age;

            if (remaining <= 2.0f && remaining > 0f)
            {
                bool visible = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
                SetVisible(visible);
            }

            if (age >= _lifetime)
            {
                DespawnSelf();
            }
        }

        private void SetVisible(bool visible)
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = visible;
            else if (_renderer != null) _renderer.enabled = visible;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            PlayerSide collector = PlayerSide.Bottom;
            bool validHit = false;

            var paddle = other.GetComponent<Paddle>();
            if (paddle != null)
            {
                collector = paddle.Side;
                validHit = true;
            }
            else
            {
                var puck = other.GetComponent<Puck>();
                if (puck != null)
                {
                    collector = puck.LastHitter;
                    validHit = true;
                }
            }

            if (validHit)
            {
                _isCollected = true;
                if (_collider != null) _collider.enabled = false;

                if (PowerUpManager.Exists)
                {
                    PowerUpManager.Instance.CollectPowerUp(this, collector);
                }
                else
                {
                    DespawnSelf();
                }
            }
        }

        public void DespawnSelf()
        {
            if (PoolManager.Exists)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
