using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadDepths.Audio;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;

namespace DeadDepths.Core
{
    /// <summary>
    /// Master game manager that owns level progression, game flow, and DDA integration.
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
        }
        #endregion

        [SerializeField] private Config managerConfig = new Config();
        public Config ManagerConfig => managerConfig;

        public static GameManager Instance { get; private set; }

        public int CurrentLevel { get; private set; } = 1;
        public bool IsLevelActive { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;

        private WorldGenerator worldGen;
        private DifficultyController diffController;
        private PlayerPerformanceTracker perfTracker;

        public event Action<int> OnLevelStarted;
        public event Action<int> OnLevelCompleted;

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

        private void Start()
        {
            worldGen = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();

            if (managerConfig.AutoGenerateOnStart)
            {
                StartLevel(CurrentLevel);
            }
        }

        private void Update()
        {
            // Death restart prompt
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
                    Time.timeScale = 1f;
                    StartLevel(CurrentLevel);
                }
            }
        }

        /// <summary>
        /// Starts and generates a level using the current DDA configuration.
        /// </summary>
        public void StartLevel(int levelNumber)
        {
            CurrentLevel = levelNumber;
            IsLevelActive = true;
            Time.timeScale = 1f;

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
        /// Called when the player extracts via helicopter in the safe zone.
        /// </summary>
        public void CompleteLevel()
        {
            if (!IsLevelActive) return;
            IsLevelActive = false;

            OnLevelCompleted?.Invoke(CurrentLevel);

            // Display exact completion screen: Supplies Collection X Cleared
            if (CompletionScreenUI.Instance != null)
            {
                CompletionScreenUI.Instance.Show(CurrentLevel);
            }
        }

        /// <summary>
        /// Advances to the next level when Continue is selected on the completion screen.
        /// </summary>
        public void ProceedToNextLevel()
        {
            // 1. Evaluate player performance
            if (perfTracker != null && diffController != null)
            {
                float score = PerformanceEvaluator.EvaluatePerformance(perfTracker, out DifficultyTier tier);
                diffController.UpdateProfile(tier);
                Debug.Log($"[GameManager] Performance score: {score:F2}, New Tier: {tier}");
            }

            // 2. Advance level number
            CurrentLevel++;

            // 3. Generate next level
            StartLevel(CurrentLevel);
        }
    }
}
