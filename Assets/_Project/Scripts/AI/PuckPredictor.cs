// PuckPredictor.cs
using UnityEngine;

namespace AIAirHockey
{
    // Pure math helper. Predicts puck motion, reflecting off the left/right
    // side walls (mirror fold, so multiple bounces are handled). Vertical
    // bounces are ignored on purpose: the bot reasons about lateral
    // interception on its guard line.
    //
    // Drag is intentionally not modelled: the puck's min-speed clamp (see
    // GameConfig.puckMinSpeedAfterHit) keeps it lively, so over the short
    // prediction horizon a straight-line projection stays accurate while
    // costing almost nothing -- which matters on mobile.
    public class PuckPredictor
    {
        private readonly float _halfWidth;
        private readonly float _puckRadius;

        public PuckPredictor(float boardHalfWidth, float puckRadius)
        {
            _halfWidth = boardHalfWidth;
            _puckRadius = puckRadius;
        }

        // Puck position 'time' seconds ahead, with x reflected across the side
        // walls. Mathf.PingPong gives the triangle wave that folds any x back
        // into the [-limit, limit] play band (handles repeated bounces).
        public Vector2 Predict(Vector2 position, Vector2 velocity, float time)
        {
            Vector2 p = position + velocity * time;

            float limit = _halfWidth - _puckRadius; // play half-width for puck center
            if (limit <= 0f) return p;

            float range = 2f * limit;
            p.x = Mathf.PingPong(p.x + limit, range) - limit;
            return p;
        }

        // Where will the puck cross the horizontal line at 'targetY'? Returns
        // the folded x at that crossing. 'maxTime' caps how far ahead the bot
        // is allowed to foresee (ties anticipation to difficulty): beyond it,
        // the bot only leads as far as it can see.
        public float PredictCrossingX(Vector2 position, Vector2 velocity, float targetY, float maxTime)
        {
            // Near-horizontal travel: no meaningful crossing, lead laterally.
            if (Mathf.Abs(velocity.y) < 0.01f)
                return Predict(position, velocity, maxTime).x;

            float t = (targetY - position.y) / velocity.y;
            if (t < 0f) return position.x;        // moving away from the line
            if (t > maxTime) t = maxTime;          // clamp to the foresight horizon
            return Predict(position, velocity, t).x;
        }
    }
}
