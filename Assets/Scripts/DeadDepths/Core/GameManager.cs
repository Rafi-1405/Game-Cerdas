using System;
using UnityEngine;
using DeadDepths.AI;
using DeadDepths.Audio;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;

namespace DeadDepths.Core
{
    /// <summary>
    /// Master game manager that owns level progression, run scoring, Retry/Continue flow, and DDA integration.
    /// ASD-STE100: This module manages the game loop and level progression.
    /// Follows Section 1.2, 4, and 8 of the specification.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public int StartingLevel = 1;
            public bool AutoGenerateOnStart = true;

            [Header("Scoring")]
            public int WalkerKillScore = 100;
            public int SpitterKillScore = 150;
            public int ChargerKillScore = 250;
            public int CrateOpenScore = 150;
            public int LevelClearBaseBonus = 500;
        }
        #endregion

        [SerializeField] private Config managerConfig = new Config();
        public Config ManagerConfig => managerConfig;

        public static GameManager Instance { get; private set; }

        public int CurrentLevel { get; private set; } = 1;
        public bool IsLevelActive { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;

        // Persistent Run Score & Statistics (preserved across Continue Run, reset on Retry)
        public int TotalScore { get; private set; } = 0;
        public int LevelScore { get; private set; } = 0;
        public int TotalZombiesKilled { get; private set; } = 0;
        public int LevelZombiesKilled { get; private set; } = 0;
        public int TotalCratesOpened { get; private set; } = 0;
        public int LevelCratesOpened { get; private set; } = 0;
        public int LevelsCleared { get; private set; } = 0;
        public int LastLevelBonus { get; private set; } = 0;
        public float LastLevelClearTime { get; private set; } = 0f;
        public float LastEvaluatedPerfScore { get; private set; } = 0.5f;
        public DifficultyTier LastEvaluatedTier { get; private set; } = DifficultyTier.Balanced;

        private WorldGenerator worldGen;
        private DifficultyController diffController;
        private PlayerPerformanceTracker perfTracker;

        public event Action<int> OnLevelStarted;
        public event Action<int> OnLevelCompleted;
        public event Action<int> OnScoreChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CurrentLevel = managerConfig.StartingLevel;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            worldGen = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();

            EnsureDebugModeExists();

            if (managerConfig.AutoGenerateOnStart)
            {
                StartLevel(CurrentLevel);
            }
        }

        private void EnsureDebugModeExists()
        {
            if (DeadDepthsDebugMode.Instance == null && FindAnyObjectByType<DeadDepthsDebugMode>() == null)
            {
                var uiRoot = GameObject.Find("UIManager");
                if (uiRoot != null)
                {
                    uiRoot.AddComponent<DeadDepthsDebugMode>();
                }
                else
                {
                    gameObject.AddComponent<DeadDepthsDebugMode>();
                }
            }
        }

        private void Update()
        {
            // Death restart shortcut [R]
            var player = DeadDepthsPlayerController.Instance;
            if (player != null && player.IsDead)
            {
                bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    restartPressed = UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
                }
#else
                restartPressed = Input.GetKeyDown(KeyCode.R);
#endif
                if (restartPressed)
                {
                    RetryRun();
                }
            }
        }

        /// <summary>
        /// Starts and generates a level using the current DDA configuration.
        /// Keeps TotalScore when advancing levels.
        /// </summary>
        public void StartLevel(int levelNumber)
        {
            CurrentLevel = levelNumber;
            IsLevelActive = true;
            LevelScore = 0;
            LevelZombiesKilled = 0;
            LevelCratesOpened = 0;
            LastLevelBonus = 0;
            Time.timeScale = 1f;

            var compUi = CompletionScreenUI.Instance ?? FindAnyObjectByType<CompletionScreenUI>();
            if (compUi != null)
            {
                compUi.Hide();
            }

            if (worldGen == null) worldGen = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();

            var profile = diffController != null ? diffController.CurrentProfile : new DifficultyController.LevelDifficultyProfile();

            if (worldGen != null)
            {
                worldGen.GenerateWorld(CurrentLevel, profile);
            }

            OnLevelStarted?.Invoke(CurrentLevel);
        }

        /// <summary>
        /// Called when Player clicks Retry on Death or chooses New Run: resets score to 0 and restarts cleanly.
        /// </summary>
        public void RetryRun()
        {
            TotalScore = 0;
            LevelScore = 0;
            TotalZombiesKilled = 0;
            LevelZombiesKilled = 0;
            TotalCratesOpened = 0;
            LevelCratesOpened = 0;
            LevelsCleared = 0;
            LastLevelBonus = 0;
            LastLevelClearTime = 0f;
            Time.timeScale = 1f;

            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (diffController != null)
            {
                diffController.UpdateProfile(DifficultyTier.Balanced);
            }

            OnScoreChanged?.Invoke(TotalScore);
            StartLevel(managerConfig.StartingLevel);
        }

        public void RecordZombieKill(ZombieArchetype arch)
        {
            int pts = managerConfig.WalkerKillScore;
            if (arch == ZombieArchetype.Spitter) pts = managerConfig.SpitterKillScore;
            else if (arch == ZombieArchetype.Charger) pts = managerConfig.ChargerKillScore;

            LevelZombiesKilled++;
            TotalZombiesKilled++;
            AddScore(pts);
        }

        public void RecordCrateOpened()
        {
            LevelCratesOpened++;
            TotalCratesOpened++;
            AddScore(managerConfig.CrateOpenScore);
        }

        public void AddScore(int points)
        {
            if (points <= 0) return;
            LevelScore += points;
            TotalScore += points;
            OnScoreChanged?.Invoke(TotalScore);
        }

        /// <summary>
        /// Called when the player extracts via helicopter in the safe zone.
        /// Evaluates DDA performance, awards level bonus, and shows Scoreboard.
        /// </summary>
        public void CompleteLevel()
        {
            if (!IsLevelActive) return;
            IsLevelActive = false;

            LevelsCleared++;
            LastLevelBonus = managerConfig.LevelClearBaseBonus * Mathf.Max(1, CurrentLevel);
            AddScore(LastLevelBonus);

            if (perfTracker == null) perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();
            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();

            LastLevelClearTime = perfTracker != null ? perfTracker.ElapsedLevelTime : 0f;

            if (perfTracker != null && diffController != null)
            {
                LastEvaluatedPerfScore = PerformanceEvaluator.EvaluatePerformance(perfTracker, out DifficultyTier tier);
                LastEvaluatedTier = tier;
                diffController.UpdateProfile(tier);
                Debug.Log($"[GameManager] Level {CurrentLevel} Cleared! Score: {TotalScore}, Perf: {LastEvaluatedPerfScore:F2}, Next Tier: {tier}");
            }

            OnLevelCompleted?.Invoke(CurrentLevel);

            var compUi = CompletionScreenUI.Instance ?? FindAnyObjectByType<CompletionScreenUI>();
            if (compUi != null)
            {
                compUi.Show(CurrentLevel);
            }
        }

        /// <summary>
        /// Advances to the next level when Continue Run is selected on the Scoreboard.
        /// Regenerates a new random cluster map while keeping the accumulated TotalScore.
        /// </summary>
        public void ProceedToNextLevel()
        {
            CurrentLevel++;
            StartLevel(CurrentLevel);
        }
    }
}
