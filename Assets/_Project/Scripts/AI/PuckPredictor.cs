// PuckPredictor.cs
using UnityEngine;

namespace AIAirHockey
{
    // Pure math helper: predicts the puck position 'time' seconds ahead,
    // reflecting off the left/right side walls. Vertical bounces are
    // ignored on purpose (the bot cares about lateral interception).
    public class PuckPredictor
    {
        private readonly float _halfWidth;
        private readonly float _puckRadius;

        public PuckPredictor(float boardHalfWidth, float puckRadius)
        {
            _halfWidth = boardHalfWidth;
            _puckRadius = puckRadius;
        }

        public Vector2 Predict(Vector2 position, Vector2 velocity, float time)
        {
            // Straight-line projection.
            Vector2 p = position + velocity * time;

            // Reflect X across side walls so the prediction respects bounces.
            float limit = _halfWidth - _puckRadius;
            // Mirror-fold p.x into [-limit, limit].
            float range = 2f * limit;
            if (range <= 0f) return p;
            float shifted = p.x + limit;                 // shift to [0, 2*limit]
            float modded = Mathf.Repeat(shifted, range); // wrap
            // Triangle wave gives the reflected position.
            float folded = Mathf.PingPong(p.x + limit, range) ; 
            p.x = folded - limit;
            return p;
        }

        // Predict where the puck crosses a given Y line (the bot's defense
        // line). Returns the X at that crossing, or current X if it never
        // crosses (moving away).
        public float PredictCrossingX(Vector2 position, Vector2 velocity, float targetY)
        {
            if (Mathf.Abs(velocity.y) < 0.01f) return position.x;
            float t = (targetY - position.y) / velocity.y;
            if (t < 0f) return position.x; // puck moving away from line
            Vector2 at = Predict(position, velocity, t);
            return at.x;
        }
    }
}