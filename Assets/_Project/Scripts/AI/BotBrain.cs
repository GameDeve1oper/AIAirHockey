// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    // Pure decision logic for the bot paddle. Given the puck state it returns
    // the world-space point the BotPaddle should move toward. No Unity
    // component lifecycle in here, so it stays cheap, deterministic and
    // mobile-friendly (only struct math per FixedUpdate, zero allocations).
    //
    // Behaviour model (Glow Hockey style):
    //   PERCEPTION  - the bot reacts to a low-pass *smoothed* view of the puck
    //                 (time constant = reactionTime). This is what makes it
    //                 feel human/laggy instead of frame-perfect.
    //   RECOVER     - puck slipped deep behind us -> retreat to the guard line
    //                 (never shove it into our own net).
    //   DEFENCE     - puck coming at us -> sit on the guard line at the puck's
    //                 predicted crossing x (bank shots included).
    //   ATTACK      - puck on our side and safe -> drive through it toward the
    //                 player's goal so the contact angle sends it goalward.
    //   IDLE        - puck on the player's half -> hold the line, shade toward
    //                 the puck's x so we're ready.
    // Aim imperfection is sampled-and-held (not per-frame noise) and eased.
    public class BotBrain
    {
        private readonly DifficultyProfile _profile;
        private readonly PuckPredictor _predictor;
        private readonly Vector2 _playerGoal; // bot defends Top, scores into Bottom

        // Perceived puck state (low-pass filtered = reaction lag).
        private bool _initialized;
        private Vector2 _perceivedPos;
        private Vector2 _perceivedVel;

        // Aim imperfection (held + eased so it never becomes high-freq jitter).
        private float _aimBias;
        private float _aimBiasTarget;
        private float _aimTimer;

        // --- tuning constants (kept named here, not magic numbers inline) ---
        
        private Vector2 _smoothedTarget; // Track the human-lagged target output
        private const float ApproachVy     = 0.4f;  // vy above this = "coming at us"
        private const float RecoverMargin  = 0.15f; // puck this far past us = behind
        private const float MinDrive       = 0.15f; // strike follow-through (passive)
        private const float MaxDrive       = 0.75f; // strike follow-through (aggressive)
        private const float IdleShade      = 0.5f;  // how much to track puck x when idle
        private const float AimIntervalMin = 0.35f; // re-aim cadence (sec)
        private const float AimIntervalMax = 0.70f;
        private const float MistakeMult    = 3f;    // a "mistake" exaggerates the aim error
        private const float AimEaseTau     = 0.10f; // aim-bias easing time constant

        public BotBrain(DifficultyProfile profile, GameConfig config)
        {
            _profile = profile;

            // Honour the documented single source of truth for puck size
            // (GameConfig.puckRadius) instead of a hardcoded literal, so the
            // predictor's wall reflection always matches the real puck.
            float puckRadius = config != null ? config.puckRadius : 0.3f;
            float halfWidth  = config != null ? config.boardHalfWidth : 2.6f;
            float halfHeight = config != null ? config.boardHalfHeight : 4.8f;

            _predictor  = new PuckPredictor(halfWidth, puckRadius);
            _playerGoal = new Vector2(0f, -halfHeight);
        }

        /* public Vector2 Decide(Vector2 paddlePos, Vector2 puckPos, Vector2 puckVel, float dt)
        {
            UpdatePerception(puckPos, puckVel, dt);
            UpdateAim(dt);

            Vector2 p = _perceivedPos;
            Vector2 v = _perceivedVel;

            float guardY      = _profile.defenseLineY;
            float aggression  = Mathf.Clamp01(_profile.aggression);
            float attackLineY = Mathf.Lerp(guardY, _profile.minY, aggression);
            float horizon     = Mathf.Max(_profile.predictionTime, 0f);

            bool onBotSide    = p.y > 0f;
            bool approaching  = v.y > ApproachVy;
            // Only "behind" when the puck is genuinely deep past us (past the
            // guard line). This keeps attacks near center from being mistaken
            // for a recover situation, so the paddle never oscillates.
            bool behindPaddle = onBotSide && p.y > guardY && p.y > paddlePos.y + RecoverMargin;

            Vector2 target;

            if (behindPaddle)
            {
                // RECOVER: it got deep behind us. Drop to the guard line at the
                // puck's x and wait to intercept the rebound. We deliberately
                // do NOT push up through it (that would be an own goal).
                target = new Vector2(p.x, guardY);
            }
            else if (approaching)
            {
                // DEFENCE: block on the guard line where the puck will cross it.
                // predictionTime bounds how far ahead the bot can foresee, so a
                // low-skill bot reacts late to long/banked shots.
                float x = _predictor.PredictCrossingX(p, v, guardY, horizon);
                target = new Vector2(x, guardY);
            }
            else if (onBotSide)
            {
                // ATTACK: lead the puck, then aim the contact point on the
                // goal-far side so driving through it sends the puck toward the
                // player's goal at an angle. Forward reach scales with aggression.
                Vector2 lead   = _predictor.Predict(p, v, horizon);
                Vector2 aimDir = (lead - _playerGoal).normalized; // goal -> puck
                float drive    = Mathf.Lerp(MinDrive, MaxDrive, aggression);
                target = lead - aimDir * drive;
                target.y = Mathf.Max(target.y, attackLineY);
            }
            else
            {
                // IDLE: hold the line, shade toward the puck's x to stay ready.
                target = new Vector2(p.x * IdleShade, guardY);
            }

            // Human aim imperfection (held + eased, never per-frame jitter).
            target.x += _aimBias;

            // Vertical band from the profile; BotPaddle.ClampToHalf finalises
            // the hard half-board clamp.
            target.y = Mathf.Clamp(target.y, _profile.minY, _profile.maxY);
            return target;
        } */
        public Vector2 Decide(Vector2 paddlePos, Vector2 puckPos, Vector2 puckVel, float dt)
{
    UpdateAim(dt);

    // FIX 1: Base tactical math on the REAL puck state, not a double-lagged vision.
    // This allows the AI to accurately calculate bank shots even if it's "slow" to move.
    Vector2 p = puckPos; 
    Vector2 v = puckVel;

    float guardY = _profile.defenseLineY;
    float aggression = Mathf.Clamp01(_profile.aggression);
    float attackLineY = Mathf.Lerp(guardY, _profile.minY, aggression);
    float horizon = Mathf.Max(_profile.predictionTime, 0f);

    bool onBotSide = p.y > 0f;
    bool approaching = v.y > ApproachVy;
    bool behindPaddle = onBotSide && p.y > guardY && p.y > paddlePos.y + RecoverMargin;

    Vector2 rawTarget;

    if (behindPaddle)
    {
        // RECOVER
        rawTarget = new Vector2(p.x, guardY);
    }
    else if (approaching)
    {
        // DEFENCE: Predicts exactly where it lands based on crisp, un-lagged physics
        float x = _predictor.PredictCrossingX(p, v, guardY, horizon);
        rawTarget = new Vector2(x, guardY);
    }
    else if (onBotSide)
    {
        // ATTACK: Intercepts fluidly by aiming at the true expected future position
        Vector2 lead = _predictor.Predict(p, v, horizon);
        Vector2 aimDir = (lead - _playerGoal).normalized;
        float drive = Mathf.Lerp(MinDrive, MaxDrive, aggression);
        rawTarget = lead - aimDir * drive;
        rawTarget.y = Mathf.Max(rawTarget.y, attackLineY);
    }
    else
    {
        // IDLE
        rawTarget = new Vector2(p.x * IdleShade, guardY);
    }

    // Apply human aim imperfection
    rawTarget.x += _aimBias;
    rawTarget.y = Mathf.Clamp(rawTarget.y, _profile.minY, _profile.maxY);

    // FIX 2: Apply the human perception lag to the TARGET destination, not the raw puck.
    // This decouples tracking accuracy from physical paddle execution.
    if (!_initialized)
    {
        _smoothedTarget = rawTarget;
        _initialized = true;
    }
    else
    {
        // Smoothly lag the target position based on the profile's reaction time
        float tau = Mathf.Max(_profile.reactionTime, 0.0001f);
        float k = 1f - Mathf.Exp(-dt / tau);
        _smoothedTarget = Vector2.Lerp(_smoothedTarget, rawTarget, k);
    }

    return _smoothedTarget;
}
        // Exponential low-pass toward the real puck. Time constant = reactionTime:
        // higher = laggier/easier, lower = sharper/harder. Continuous (per frame)
        // instead of a cached step, so movement stays fluid with no snap.
        private void UpdatePerception(Vector2 puckPos, Vector2 puckVel, float dt)
        {
            if (!_initialized)
            {
                _perceivedPos = puckPos;
                _perceivedVel = puckVel;
                _initialized = true;
                return;
            }

            float tau = Mathf.Max(_profile.reactionTime, 0.0001f);
            float k = 1f - Mathf.Exp(-dt / tau);
            _perceivedPos = Vector2.Lerp(_perceivedPos, puckPos, k);
            _perceivedVel = Vector2.Lerp(_perceivedVel, puckVel, k);
        }

        // Sample-and-hold aim error: pick a new offset every ~0.35-0.7s and ease
        // toward it. Occasionally exaggerate it (a human "misjudge"). This gives
        // believable, stable-within-a-rally aiming instead of a vibrating servo.
        private void UpdateAim(float dt)
        {
            _aimTimer -= dt;
            if (_aimTimer <= 0f)
            {
                _aimTimer = Random.Range(AimIntervalMin, AimIntervalMax);
                float e = Random.Range(-_profile.aimError, _profile.aimError);
                if (Random.value < Mathf.Clamp01(_profile.mistakeChance))
                    e *= MistakeMult;
                _aimBiasTarget = e;
            }

            float k = 1f - Mathf.Exp(-dt / AimEaseTau);
            _aimBias = Mathf.Lerp(_aimBias, _aimBiasTarget, k);
        }
    }
}
