// PowerUpManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace AIAirHockey
{
    public class PowerUpManager : Singleton<PowerUpManager>
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private GameObject _powerUpPrefab;
        [SerializeField] private Sprite[] _powerUpSprites; // 5 sprites from PowerUp_Placeholder_SpriteSheet
        [SerializeField] private GameObject _goalShieldPrefab;
        [SerializeField] private GameObject _secondaryPuckPrefab;

        private List<PowerUpItem> _activeItems = new List<PowerUpItem>();
        public List<PowerUpItem> ActivePowerUpItems => _activeItems ?? (_activeItems = new List<PowerUpItem>());

        private float _nextSpawnTime;
        private bool _isSpawningActive;

        // Active Status Effects Tracking
        private Dictionary<PlayerSide, Coroutine> _titanPaddleRoutines = new Dictionary<PlayerSide, Coroutine>();
        private Coroutine _speedPuckRoutine;
        private Coroutine _timeWarpRoutine;
        private Coroutine _multiPuckRoutine;
        private GameObject _secondaryPuckInstance;
        private GoalShield _activeTopShield;
        private GoalShield _activeBottomShield;

        private Paddle _playerPaddle;
        private Paddle _botPaddle;
        private Puck _primaryPuck;

        private void OnEnable()
        {
            EventBus.OnGoalScored += HandleGoalScored;
            EventBus.OnMatchStateChanged += HandleMatchStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnGoalScored -= HandleGoalScored;
            EventBus.OnMatchStateChanged -= HandleMatchStateChanged;
        }

        private void Start()
        {
            FindSceneReferences();
            ScheduleNextSpawn(isFirstSpawn: true);
        }

        private void FindSceneReferences()
        {
            if (_primaryPuck == null) _primaryPuck = FindObjectOfType<Puck>();
            var paddles = FindObjectsOfType<Paddle>();
            foreach (var p in paddles)
            {
                if (p.Side == PlayerSide.Bottom) _playerPaddle = p;
                else if (p.Side == PlayerSide.Top) _botPaddle = p;
            }
        }

        public void SetSpawningActive(bool active)
        {
            _isSpawningActive = active;
            if (active)
            {
                ScheduleNextSpawn(isFirstSpawn: true);
            }
        }

        private void ScheduleNextSpawn(bool isFirstSpawn)
        {
            float min = isFirstSpawn ? 2.0f : 8.0f;
            float max = isFirstSpawn ? 4.0f : 12.0f;

            _nextSpawnTime = Time.time + Random.Range(min, max);
        }

        private void Update()
        {
            CleanItemReferences();

            if (!_isSpawningActive) return;

            int maxActive = _config != null ? _config.powerUpMaxActive : 1;
            if (_activeItems.Count < maxActive && Time.time >= _nextSpawnTime)
            {
                TrySpawnRandomPowerUp();
                ScheduleNextSpawn(isFirstSpawn: false);
            }

            UpdateTimeWarpEffect();
        }

        private void CleanItemReferences()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i] == null || !_activeItems[i].gameObject.activeInHierarchy)
                {
                    _activeItems.RemoveAt(i);
                }
            }
        }

        public void TrySpawnRandomPowerUp()
        {
            FindSceneReferences();
            if (!TryGetValidSpawnPosition(out Vector2 spawnPos)) return;

            PowerUpType randomType = (PowerUpType)Random.Range(0, 5);

            GameObject obj = Instantiate(_powerUpPrefab, spawnPos, Quaternion.identity);

            var item = obj.GetComponent<PowerUpItem>();
            if (item == null) item = obj.AddComponent<PowerUpItem>();

            Sprite icon = (_powerUpSprites != null && (int)randomType < _powerUpSprites.Length)
                ? _powerUpSprites[(int)randomType]
                : null;

            float lifetime = _config != null ? _config.powerUpLifetime : 8f;
            item.Configure(randomType, lifetime, icon);

            _activeItems.Add(item);
            EventBus.RaisePowerUpSpawned(randomType, spawnPos);
        }

        private bool TryGetValidSpawnPosition(out Vector2 pos)
        {
            pos = Vector2.zero;
            FindSceneReferences();

            float boardHW = _config != null ? _config.boardHalfWidth : 2.6f;
            float boardHH = _config != null ? _config.boardHalfHeight : 4.8f;
            float goalHW = _config != null ? _config.goalHalfWidth : 1.0f;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                float rx = Random.Range(-boardHW + 0.5f, boardHW - 0.5f);
                float ry = Random.Range(-boardHH + 0.8f, boardHH - 0.8f);
                Vector2 candidate = new Vector2(rx, ry);

                // Exclusion Zone 1: Goal mouth gap
                if (Mathf.Abs(candidate.x) <= (goalHW + 0.3f) && Mathf.Abs(candidate.y) > (boardHH - 1.2f))
                    continue;

                // Exclusion Zone 2: Center line margin
                if (Mathf.Abs(candidate.y) < 0.5f)
                    continue;

                // Exclusion Zone 3: Puck proximity
                if (_primaryPuck != null && Vector2.Distance(candidate, _primaryPuck.Position) < 1.5f)
                    continue;

                // Exclusion Zone 4: Paddle proximity
                if (_playerPaddle != null && Vector2.Distance(candidate, _playerPaddle.Position) < 1.0f)
                    continue;
                if (_botPaddle != null && Vector2.Distance(candidate, _botPaddle.Position) < 1.0f)
                    continue;

                pos = candidate;
                return true;
            }
            return false;
        }

        public void CollectPowerUp(PowerUpItem item, PlayerSide collector)
        {
            if (item == null) return;
            FindSceneReferences();
            PowerUpType type = item.Type;

            if (_activeItems.Contains(item))
            {
                _activeItems.Remove(item);
            }

            item.DespawnSelf();
            EventBus.RaisePowerUpCollected(type, collector);

            ApplyPowerUpEffect(type, collector);
        }

        private void ApplyPowerUpEffect(PowerUpType type, PlayerSide collector)
        {
            switch (type)
            {
                case PowerUpType.SpeedPuck:
                    ApplySpeedPuckEffect(collector);
                    break;
                case PowerUpType.TitanPaddle:
                    ApplyTitanPaddleEffect(collector);
                    break;
                case PowerUpType.GoalShield:
                    ApplyGoalShieldEffect(collector);
                    break;
                case PowerUpType.TimeWarp:
                    ApplyTimeWarpEffect(collector);
                    break;
                case PowerUpType.MultiPuck:
                    ApplyMultiPuckEffect(collector);
                    break;
            }
        }

        // --- 1. SPEED PUCK ---
        private void ApplySpeedPuckEffect(PlayerSide collector)
        {
            if (_speedPuckRoutine != null) StopCoroutine(_speedPuckRoutine);
            _speedPuckRoutine = StartCoroutine(SpeedPuckRoutine(collector));
        }

        private System.Collections.IEnumerator SpeedPuckRoutine(PlayerSide collector)
        {
            FindSceneReferences();
            if (_primaryPuck != null) _primaryPuck.SetMaxSpeedMultiplier(1.7f); // +70% speed limit (14 -> 24 u/s)

            yield return new WaitForSeconds(6.0f);

            if (_primaryPuck != null) _primaryPuck.SetMaxSpeedMultiplier(1.0f);
            EventBus.RaisePowerUpExpired(PowerUpType.SpeedPuck, collector);
            _speedPuckRoutine = null;
        }

        // --- 2. TITAN PADDLE ---
        private void ApplyTitanPaddleEffect(PlayerSide collector)
        {
            if (_titanPaddleRoutines == null) _titanPaddleRoutines = new Dictionary<PlayerSide, Coroutine>();
            if (_titanPaddleRoutines.TryGetValue(collector, out Coroutine existing) && existing != null)
            {
                StopCoroutine(existing);
            }
            _titanPaddleRoutines[collector] = StartCoroutine(TitanPaddleRoutine(collector));
        }

        private System.Collections.IEnumerator TitanPaddleRoutine(PlayerSide collector)
        {
            FindSceneReferences();
            Paddle paddle = collector == PlayerSide.Bottom ? _playerPaddle : _botPaddle;
            if (paddle != null) paddle.SetScaleModifier(1.5f); // +50% paddle radius

            yield return new WaitForSeconds(8.0f);

            if (paddle != null) paddle.SetScaleModifier(1.0f);
            EventBus.RaisePowerUpExpired(PowerUpType.TitanPaddle, collector);
            _titanPaddleRoutines.Remove(collector);
        }

        // --- 3. GOAL SHIELD ---
        private void ApplyGoalShieldEffect(PlayerSide collector)
        {
            FindSceneReferences();
            float boardHH = _config != null ? _config.boardHalfHeight : 4.8f;
            float goalY = collector == PlayerSide.Bottom ? -boardHH + 0.2f : boardHH - 0.2f;
            Vector2 shieldPos = new Vector2(0f, goalY);

            if (collector == PlayerSide.Bottom && _activeBottomShield != null)
            {
                DestroyOrDespawn(_activeBottomShield.gameObject);
            }
            else if (collector == PlayerSide.Top && _activeTopShield != null)
            {
                DestroyOrDespawn(_activeTopShield.gameObject);
            }

            GameObject shieldObj = null;
            if (_goalShieldPrefab != null && PoolManager.Exists)
            {
                shieldObj = PoolManager.Instance.Spawn(_goalShieldPrefab, shieldPos, Quaternion.identity);
            }
            else if (_goalShieldPrefab != null)
            {
                shieldObj = Instantiate(_goalShieldPrefab, shieldPos, Quaternion.identity);
            }
            else
            {
                // Fallback shield object
                shieldObj = new GameObject("GoalShield_" + collector);
                shieldObj.transform.position = shieldPos;
                var box = shieldObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2((_config != null ? _config.goalHalfWidth : 1.0f) * 2f, 0.3f);
                var gs = shieldObj.AddComponent<GoalShield>();
                gs.Initialize(collector, 10.0f);
            }

            var shieldComp = shieldObj.GetComponent<GoalShield>();
            if (shieldComp == null) shieldComp = shieldObj.AddComponent<GoalShield>();
            if (shieldComp != null) shieldComp.Initialize(collector, 10.0f);

            if (collector == PlayerSide.Bottom) _activeBottomShield = shieldComp;
            else _activeTopShield = shieldComp;
        }

        public void NotifyShieldExpired(PlayerSide side, bool absorbedHit)
        {
            if (side == PlayerSide.Bottom) _activeBottomShield = null;
            else _activeTopShield = null;

            EventBus.RaisePowerUpExpired(PowerUpType.GoalShield, side);
        }

        // --- 4. TIME WARP ---
        private PlayerSide _timeWarpCollector;
        private void ApplyTimeWarpEffect(PlayerSide collector)
        {
            _timeWarpCollector = collector;
            if (_timeWarpRoutine != null) StopCoroutine(_timeWarpRoutine);
            _timeWarpRoutine = StartCoroutine(TimeWarpRoutine(collector));
        }

        private System.Collections.IEnumerator TimeWarpRoutine(PlayerSide collector)
        {
            yield return new WaitForSeconds(7.0f);

            FindSceneReferences();
            if (_primaryPuck != null) _primaryPuck.SetMaxSpeedMultiplier(1.0f);
            EventBus.RaisePowerUpExpired(PowerUpType.TimeWarp, collector);
            _timeWarpRoutine = null;
        }

        private void UpdateTimeWarpEffect()
        {
            if (_timeWarpRoutine == null || _primaryPuck == null) return;

            // Opponent half check: Bottom player collector -> opponent half is Y > 0
            bool onOpponentHalf = (_timeWarpCollector == PlayerSide.Bottom && _primaryPuck.Position.y > 0f)
                               || (_timeWarpCollector == PlayerSide.Top && _primaryPuck.Position.y < 0f);

            if (onOpponentHalf)
            {
                _primaryPuck.SetMaxSpeedMultiplier(0.5f); // -50% puck speed on opponent half
            }
            else
            {
                _primaryPuck.SetMaxSpeedMultiplier(1.0f);
            }
        }

        // --- 5. MULTI PUCK ---
        private void ApplyMultiPuckEffect(PlayerSide collector)
        {
            FindSceneReferences();
            if (_primaryPuck == null) return;

            if (_multiPuckRoutine != null) StopCoroutine(_multiPuckRoutine);
            _multiPuckRoutine = StartCoroutine(MultiPuckRoutine(collector));
        }

        private System.Collections.IEnumerator MultiPuckRoutine(PlayerSide collector)
        {
            Vector2 spawnPos = _primaryPuck.Position + new Vector2(0.3f, 0f);
            Vector2 launchDir = new Vector2(-_primaryPuck.Velocity.x, _primaryPuck.Velocity.y).normalized;
            if (launchDir.sqrMagnitude < 0.001f) launchDir = Vector2.up;

            if (_secondaryPuckInstance != null)
            {
                DestroyOrDespawn(_secondaryPuckInstance);
                _secondaryPuckInstance = null;
            }

            if (_secondaryPuckPrefab != null && PoolManager.Exists)
            {
                _secondaryPuckInstance = PoolManager.Instance.Spawn(_secondaryPuckPrefab, spawnPos, Quaternion.identity);
            }
            else if (_secondaryPuckPrefab != null)
            {
                _secondaryPuckInstance = Instantiate(_secondaryPuckPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Instantiate duplicate copy of primary puck
                _secondaryPuckInstance = Instantiate(_primaryPuck.gameObject, spawnPos, Quaternion.identity);
            }

            var secPuck = _secondaryPuckInstance.GetComponent<Puck>();
            if (secPuck != null)
            {
                secPuck.ResetPuck(spawnPos, launchDir);
            }

            yield return new WaitForSeconds(12.0f);

            DespawnSecondaryPuck();
            EventBus.RaisePowerUpExpired(PowerUpType.MultiPuck, collector);
            _multiPuckRoutine = null;
        }

        // Safeguard #3: Despawn secondary puck immediately when a goal is scored
        private void HandleGoalScored(PlayerSide side)
        {
            ResetAllPowerUpEffects();
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (state == MatchState.Playing)
            {
                SetSpawningActive(true);
            }
            else
            {
                SetSpawningActive(false);
                if (state == MatchState.GoalScored || state == MatchState.Finished)
                {
                    DespawnAllTableItems();
                    ResetAllPowerUpEffects();
                }
            }
        }

        private void DespawnSecondaryPuck()
        {
            if (_secondaryPuckInstance != null)
            {
                DestroyOrDespawn(_secondaryPuckInstance);
                _secondaryPuckInstance = null;
            }
        }

        private void DespawnAllTableItems()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i] != null)
                {
                    _activeItems[i].DespawnSelf();
                }
            }
            _activeItems.Clear();
        }

        private void ResetAllPowerUpEffects()
        {
            if (_speedPuckRoutine != null) { StopCoroutine(_speedPuckRoutine); _speedPuckRoutine = null; }
            if (_timeWarpRoutine != null) { StopCoroutine(_timeWarpRoutine); _timeWarpRoutine = null; }
            if (_multiPuckRoutine != null) { StopCoroutine(_multiPuckRoutine); _multiPuckRoutine = null; }

            foreach (var kvp in _titanPaddleRoutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _titanPaddleRoutines.Clear();

            FindSceneReferences();
            if (_primaryPuck != null) _primaryPuck.SetMaxSpeedMultiplier(1.0f);
            if (_playerPaddle != null) _playerPaddle.SetScaleModifier(1.0f);
            if (_botPaddle != null) _botPaddle.SetScaleModifier(1.0f);

            DespawnSecondaryPuck();

            if (_activeBottomShield != null) { DestroyOrDespawn(_activeBottomShield.gameObject); _activeBottomShield = null; }
            if (_activeTopShield != null) { DestroyOrDespawn(_activeTopShield.gameObject); _activeTopShield = null; }
        }

        private void DestroyOrDespawn(GameObject obj)
        {
            if (obj == null) return;
            if (PoolManager.Exists) PoolManager.Instance.Despawn(obj);
            else Destroy(obj);
        }
    }
}
