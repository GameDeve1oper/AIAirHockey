// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    public enum AIState { Guard, Follow, Recover, Corner, Recoil }

    public class BotBrain
    {
        // --- tuning constants -----------------------------------------
        private const float SideHysteresis      = 0.05f; // stops state flicker right at center line
        private const float RecoverMargin       = 0.15f; // puck must be this far past us to count as "behind"
        private const float RecoverBehindMult   = 3f;    // how far behind puck to tuck in, x puck radius
        private const float RecoverDodgeMult    = 3f;    // sideways step on way in, x puck radius
        private const float CornerJamMult       = 3f;    // distance below which we call it "jammed", x puck radius
        private const float CornerEscapeMult    = 5f;    // sideways step to open angle in corner, x puck radius
        private const float CornerBehindMult    = 1f;    // slight behind-offset while escaping corner, x puck radius
        private const float CornerWallFraction  = 0.3f;  // last 30% of half-width counts as "near wall"
        private const float CornerDepthFraction = 0.35f; // must be at least this far into our half for corner

        private const float FollowBehindMult    = 1.0f;  // x puck radius, offset toward own goal
        private const float RecoilDuration      = 0.18f; // seconds
        private const float RecoilBehindMult    = 2.5f;  // x puck radius

        private const float WanderIntervalMin   = 0.5f;  // seconds between picking new wander point
        private const float WanderIntervalMax   = 1.0f;
        private const float WanderYMinFraction  = 0.1f;  // wander roams across most of our half...
        private const float WanderYMaxFraction  = 0.85f; // ...but stops short of goal mouth

        private readonly float _halfWidth;
        private readonly float _halfHeight;
        private readonly float _puckRadius;
        private readonly float _paddleMargin;

        private readonly float _cornerWallBand;
        private readonly float _cornerDepth;
        private readonly float _reachX;
        private readonly float _maxY;

        private AIState _state = AIState.Guard;
        public AIState CurrentState => _state;

        private DifficultyProfile _profile;

        // --- Reaction & Perception State ---
        private float _perceptionFrozenUntil = -1f;
        private Vector2 _frozenPuckPos;
        private Vector2 _frozenPuckVel;
        private Vector2 _lastPuckVel;

        // --- Prediction State ---
        private float _currentPredictedX;

        // --- Aim Error State ---
        private float _currentAimOffset;
        private bool _wasInFollow;

        // --- Recoil State ---
        private float _recoilUntil = -1f;

        // --- Idle Wander State ---
        private Vector2 _wanderTarget;
        private float _nextWanderTime = -1f;

        // Debug Properties for Gizmos
        public Vector2 DebugRawPredictionPoint { get; private set; }
        public Vector2 CurrentTarget { get; private set; }

        public BotBrain(GameConfig config, DifficultyProfile profile = null)
        {
            _profile      = profile;
            _halfWidth    = config != null ? config.boardHalfWidth  : 2.6f;
            _halfHeight   = config != null ? config.boardHalfHeight : 4.8f;
            _puckRadius   = config != null ? config.puckRadius      : 0.3f;
            _paddleMargin = 0.35f;

            _cornerWallBand = _halfWidth  * CornerWallFraction;
            _cornerDepth    = _halfHeight * CornerDepthFraction;
            _reachX         = _halfWidth  - _paddleMargin;
            _maxY           = _halfHeight - _paddleMargin;
        }

        public void SetProfile(DifficultyProfile profile)
        {
            _profile = profile;
        }

        public void NotifyHit()
        {
            _recoilUntil = Time.time + RecoilDuration;
            // Also trigger perception refresh and sample a new aim error offset for the hit response
            _perceptionFrozenUntil = -1f;
            SampleNewAimOffset();
        }

        public Vector2 Decide(Vector2 paddlePos, Vector2 actualPuckPos, Vector2 actualPuckVel, System.Collections.Generic.List<PowerUpItem> activePowerUps = null)
        {
            if (activePowerUps == null && PowerUpManager.Exists)
            {
                activePowerUps = PowerUpManager.Instance.ActivePowerUpItems;
            }

            // 1. PERCEPTUAL FREEZE & REACTION TIME
            // Check for major event (velocity direction flip or impact)
            bool velDirectionFlipped = Vector2.Dot(_lastPuckVel.normalized, actualPuckVel.normalized) < 0.2f
                                       && actualPuckVel.sqrMagnitude > 1.0f;
            _lastPuckVel = actualPuckVel;

            if (velDirectionFlipped && _profile != null && _profile.reactionTime > 0f)
            {
                _perceptionFrozenUntil = Time.time + _profile.reactionTime;
                _frozenPuckPos = actualPuckPos;
                _frozenPuckVel = actualPuckVel;
            }

            Vector2 perceivedPos = Time.time < _perceptionFrozenUntil ? _frozenPuckPos : actualPuckPos;
            Vector2 perceivedVel = Time.time < _perceptionFrozenUntil ? _frozenPuckVel : actualPuckVel;

            // 2. GOAL-LINE EMERGENCY OVERRIDE (Defense takes absolute priority - ignores power-ups)
            float emergencyY = _halfHeight * 0.55f;
            if (actualPuckPos.y > emergencyY)
            {
                float danger = Mathf.Clamp01((actualPuckPos.y - emergencyY) / (_halfHeight - emergencyY - _paddleMargin));
                perceivedPos = Vector2.Lerp(perceivedPos, actualPuckPos, danger);
                perceivedVel = Vector2.Lerp(perceivedVel, actualPuckVel, danger);
            }

            // 3. CONTINUOUS TRAJECTORY PREDICTION (EXPONENTIAL LERP)
            if (_profile != null && _profile.predictionTime > 0f && perceivedVel.y > 0.1f)
            {
                float yTarget = perceivedPos.y + _puckRadius * FollowBehindMult;
                float tReach = (yTarget - perceivedPos.y) / perceivedVel.y;

                if (tReach > 0f && tReach <= _profile.predictionTime)
                {
                    float xRaw = perceivedPos.x + perceivedVel.x * tReach;
                    float limitX = _halfWidth - _puckRadius;

                    if (Mathf.Abs(xRaw) > limitX)
                    {
                        float over = Mathf.Abs(xRaw) - limitX;
                        float sign = Mathf.Sign(xRaw);
                        xRaw = sign * (limitX - over);

                        if (Random.value < _profile.mistakeChance)
                        {
                            xRaw += sign * Random.Range(0.15f, 0.4f);
                        }
                    }

                    xRaw = Mathf.Clamp(xRaw, -_reachX, _reachX);
                    DebugRawPredictionPoint = new Vector2(xRaw, yTarget);

                    // Continuous Exponential Lerp Blend toward raw prediction
                    _currentPredictedX = Mathf.Lerp(_currentPredictedX, xRaw, 15f * Time.fixedDeltaTime);
                }
                else
                {
                    _currentPredictedX = Mathf.Lerp(_currentPredictedX, perceivedPos.x, 15f * Time.fixedDeltaTime);
                    DebugRawPredictionPoint = Vector2.zero;
                }
            }
            else
            {
                _currentPredictedX = Mathf.Lerp(_currentPredictedX, perceivedPos.x, 15f * Time.fixedDeltaTime);
                DebugRawPredictionPoint = Vector2.zero;
            }

            // 4. STATE MACHINE EVALUATION
            // Puck is considered engageable if it's on bot's half (y > 0) OR if it's sitting neutral near center (abs(y) < 0.35f)
            bool isPuckAtCenter = Mathf.Abs(perceivedPos.x) < 0.8f && Mathf.Abs(perceivedPos.y) < 0.35f && perceivedVel.sqrMagnitude < 4.0f;
            bool onBotSide = _state == AIState.Guard
                ? (perceivedPos.y > -0.05f || isPuckAtCenter)
                : (perceivedPos.y > -SideHysteresis || isPuckAtCenter);

            if (!onBotSide)
            {
                _state = AIState.Guard;
                _wasInFollow = false;
                CurrentTarget = Guard(activePowerUps);
                return CurrentTarget;
            }

            // Defense state 1: Recover (Ignores power-ups)
            if (IsBehind(paddlePos, perceivedPos))
            {
                _state = AIState.Recover;
                _wasInFollow = false;
                CurrentTarget = Recover(paddlePos, perceivedPos);
                return CurrentTarget;
            }

            // Defense state 2: Corner Jam (Ignores power-ups)
            if (IsCornerJam(paddlePos, perceivedPos))
            {
                _state = AIState.Corner;
                _wasInFollow = false;
                CurrentTarget = CornerEscape(perceivedPos);
                return CurrentTarget;
            }

            // Defense state 3: Recoil (Ignores power-ups)
            if (Time.time < _recoilUntil)
            {
                _state = AIState.Recoil;
                CurrentTarget = Recoil(perceivedPos);
                return CurrentTarget;
            }

            // 5. FOLLOW STATE (WITH PER-STROKE STATIC AIM ERROR & OPTIONAL POWER-UP BIAS)
            _state = AIState.Follow;
            if (!_wasInFollow)
            {
                _wasInFollow = true;
                SampleNewAimOffset();
            }

            CurrentTarget = Follow(perceivedPos, activePowerUps);
            return CurrentTarget;
        }

        private void SampleNewAimOffset()
        {
            if (_profile == null || _profile.aimError <= 0f)
            {
                _currentAimOffset = 0f;
                return;
            }
            float sign = Random.value < 0.5f ? -1f : 1f;
            _currentAimOffset = sign * Random.Range(0.05f, _profile.aimError);
        }

        private Vector2 Guard(System.Collections.Generic.List<PowerUpItem> activePowerUps = null)
        {
            // Opportunistic seeking ONLY in Guard state: if a power-up is sitting on bot's half (y > 0.5u), seek it
            if (activePowerUps != null && activePowerUps.Count > 0)
            {
                foreach (var item in activePowerUps)
                {
                    if (item != null && item.gameObject.activeInHierarchy && item.Position.y > 0.5f)
                    {
                        return item.Position;
                    }
                }
            }

            if (Time.time >= _nextWanderTime)
            {
                _wanderTarget = PickWanderPoint();
                _nextWanderTime = Time.time + Random.Range(WanderIntervalMin, WanderIntervalMax);
            }
            return _wanderTarget;
        }

        private Vector2 PickWanderPoint()
        {
            float x = Random.Range(-_reachX, _reachX);
            float y = Random.Range(_halfHeight * WanderYMinFraction, _halfHeight * WanderYMaxFraction);
            return new Vector2(x, y);
        }

        private Vector2 Follow(Vector2 perceivedPuckPos, System.Collections.Generic.List<PowerUpItem> activePowerUps = null)
        {
            float targetX = Mathf.Clamp(_currentPredictedX + _currentAimOffset, -_reachX, _reachX);
            float offset = _puckRadius * FollowBehindMult;
            float targetY = Mathf.Min(perceivedPuckPos.y + offset, _maxY);

            // Path-intercept bias ONLY in Follow state: if a power-up lies on bot's half near movement path, bias X slightly
            if (activePowerUps != null && activePowerUps.Count > 0)
            {
                foreach (var item in activePowerUps)
                {
                    if (item != null && item.gameObject.activeInHierarchy && item.Position.y > 0.5f)
                    {
                        if (Mathf.Abs(item.Position.y - targetY) < 1.2f)
                        {
                            targetX = Mathf.Lerp(targetX, item.Position.x, 0.35f);
                            break;
                        }
                    }
                }
            }

            return new Vector2(targetX, targetY);
        }

        private Vector2 Recoil(Vector2 perceivedPuckPos)
        {
            float offset = _puckRadius * RecoilBehindMult;
            float y = Mathf.Min(perceivedPuckPos.y + offset, _maxY);
            return new Vector2(perceivedPuckPos.x, y);
        }

        public void ResetToGuard()
        {
            _state = AIState.Guard;
            _nextWanderTime = -1f;
            _recoilUntil = -1f;
            _wasInFollow = false;
            _perceptionFrozenUntil = -1f;
            _currentPredictedX = 0f;
            _currentAimOffset = 0f;
        }

        private bool IsBehind(Vector2 paddlePos, Vector2 puckPos)
        {
            return puckPos.y > paddlePos.y + RecoverMargin;
        }

        private Vector2 Recover(Vector2 paddlePos, Vector2 puckPos)
        {
            float dodgeSide = paddlePos.x <= puckPos.x ? -1f : 1f;
            float x = puckPos.x + dodgeSide * (_puckRadius * RecoverDodgeMult);
            x = Mathf.Clamp(x, -_reachX, _reachX);

            float y = Mathf.Min(puckPos.y + _puckRadius * RecoverBehindMult, _maxY);
            return new Vector2(x, y);
        }

        private bool IsCornerJam(Vector2 paddlePos, Vector2 puckPos)
        {
            bool puckNearWall   = (_halfWidth - Mathf.Abs(puckPos.x)) < _cornerWallBand;
            bool puckDeepEnough = puckPos.y > _cornerDepth;
            float jamDist       = _puckRadius * CornerJamMult;
            bool jammedTogether = Vector2.Distance(paddlePos, puckPos) < jamDist;

            return puckNearWall && puckDeepEnough && jammedTogether;
        }

        private Vector2 CornerEscape(Vector2 puckPos)
        {
            float wallSign = puckPos.x >= 0f ? 1f : -1f;
            float x = puckPos.x - wallSign * (_puckRadius * CornerEscapeMult);
            x = Mathf.Clamp(x, -_reachX, _reachX);

            float y = Mathf.Min(puckPos.y + _puckRadius * CornerBehindMult, _maxY);
            return new Vector2(x, y);
        }
    }
}
