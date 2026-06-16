// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    // Decides the bot paddle's target point each decision tick.
    // The bot defends its own goal (top), intercepts incoming pucks,
    // attacks loose pucks on its side, and crucially NEVER pushes the puck
    // toward its own goal (no own-goals).
    public class BotBrain
    {
        private enum AIState { Idle, Defend, Intercept, Attack, Recover }

        private readonly DifficultyProfile _profile;
        private readonly GameConfig _config;
        private readonly PuckPredictor _predictor;

        // Bot defends the TOP goal. 'Own goal' is at +Y (top of board).
        private readonly float _ownGoalY;

        private float _reactionTimer;
        private Vector2 _cachedTarget;
        private float _mistakeOffset;
        private float _mistakeTimer;

        public BotBrain(DifficultyProfile profile, GameConfig config)
        {
            _profile = profile;
            _config = config;
            _predictor = new PuckPredictor(config.boardHalfWidth, 0.3f);
            _ownGoalY = config.boardHalfHeight; // top wall line
            _cachedTarget = new Vector2(0f, profile.defenseLineY);
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
            bool puckOnBotSide = puckPos.y > 0f;
            bool puckApproaching = puckVel.y > 0.1f; // moving up toward bot

            // Is the puck BEHIND the paddle (between paddle and own goal)?
            // On the top side, 'behind' means the puck's Y is greater than
            // the paddle's Y (closer to the top goal).
            bool puckBehindPaddle = puckPos.y > paddlePos.y + 0.05f && puckOnBotSide;

            AIState state;
            if (puckBehindPaddle)
                state = AIState.Recover;                 // get goal-side, clear it
            else if (puckOnBotSide && puckVel.y > -0.1f)
                state = AIState.Attack;                   // loose puck on our half
            else if (puckApproaching)
                state = AIState.Intercept;               // incoming, predict + meet
            else if (puckOnBotSide)
                state = AIState.Defend;                   // on our half, settle back
            else
                state = AIState.Idle;                     // puck on player's half

            Vector2 target;
            switch (state)
            {
                case AIState.Recover:
                {
                    // Puck is behind us. Move goal-side of the puck and to one
                    // side so the next push sends it sideways/down, never up
                    // into our own goal.
                    float clearX = puckPos.x + (puckPos.x >= 0f ? 0.6f : -0.6f);
                    float behindY = Mathf.Min(puckPos.y + 0.6f, _ownGoalY - 0.3f);
                    target = new Vector2(clearX, behindY);
                    break;
                }
                case AIState.Attack:
                {
                    // Approach the puck from ABOVE so contact drives it DOWN
                    // toward the player's goal at (0, -boardHalfHeight).
                    Vector2 playerGoal = new Vector2(0f, -_config.boardHalfHeight);
                    Vector2 dirToGoal = (playerGoal - puckPos).normalized;
                    // Strike point is on the far side of the puck from the goal,
                    // so pushing forward sends it goalward.
                    Vector2 strike = puckPos - dirToGoal * 0.45f;
                    Vector2 hold = new Vector2(puckPos.x, _profile.defenseLineY);
                    target = Vector2.Lerp(hold, strike, _profile.aggression);
                    break;
                }
                case AIState.Intercept:
                {
                    float x = _profile.predictionTime > 0f
                        ? _predictor.PredictCrossingX(puckPos, puckVel, _profile.defenseLineY)
                        : puckPos.x;
                    // Move DOWN a little to meet the puck earlier (Y movement!).
                    float meetY = Mathf.Lerp(_profile.defenseLineY,
                                             _profile.defenseLineY - 1.0f,
                                             _profile.aggression);
                    target = new Vector2(x, meetY);
                    break;
                }
                case AIState.Defend:
                {
                    // Track the puck's X but stay goal-side on the defense line.
                    target = new Vector2(puckPos.x, _profile.defenseLineY);
                    break;
                }
                default: // Idle
                {
                    // Recenter, but gently follow the puck's X so it's ready.
                    target = new Vector2(puckPos.x * 0.5f, _profile.defenseLineY);
                    break;
                }
            }

            // Safety clamp: never let the target pass the own goal line.
            target.y = Mathf.Clamp(target.y, 0.3f, _ownGoalY - 0.3f);

            // Aim error + mistakes (human imperfection).
            float randomError = Random.Range(-_profile.aimError, _profile.aimError);
            target.x += randomError + _mistakeOffset;
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