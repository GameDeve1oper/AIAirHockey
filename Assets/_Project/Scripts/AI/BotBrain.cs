// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    // Decides the bot paddle's target point each decision tick.
    // Internal states model attack/defense behavior. Difficulty profile
    // drives all the numbers, so the four difficulties differ only by data.
    public class BotBrain
    {
        private enum AIState { Idle, Defend, Intercept, Attack }

        private readonly DifficultyProfile _profile;
        private readonly GameConfig _config;
        private readonly PuckPredictor _predictor;

        private AIState _state = AIState.Idle;
        private float _reactionTimer;       // counts down reaction delay
        private Vector2 _cachedTarget;      // target we commit to during delay
        private float _mistakeOffset;       // current deliberate error in X
        private float _mistakeTimer;        // how long the mistake lasts

        public BotBrain(DifficultyProfile profile, GameConfig config)
        {
            _profile = profile;
            _config = config;
            _predictor = new PuckPredictor(config.boardHalfWidth, 0.3f);
            _cachedTarget = new Vector2(0f, profile.defenseLineY);
        }

        // Called every FixedUpdate by BotPaddle. Returns where to move.
        public Vector2 Decide(Vector2 paddlePos, Vector2 puckPos, Vector2 puckVel, float dt)
        {
            UpdateMistake(dt);

            // Reaction delay: only re-decide after the timer elapses. Between
            // decisions the bot keeps moving to the last committed target,
            // which is exactly how a delayed human reaction feels.
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
            bool puckOnBotSide = puckPos.y > 0f;
            bool puckApproaching = puckVel.y > 0.1f;

            // State selection.
            if (puckOnBotSide && puckVel.y > -0.1f)
                _state = AIState.Attack;
            else if (puckApproaching)
                _state = AIState.Intercept;
            else if (puckOnBotSide)
                _state = AIState.Defend;
            else
                _state = AIState.Idle;

            Vector2 target;
            switch (_state)
            {
                case AIState.Attack:
                {
                    // Move onto the puck and a touch behind it (toward center)
                    // so the hit drives it down toward the player's goal.
                    Vector2 behind = puckPos + new Vector2(0f, 0.5f);
                    // Aggression pulls the bot further forward to strike.
                    target = Vector2.Lerp(new Vector2(puckPos.x, _profile.defenseLineY),
                                          behind, _profile.aggression);
                    break;
                }
                case AIState.Intercept:
                {
                    // Predict where puck crosses the defense line and meet it.
                    float x = _profile.predictionTime > 0f
                        ? _predictor.PredictCrossingX(puckPos, puckVel, _profile.defenseLineY)
                        : puckPos.x; // Easy: no prediction
                    target = new Vector2(x, _profile.defenseLineY);
                    break;
                }
                case AIState.Defend:
                {
                    // Stay between puck and own goal, on the defense line.
                    target = new Vector2(puckPos.x, _profile.defenseLineY);
                    break;
                }
                default: // Idle
                {
                    // Recenter on the defense line.
                    target = new Vector2(0f, _profile.defenseLineY);
                    break;
                }
            }

            // Apply aim error (random wobble) + any active mistake offset.
            float randomError = Random.Range(-_profile.aimError, _profile.aimError);
            target.x += randomError + _mistakeOffset;
            return target;
        }

        // Occasionally commit a deliberate mistake: a big wrong-direction
        // offset for a short time. This is the 'human imperfection' system.
        private void UpdateMistake(float dt)
        {
            if (_mistakeTimer > 0f)
            {
                _mistakeTimer -= dt;
                if (_mistakeTimer <= 0f) _mistakeOffset = 0f;
                return;
            }
            // Roll for a new mistake (scaled so it's per-second-ish).
            if (Random.value < _profile.mistakeChance * dt * 3f)
            {
                _mistakeOffset = Random.Range(-1.2f, 1.2f);
                _mistakeTimer = Random.Range(0.2f, 0.5f);
            }
        }
    }
}