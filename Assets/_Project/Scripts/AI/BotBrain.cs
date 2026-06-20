// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    // ------------------------------------------------------------------
    // REWRITE NOTE (this version replaces the old state-machine bot):
    //
    // The previous version used discrete states (Idle/Defend/Intercept/
    // Attack/Recover) chosen by hard threshold checks like
    // "puckVel.y > 0.1f" or "puckPos.y > paddlePos.y + 0.05f". Those
    // thresholds created GAPS: a puck sitting at, say, y = 0.5 with
    // near-zero velocity could fail every condition cleanly, or flip
    // between two states every frame, producing the "AI doesn't react in
    // certain spots" bug.
    //
    // This version (closer to how simple, satisfying arcade air-hockey
    // bots like Glow Hockey behave) drops states entirely. Every single
    // frame it runs ONE continuous formula:
    //   1. Always track the puck's X (no on/off switch).
    //   2. Always chase the puck's Y as a smooth function of how
    //      dangerous the puck is (close + fast = chase deep; far + slow
    //      = hover near the defense line) instead of switching modes.
    //   3. Blend in an "attack-from-behind" steering offset so contact
    //      naturally pushes the puck toward the player's goal, without a
    //      separate Attack state.
    //   4. Clamp once, at the very end, in a way that can't break the
    //      geometry of the computed point (fixes the old corner-stall
    //      bug from the previous patch).
    //
    // No reaction-time-gated state caching either: the cached-target/
    // reactionTimer pattern is kept (it's a deliberate "human reflex"
    // delay), but what gets cached is now always a valid, continuous
    // target -- never a value that depends on which discrete state was
    // active.
    // ------------------------------------------------------------------
    public class BotBrain
    {
        private readonly DifficultyProfile _profile;
        private readonly GameConfig _config;
        private readonly PuckPredictor _predictor;

        // Bot defends the TOP goal. 'Own goal' is at +Y (top of board).
        private readonly float _ownGoalY;
        private readonly float _ownGoalLimit; // safe inner line, never crossed
        private readonly float _benchY;       // resting/home Y when puck is far away

        private float _reactionTimer;
        private Vector2 _cachedTarget;
        private float _mistakeOffset;
        private float _mistakeTimer;

        public BotBrain(DifficultyProfile profile, GameConfig config)
        {
            _profile = profile;
            _config = config;
            _predictor = new PuckPredictor(config.boardHalfWidth, config.puckRadius);
            _ownGoalY = config.boardHalfHeight; // top wall line
            _ownGoalLimit = _ownGoalY - 0.3f;   // never let the paddle target past this
            _benchY = Mathf.Min(_profile.defenseLineY, _ownGoalLimit);
            _cachedTarget = new Vector2(0f, _benchY);
        }

        public Vector2 Decide(Vector2 paddlePos, Vector2 puckPos, Vector2 puckVel, float dt)
        {
            UpdateMistake(dt);

            _reactionTimer -= dt;
            if (_reactionTimer <= 0f)
            {
                _reactionTimer = _profile.reactionTime;
                _cachedTarget = ComputeTarget(paddlePos, puckPos, puckVel);
            }
            return _cachedTarget;
        }

        private Vector2 ComputeTarget(Vector2 paddlePos, Vector2 puckPos, Vector2 puckVel)
        {
            // ---- 1. ALWAYS predict where the puck is heading on X. ----
            // No threshold gate here at all -- every frame, regardless of
            // where the puck is or how it's moving, we know where it will
            // cross our defense line if it keeps its current velocity.
            float predictedX = _profile.predictionTime > 0f
                ? _predictor.PredictCrossingX(puckPos, puckVel, _profile.defenseLineY)
                : puckPos.x;

            // ---- 2. Continuous "danger" score (0 = totally safe, far on ----
            //          the player's side; 1 = puck right on our goal line).
            // This replaces the old binary puckOnBotSide / puckApproaching
            // checks. There's no point where danger "jumps" from 0 to 1 --
            // it's a smooth ramp, so there's no gap for the bot to fall
            // through.
            float danger = Mathf.InverseLerp(-_config.boardHalfHeight, _ownGoalY, puckPos.y);
            danger = Mathf.Clamp01(danger);

            // Puck moving toward us (up, +Y) makes it feel more dangerous
            // even a little early; moving away relaxes the bot smoothly.
            float approachFactor = Mathf.Clamp01(puckVel.y / 6f); // -1..1 roughly, clamped to 0..1 on the "coming at us" side
            danger = Mathf.Clamp01(danger + approachFactor * 0.25f);

            // ---- 3. Continuous Y target: hover near defense line when ----
            //          safe, push deep toward the puck when dangerous.
            // This single Lerp replaces Idle/Defend/Intercept entirely.
            float chaseDepth = Mathf.Lerp(0f, _profile.defenseLineY - 0.8f, _profile.aggression);
            float reactiveY = Mathf.Lerp(_benchY, _benchY - chaseDepth, danger);

            // ---- 4. If the puck is BEHIND us (between paddle and our ----
            //          own goal), we must get goal-side of it immediately,
            //          no matter what danger says. This was the old
            //          "Recover" state; here it's a continuous override
            //          blended in by how far behind the puck is, instead
            //          of a hard switch.
            float behindAmount = Mathf.Clamp01((puckPos.y - paddlePos.y) / 1.0f); // 0..1 over 1 unit of "behind-ness"
            float recoverY = Mathf.Min(puckPos.y + 0.5f, _ownGoalLimit);
            float targetY = Mathf.Lerp(reactiveY, recoverY, behindAmount);
            targetY = Mathf.Clamp(targetY, 0.3f, _ownGoalLimit);

            // ---- 5. X target: always track the puck/predicted X, with ----
            //          a steering offset so contact sends the puck toward
            //          the player's goal instead of straight back. The
            //          offset strength scales with danger+aggression
            //          instead of an on/off Attack state, and is clamped
            //          relative to the puck position BEFORE being added,
            //          so it can never produce an incoherent target (the
            //          bug from the previous Attack-state rewrite).
            Vector2 playerGoal = new Vector2(0f, -_config.boardHalfHeight);
            Vector2 dirToGoal = (playerGoal - puckPos).normalized;
            float strikeStrength = 0.4f * _profile.aggression;
            Vector2 steer = -dirToGoal * strikeStrength; // sit slightly on the far side from the goal

            // Clamp the steer offset's Y component so it can't push the
            // target past our own goal line even when the puck is right
            // up against it -- prevents the "stall near own goal" bug.
            float maxSteerY = _ownGoalLimit - puckPos.y;
            if (steer.y > maxSteerY) steer.y = maxSteerY;

            float targetX = Mathf.Lerp(predictedX, puckPos.x + steer.x, danger);
            // Always blend in at least a little pursuit of the puck's raw
            // X even at low danger, so the bot never looks "asleep" --
            // this is the key difference from the old Idle state, which
            // only followed puckPos.x * 0.5f and could look unresponsive.
            targetX = Mathf.Lerp(puckPos.x, targetX, 0.5f + 0.5f * danger);

            Vector2 target = new Vector2(targetX, targetY + steer.y * danger);
            target.y = Mathf.Clamp(target.y, 0.3f, _ownGoalLimit);

            // ---- 6. Aim error + mistakes (human imperfection), unchanged. ----
            float randomError = Random.Range(-_profile.aimError, _profile.aimError);
            target.x += randomError + _mistakeOffset;

            // Final X safety clamp so aim error/mistakes can never send the
            // chase target past the side walls.
            float maxX = _config.boardHalfWidth - 0.4f;
            target.x = Mathf.Clamp(target.x, -maxX, maxX);

            return target;
        }

        private void UpdateMistake(float dt)
        {
            if (_mistakeTimer > 0f)
            {
                _mistakeTimer -= dt;
                if (_mistakeTimer <= 0f) _mistakeOffset = 0f;
                return;
            }
            if (Random.value < _profile.mistakeChance * dt * 3f)
            {
                _mistakeOffset = Random.Range(-1.2f, 1.2f);
                _mistakeTimer = Random.Range(0.2f, 0.5f);
            }
        }
    }
}
