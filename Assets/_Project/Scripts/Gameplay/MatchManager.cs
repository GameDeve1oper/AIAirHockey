// MatchManager.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Orchestrates a single match in the Gameplay scene.
    public class MatchManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;

        [Header("Scene References")]
        [SerializeField] private Puck _puck;
        [SerializeField] private PlayerPaddle _playerPaddle;
        [SerializeField] private BotPaddle _botPaddle;      // used in PvBot
        [SerializeField] private PlayerPaddle _player2Paddle; // used in PvP (top)
        [SerializeField] private Goal _goalTop;
        [SerializeField] private Goal _goalBottom;
        [SerializeField] private CountdownController _countdown;

        [Header("Effects (assigned later)")]
        [SerializeField] private GameObject _goalParticlePrefab;
        [SerializeField] private GameObject _puckSpawnPrefab;

        private ScoreSystem _score;
        private MatchState _state = MatchState.Idle;
        public MatchState State => _state;

        private void OnEnable()
        {
            EventBus.OnGoalScored += HandleGoalScored;
        }

        private void OnDisable()
        {
            EventBus.OnGoalScored -= HandleGoalScored;
        }

        private void Start()
        {
            _score = new ScoreSystem();
            _score.Reset();
            SetupForMode();
            AudioManager.Instance.PlayGameMusic();
            StartRound(firstServeToTop: Random.value > 0.5f);
        }

        // Enable the correct top paddle based on selected mode.
               private void SetupForMode()
        {
            if (GameManager.Instance == null)
            { Debug.LogError("SetupForMode: GameManager.Instance is NULL -> start from Bootstrap scene"); return; }

            bool vsBot = GameManager.Instance.SelectedMode == GameMode.PlayerVsBot;
            Debug.Log("SetupForMode: mode=" + GameManager.Instance.SelectedMode +
                      "  botPaddle assigned=" + (_botPaddle != null));

            if (_botPaddle != null) _botPaddle.gameObject.SetActive(vsBot);
            if (_player2Paddle != null) _player2Paddle.gameObject.SetActive(!vsBot);
            if (vsBot && _botPaddle != null)
                _botPaddle.Configure(GameManager.Instance.SelectedDifficulty);
        }


        // Begin a round: freeze puck, countdown, then launch.
        private void StartRound(bool firstServeToTop)
        {
            SetState(MatchState.Countdown);
            _puck.ResetPuck(Vector2.zero, Vector2.zero);
            _puck.Freeze();
            _goalTop.Arm();
            _goalBottom.Arm();

            StartCoroutine(_countdown.Run(() =>
            {
                _puck.Unfreeze();
                Vector2 dir = firstServeToTop ? Vector2.up : Vector2.down;
                _puck.ResetPuck(Vector2.zero, dir);
                if (_puckSpawnPrefab != null && PoolManager.Exists)
                    PoolManager.Instance.SpawnTimed(_puckSpawnPrefab, Vector2.zero, Quaternion.identity, 0.6f);
                SetState(MatchState.Playing);
            }));
        }

        private void HandleGoalScored(PlayerSide concededSide)
        {
            if (_state != MatchState.Playing) return;
            SetState(MatchState.GoalScored);

            _score.AwardForConceded(concededSide);
            AudioManager.Instance.Play(SoundId.Goal);

            // Goal particle at the conceding goal.
            Vector3 fxPos = concededSide == PlayerSide.Top
                ? _goalTop.transform.position
                : _goalBottom.transform.position;
            if (_goalParticlePrefab != null && PoolManager.Exists)
                PoolManager.Instance.SpawnTimed(_goalParticlePrefab, fxPos, Quaternion.identity, 1.5f);

            // Update stats.
            var data = SaveManager.Instance.Data;
            if (concededSide == PlayerSide.Top) data.goalsScored++;
            else data.goalsConceded++;
            SaveManager.Instance.Save();

            // Check for winner, else next round.
            if (_score.HasWinner(_config.goalsToWin, out PlayerSide winner))
                StartCoroutine(FinishMatch(winner));
            else
                StartCoroutine(NextRoundAfterDelay(concededSide));
        }

        private IEnumerator NextRoundAfterDelay(PlayerSide concededSide)
        {
            yield return new WaitForSeconds(_config.goalResetDelay);
            // Serve toward whoever just conceded (give them the puck).
            bool serveToTop = concededSide == PlayerSide.Top;
            StartRound(serveToTop);
        }

        private IEnumerator FinishMatch(PlayerSide winner)
        {
            yield return new WaitForSeconds(_config.goalResetDelay);
            SetState(MatchState.Finished);
            _puck.Freeze();

            // Stats: matches played/won.
            var data = SaveManager.Instance.Data;
            data.matchesPlayed++;
            bool playerWon = winner == PlayerSide.Bottom;
            if (playerWon) data.matchesWon++;
            SaveManager.Instance.Save();

            AudioManager.Instance.Play(playerWon ? SoundId.Victory : SoundId.Defeat);
            EventBus.RaiseMatchFinished(winner);
        }

        // --- Pause / resume / restart, called by UI ---
        public void PauseMatch()
        {
            if (_state == MatchState.Finished) return;
            Time.timeScale = 0f;
            SetState(MatchState.Paused);
        }

        public void ResumeMatch()
        {
            if (_state != MatchState.Paused) return;
            Time.timeScale = 1f;
            SetState(MatchState.Playing);
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            EventBus.ClearGameplayEvents();
            SceneLoader.Instance.Load(SceneLoader.Gameplay);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            EventBus.ClearGameplayEvents();
            SceneLoader.Instance.Load(SceneLoader.MainMenu);
        }

        private void SetState(MatchState s)
        {
            _state = s;
            EventBus.RaiseMatchStateChanged(s);
        }
    }
}