// BotBrain.cs
using UnityEngine;

namespace AIAirHockey
{
    // Pure decision logic for the bot paddle. Given the current paddle and
    // puck positions, returns the world-space point BotPaddle should move
    // toward this physics step.
    //
    // Deliberately simple: four states, decided purely from *current*
    // positions. No bounce prediction, no reaction lag, no aim error.
    // Difficulty only ever changes how fast the paddle can move
    // (see DifficultyProfile / BotPaddle) -- never what it decides to do.
    //
    // States, checked in this priority order every step:
    //
    //   RECOVER  the puck slipped in BEHIND us (closer to our own goal
    //            than we are). Pushing straight at it now would shove it
    //            further toward our own net, so we step around it and
    //            tuck in behind it first, then hand off to FOLLOW to
    //            push it back out.
    //   CORNER   puck and paddle are jammed together near the same side
    //            wall, deep in our half. Pinning it flat against the wall
    //            doesn't lead anywhere, so we slide to the inside of the
    //            puck (toward center) to open a clean angle, then FOLLOW
    //            takes over and strikes it out.
    //   FOLLOW   normal case: puck is on our side and not in trouble.
    //            Chase it directly -- full commit, anywhere on our half.
    //   GUARD    puck is on the player's side. Wander randomly around our
    //            own half (NOT tracking the puck's x -- that looked too
    //            robotic) so we look relaxed, but react instantly the
    //            moment the puck crosses back onto our side.
    public enum AIState { Guard, Follow, Recover, Corner }

    public class BotBrain
    {
        // --- tuning constants -----------------------------------------
        // These describe HOW the bot behaves and are shared by every
        // difficulty tier (only moveSpeed differs between tiers). Most
        // are expressed relative to board/puck size so they scale
        // automatically if those are ever changed in GameConfig.
        private const float SideHysteresis      = 0.05f; // stops state flicker right at the center line
        private const float RecoverMargin       = 0.15f; // puck must be this far past us to count as "behind"
        private const float RecoverBehindMult   = 3f;    // how far behind the puck to tuck in, x puck radius
        private const float RecoverDodgeMult    = 3f;    // sideways step on the way in, so we go AROUND the puck
        private const float CornerJamMult       = 3f;    // paddle-puck distance below which we call it "jammed", x puck radius
        private const float CornerEscapeMult    = 5f;    // sideways step to open an angle in a corner, x puck radius
        private const float CornerBehindMult    = 1f;    // slight behind-offset while escaping a corner, x puck radius
        private const float CornerWallFraction  = 0.3f;  // last 30% of the half-width counts as "near the wall"
        private const float CornerDepthFraction = 0.35f; // must be at least this far into our half to count as a corner

        // FOLLOW offset: in FOLLOW state, target this distance BEHIND (upward,
        // toward our own goal) from the puck instead of directly on it. This
        // lets the paddle hit cleanly and overshoots naturally past the puck
        // instead of getting stuck pinning it and bouncing it repeatedly.
        private const float FollowBehindMult    = 1.0f;  // x paddle radius, offset toward own goal

        // Idle wander, used by GUARD instead of mirroring the puck's x --
        // looks relaxed/human rather than robotically tracking the player.
        private const float WanderIntervalMin   = 0.5f;  // seconds between picking a new wander point
        private const float WanderIntervalMax   = 1f;
        private const float WanderYMinFraction  = 0.1f;  // wander roams across most of our half...
        private const float WanderYMaxFraction  = 0.85f; // ...but stops short of the goal mouth

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

        // Idle wander state (see Guard()).
        private Vector2 _wanderTarget;
        private float _nextWanderTime = -1f;

        public BotBrain(GameConfig config)
        {
            _halfWidth    = config != null ? config.boardHalfWidth  : 2.6f;
            _halfHeight   = config != null ? config.boardHalfHeight : 4.8f;
            _puckRadius   = config != null ? config.puckRadius      : 0.3f;
            _paddleMargin = 0.35f; // mirrors Paddle.ClampToHalf's margin

            _cornerWallBand = _halfWidth  * CornerWallFraction;
            _cornerDepth    = _halfHeight * CornerDepthFraction;
            _reachX         = _halfWidth  - _paddleMargin;
            _maxY           = _halfHeight - _paddleMargin;
        }

        public Vector2 Decide(Vector2 paddlePos, Vector2 puckPos)
        {
            // Small hysteresis around the center line so a puck resting
            // right on y=0 doesn't make the bot flicker between states.
            bool onBotSide = _state == AIState.Guard
                ? puckPos.y > SideHysteresis
                : puckPos.y > -SideHysteresis;

            if (!onBotSide)
            {
                _state = AIState.Guard;
                return Guard();
            }

            if (IsBehind(paddlePos, puckPos))
            {
                _state = AIState.Recover;
                return Recover(paddlePos, puckPos);
            }

            if (IsCornerJam(paddlePos, puckPos))
            {
                _state = AIState.Corner;
                return CornerEscape(puckPos);
            }

            _state = AIState.Follow;
            return Follow(puckPos); // hit and pull back, not camp on the puck
        }

        // GUARD: idle wander. Deliberately NOT tracking puck.x -- that
        // looked like the bot was robotically mirroring the player. Picks
        // a new random point across most of our half every ~0.5-1s. The
        // moment the puck crosses back onto our side, Decide() stops
        // calling this at all and reacts immediately on the very next
        // physics step -- there's no special "abort" needed here.
        private Vector2 Guard()
        {
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

        // FOLLOW: chase the puck, but target a point BEHIND it (toward our own
        // goal) instead of its exact center. This lets us hit it cleanly and
        // naturally overshoot past it instead of camping on it and bouncing
        // it repeatedly. Full commit -- anywhere on our half.
        private Vector2 Follow(Vector2 puckPos)
        {
            // Offset upward (toward our own goal at +Y) so we stay behind
            // the puck and separate naturally after impact.
            float offset = _puckRadius * FollowBehindMult;
            float y = Mathf.Min(puckPos.y + offset, _maxY);

            // Still chase the x, but the y offset pulls us back.
            return new Vector2(puckPos.x, y);
        }

        // Called by BotPaddle whenever a round isn't actively playing
        // (countdown, goal pause, etc.) so we never carry a stale
        // Follow/Recover/Corner state -- and its leftover target -- into
        // the next round's frozen puck position.
        public void ResetToGuard()
        {
            _state = AIState.Guard;
            _nextWanderTime = -1f; // forces a fresh wander point next time Guard() runs
        }

        // RECOVER -----------------------------------------------------
        private bool IsBehind(Vector2 paddlePos, Vector2 puckPos)
        {
            return puckPos.y > paddlePos.y + RecoverMargin;
        }

        private Vector2 Recover(Vector2 paddlePos, Vector2 puckPos)
        {
            // Step around the puck rather than straight through it: dodge
            // to whichever side we're already closer to, so we don't ram
            // it from below on the way to tucking in behind it.
            float dodgeSide = paddlePos.x <= puckPos.x ? -1f : 1f;
            float x = puckPos.x + dodgeSide * (_puckRadius * RecoverDodgeMult);
            x = Mathf.Clamp(x, -_reachX, _reachX);

            float y = Mathf.Min(puckPos.y + _puckRadius * RecoverBehindMult, _maxY);
            return new Vector2(x, y);
        }

        // CORNER --------------------------------------------------------
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
            // Slide to the inside of the puck (toward center, away from
            // the wall) to open a hitting angle, with a slight behind-
            // offset so the eventual strike has some forward push to it
            // instead of being a flat sideways shove.
            float wallSign = puckPos.x >= 0f ? 1f : -1f;
            float x = puckPos.x - wallSign * (_puckRadius * CornerEscapeMult);
            x = Mathf.Clamp(x, -_reachX, _reachX);

            float y = Mathf.Min(puckPos.y + _puckRadius * CornerBehindMult, _maxY);
            return new Vector2(x, y);
        }
    }
}