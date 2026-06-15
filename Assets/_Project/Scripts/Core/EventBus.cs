// EventBus.cs
using System;

namespace AIAirHockey
{
    // Central event hub. Systems raise events here; other systems subscribe.
    // This keeps gameplay, UI, audio, and feel decoupled.
    public static class EventBus
    {
        // A goal was scored. Argument: the side that was scored ON (conceded).
        public static event Action<PlayerSide> OnGoalScored;

        // Score changed. Arguments: bottomScore, topScore.
        public static event Action<int, int> OnScoreChanged;

        // Match state changed (countdown, playing, finished, etc).
        public static event Action<MatchState> OnMatchStateChanged;

        // Match finished. Argument: the winning side.
        public static event Action<PlayerSide> OnMatchFinished;

        // Puck hit something. Argument: world position of the impact.
        public static event Action<UnityEngine.Vector2> OnPuckImpact;

        // Countdown tick. Argument: current number (3,2,1) or 0 for GO.
        public static event Action<int> OnCountdownTick;

        // --- Raise methods (call these to fire an event) ---
        public static void RaiseGoalScored(PlayerSide concededSide) => OnGoalScored?.Invoke(concededSide);
        public static void RaiseScoreChanged(int bottom, int top) => OnScoreChanged?.Invoke(bottom, top);
        public static void RaiseMatchStateChanged(MatchState state) => OnMatchStateChanged?.Invoke(state);
        public static void RaiseMatchFinished(PlayerSide winner) => OnMatchFinished?.Invoke(winner);
        public static void RaisePuckImpact(UnityEngine.Vector2 pos) => OnPuckImpact?.Invoke(pos);
        public static void RaiseCountdownTick(int n) => OnCountdownTick?.Invoke(n);

        // Clears all subscribers. Call when leaving Gameplay so old
        // listeners from a previous match don't linger.
        public static void ClearGameplayEvents()
        {
            OnGoalScored = null;
            OnScoreChanged = null;
            OnMatchStateChanged = null;
            OnMatchFinished = null;
            OnPuckImpact = null;
            OnCountdownTick = null;
        }
    }
}