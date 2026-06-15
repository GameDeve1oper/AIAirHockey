// ScoreSystem.cs
namespace AIAirHockey
{
    // Pure data holder for the current match score. No MonoBehaviour
    // needed; MatchManager owns one instance.
    public class ScoreSystem
    {
        public int BottomScore { get; private set; }
        public int TopScore { get; private set; }

        public void Reset()
        {
            BottomScore = 0;
            TopScore = 0;
            EventBus.RaiseScoreChanged(BottomScore, TopScore);
        }

        // The defender side was scored on, so award the other side.
        public void AwardForConceded(PlayerSide concededSide)
        {
            if (concededSide == PlayerSide.Top) BottomScore++;
            else TopScore++;
            EventBus.RaiseScoreChanged(BottomScore, TopScore);
        }

        public bool HasWinner(int goalsToWin, out PlayerSide winner)
        {
            if (BottomScore >= goalsToWin) { winner = PlayerSide.Bottom; return true; }
            if (TopScore >= goalsToWin) { winner = PlayerSide.Top; return true; }
            winner = PlayerSide.Bottom;
            return false;
        }
    }
}