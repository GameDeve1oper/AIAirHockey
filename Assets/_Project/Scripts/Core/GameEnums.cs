// GameEnums.cs
namespace AIAirHockey
{
    // Which mode the player picked in the menu.
    public enum GameMode
    {
        PlayerVsBot,
        PlayerVsPlayer
        // Future: Online, Story, Challenge (reserved, do not reorder)
    }

    // AI difficulty. Order matters for UI; do not reorder.
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        HumanLike
    }

    // High-level match lifecycle, used by MatchManager.
    public enum MatchState
    {
        Idle,        // before anything starts
        Countdown,   // 3-2-1-GO
        Playing,     // puck live
        GoalScored,  // brief pause after a goal
        Paused,      // user paused
        Finished     // someone won the match
    }

    // Which side a goal/paddle belongs to.
    public enum PlayerSide
    {
        Bottom, // human player
        Top     // bot or player 2
    }

    // Power-up types available during gameplay.
    public enum PowerUpType
    {
        SpeedPuck,
        TitanPaddle,
        GoalShield,
        TimeWarp,
        MultiPuck
    }

    // Every sound effect id. AudioManager maps these to clips.
    public enum SoundId
    {
        ButtonClick,
        PaddleHit,
        WallHit,
        Goal,
        Countdown,
        CountdownGo,
        Victory,
        Defeat
    }
}