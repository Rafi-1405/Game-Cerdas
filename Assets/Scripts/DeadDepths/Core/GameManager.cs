using System;
using UnityEngine;
using DeadDepths.AI;
using DeadDepths.Audio;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;
using UTS.Audio;

namespace DeadDepths.Core
{
    public enum BeltItemType
    {
        Bandage = 0,
        Medkit = 1,
        Adrenaline = 2
    }

    /// <summary>
    /// Master game manager that owns Main Menu diorama isolation, Pause flow, level progression,
    /// run scoring, Supply Intel objectives, Quick-Use Belt inventory, Retry/Continue flow, and DDA integration.
    /// ASD-STE100: This module manages the game loop and level progression.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public int StartingLevel = 1;
            public bool AutoGenerateOnStart = true;

            [Header("Objectives & Belt")]
            public int RequiredSupplyIntel = 3;
            public float BandageHeal = 20f;
            public float MedkitHeal = 50f;
            public float AdrenalineStamina = 40f;
            public float AdrenalineDuration = 6.0f;

            [Header("Scoring")]
            public int WalkerKillScore = 100;
            public int SpitterKillScore = 150;
            public int ChargerKillScore = 250;
            public int CrateOpenScore = 150;
            public int LevelClearBaseBonus = 500;
        }
        #endregion

        public const string PrefHighScore = "DeadDepths_HighScore";
        public const string PrefBestSector = "DeadDepths_BestSector";

        [SerializeField] private Config managerConfig = new Config();
        public Config ManagerConfig => managerConfig;

        public static GameManager Instance { get; private set; }

        public int CurrentLevel { get; private set; } = 1;
        public bool IsLevelActive { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;
        public bool IsInMainMenu { get; private set; } = false;

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

        // High Score & Best Sector (PlayerPrefs)
        public int HighScore { get; private set; } = 0;
        public int BestSector { get; private set; } = 1;
        public bool IsNewHighScore { get; private set; } = false;

        // Sector Supply Intel Objective (3/3 required to extract in SafeZoneController)
        public int SupplyIntelCollected { get; private set; } = 0;
        public int RequiredSupplyIntel => Mathf.Max(1, managerConfig != null ? managerConfig.RequiredSupplyIntel : 3);
        public bool HasRequiredSupplyIntel => SupplyIntelCollected >= RequiredSupplyIntel;

        // 3-Slot Quick-Use Belt ([1] Bandage, [2] Medkit, [3] Adrenaline)
        public int BandageCount { get; private set; } = 1;
        public int MedkitCount { get; private set; } = 0;
        public int AdrenalineCount { get; private set; } = 0;
        public float AdrenalineBuffRemaining { get; private set; } = 0f;
        public bool IsAdrenalineActive => AdrenalineBuffRemaining > 0f;

        private readonly float[] beltCooldownRemaining = new float[3];
        private readonly float[] beltCooldownDuration = { 1.0f, 1.5f, 8.0f };

        private WorldGenerator worldGen;
        private DifficultyController diffController;
        private PlayerPerformanceTracker perfTracker;

        public event Action<int> OnLevelStarted;
        public event Action<int> OnLevelCompleted;
        public event Action<int> OnScoreChanged;
        public event Action<bool> OnMainMenuChanged;
        public event Action<bool> OnPauseChanged;
        public event Action OnObjectiveUpdated;
        public event Action OnBeltUpdated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

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

            ApplyFallbackDefaults();
            CurrentLevel = managerConfig.StartingLevel;
            LoadPersistentRecords();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (managerConfig == null) managerConfig = new Config();
            if (managerConfig.StartingLevel <= 0) managerConfig.StartingLevel = 1;
            if (managerConfig.RequiredSupplyIntel <= 0) managerConfig.RequiredSupplyIntel = 3;
            if (managerConfig.BandageHeal <= 0f) managerConfig.BandageHeal = 20f;
            if (managerConfig.MedkitHeal <= 0f) managerConfig.MedkitHeal = 50f;
            if (managerConfig.AdrenalineStamina <= 0f) managerConfig.AdrenalineStamina = 40f;
            if (managerConfig.AdrenalineDuration <= 0f) managerConfig.AdrenalineDuration = 6.0f;
        }

        private void LoadPersistentRecords()
        {
            HighScore = PlayerPrefs.GetInt(PrefHighScore, 0);
            BestSector = Mathf.Max(1, PlayerPrefs.GetInt(PrefBestSector, 1));
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
            EnsureVfxManagerExists();

            if (managerConfig.AutoGenerateOnStart)
            {
                // Bypass Main Menu in BatchMode so automated tests start directly in Sector 1
                if (Application.isBatchMode)
                {
                    StartLevel(CurrentLevel);
                }
                else
                {
                    EnterMainMenu();
                }
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

        private void EnsureVfxManagerExists()
        {
            if (CombatVfxManager.Instance == null && FindAnyObjectByType<CombatVfxManager>() == null)
            {
                gameObject.AddComponent<CombatVfxManager>();
            }
        }

        private void Update()
        {
            if (!IsInMainMenu && !IsPaused && IsLevelActive)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (beltCooldownRemaining[i] > 0f)
                    {
                        beltCooldownRemaining[i] = Mathf.Max(0f, beltCooldownRemaining[i] - Time.deltaTime);
                    }
                }

                if (AdrenalineBuffRemaining > 0f)
                {
                    AdrenalineBuffRemaining = Mathf.Max(0f, AdrenalineBuffRemaining - Time.deltaTime);
                    if (AdrenalineBuffRemaining <= 0f)
                    {
                        OnBeltUpdated?.Invoke();
                    }
                }
            }

            // Death restart shortcut [R]
            var player = DeadDepthsPlayerController.Instance;
            if (!IsInMainMenu && player != null && player.IsDead)
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

        public void EnterMainMenu()
        {
            if (Instance == null) Instance = this;
            IsInMainMenu = true;
            IsLevelActive = false;
            IsPaused = false;
            Time.timeScale = 1f;

            TotalScore = 0;
            LevelScore = 0;
            TotalZombiesKilled = 0;
            LevelZombiesKilled = 0;
            TotalCratesOpened = 0;
            LevelCratesOpened = 0;
            LevelsCleared = 0;
            LastLevelBonus = 0;
            LastLevelClearTime = 0f;
            SupplyIntelCollected = 0;
            IsNewHighScore = false;
            ResetBeltForNewRun();

            var compUi = CompletionScreenUI.Instance ?? FindAnyObjectByType<CompletionScreenUI>();
            if (compUi != null) compUi.Hide();

            if (worldGen == null) worldGen = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (perfTracker == null) perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();

            var profile = diffController != null
                ? diffController.UpdateProfile(DifficultyTier.Balanced)
                : new DifficultyController.LevelDifficultyProfile();

            CurrentLevel = managerConfig.StartingLevel;
            if (worldGen != null)
            {
                worldGen.GenerateWorld(CurrentLevel, profile);
            }

            if (perfTracker != null)
            {
                perfTracker.SetTimePaused(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnPauseChanged?.Invoke(false);
            OnMainMenuChanged?.Invoke(true);
            OnObjectiveUpdated?.Invoke();
            OnBeltUpdated?.Invoke();
        }

        public void StartPlayFromMainMenu()
        {
            AudioManager.Instance?.PlayCue(SfxCue.UI);
            RetryRun();
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (IsInMainMenu) return;
            var player = DeadDepthsPlayerController.Instance;
            if (player != null && player.IsDead) return;
            if (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible) return;

            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (perfTracker == null) perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();
            if (perfTracker != null) perfTracker.SetTimePaused(paused);

            AudioManager.Instance?.PlayCue(SfxCue.UI);
            OnPauseChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// Starts and generates a level using the current DDA configuration.
        /// Keeps TotalScore when advancing levels and exits Main Menu if active.
        /// </summary>
        public void StartLevel(int levelNumber)
        {
            if (Instance == null) Instance = this;
            bool wasInMenu = IsInMainMenu;
            bool wasPaused = IsPaused;

            CurrentLevel = Mathf.Max(1, levelNumber);
            IsInMainMenu = false;
            IsPaused = false;
            IsLevelActive = true;
            LevelScore = 0;
            LevelZombiesKilled = 0;
            LevelCratesOpened = 0;
            LastLevelBonus = 0;
            SupplyIntelCollected = 0;
            AdrenalineBuffRemaining = 0f;
            Array.Clear(beltCooldownRemaining, 0, beltCooldownRemaining.Length);
            Time.timeScale = 1f;

            if (CurrentLevel > BestSector)
            {
                BestSector = CurrentLevel;
                PlayerPrefs.SetInt(PrefBestSector, BestSector);
                PlayerPrefs.Save();
            }

            var compUi = CompletionScreenUI.Instance ?? FindAnyObjectByType<CompletionScreenUI>();
            if (compUi != null)
            {
                compUi.Hide();
            }

            if (worldGen == null) worldGen = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (perfTracker == null) perfTracker = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();

            var profile = diffController != null ? diffController.CurrentProfile : new DifficultyController.LevelDifficultyProfile();

            if (worldGen != null)
            {
                worldGen.GenerateWorld(CurrentLevel, profile);
            }

            if (perfTracker != null)
            {
                perfTracker.SetTimePaused(false);
            }

            if (wasInMenu) OnMainMenuChanged?.Invoke(false);
            if (wasPaused) OnPauseChanged?.Invoke(false);

            OnObjectiveUpdated?.Invoke();
            OnBeltUpdated?.Invoke();
            OnLevelStarted?.Invoke(CurrentLevel);
        }

        /// <summary>
        /// Called when Player clicks Retry on Death or chooses New Run: resets score to 0 and restarts cleanly.
        /// </summary>
        public void RetryRun()
        {
            if (Instance == null) Instance = this;
            IsInMainMenu = false;
            IsPaused = false;
            TotalScore = 0;
            LevelScore = 0;
            TotalZombiesKilled = 0;
            LevelZombiesKilled = 0;
            TotalCratesOpened = 0;
            LevelCratesOpened = 0;
            LevelsCleared = 0;
            LastLevelBonus = 0;
            LastLevelClearTime = 0f;
            IsNewHighScore = false;
            ResetBeltForNewRun();
            Time.timeScale = 1f;

            if (diffController == null) diffController = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (diffController != null)
            {
                diffController.UpdateProfile(DifficultyTier.Balanced);
            }

            OnScoreChanged?.Invoke(TotalScore);
            StartLevel(managerConfig.StartingLevel);
        }

        private void ResetBeltForNewRun()
        {
            BandageCount = 1;
            MedkitCount = 0;
            AdrenalineCount = 0;
            AdrenalineBuffRemaining = 0f;
            Array.Clear(beltCooldownRemaining, 0, beltCooldownRemaining.Length);
        }

        public void AddSupplyIntel(int amount = 1)
        {
            if (amount <= 0) return;
            SupplyIntelCollected = Mathf.Min(RequiredSupplyIntel, SupplyIntelCollected + amount);
            OnObjectiveUpdated?.Invoke();
        }

        public void GrantAllSupplyIntelForDemo()
        {
            SupplyIntelCollected = RequiredSupplyIntel;
            OnObjectiveUpdated?.Invoke();
        }

        public void AddBeltItem(BeltItemType itemType, int quantity = 1)
        {
            if (quantity <= 0) return;
            switch (itemType)
            {
                case BeltItemType.Bandage: BandageCount = Mathf.Min(9, BandageCount + quantity); break;
                case BeltItemType.Medkit: MedkitCount = Mathf.Min(9, MedkitCount + quantity); break;
                case BeltItemType.Adrenaline: AdrenalineCount = Mathf.Min(9, AdrenalineCount + quantity); break;
            }
            OnBeltUpdated?.Invoke();
        }

        public int GetBeltCount(BeltItemType itemType)
        {
            switch (itemType)
            {
                case BeltItemType.Bandage: return BandageCount;
                case BeltItemType.Medkit: return MedkitCount;
                case BeltItemType.Adrenaline: return AdrenalineCount;
                default: return 0;
            }
        }

        public float GetBeltCooldownRatio(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3) return 0f;
            float dur = Mathf.Max(0.1f, beltCooldownDuration[slotIndex]);
            return Mathf.Clamp01(beltCooldownRemaining[slotIndex] / dur);
        }

        public float GetBeltCooldownSeconds(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3) return 0f;
            return beltCooldownRemaining[slotIndex];
        }

        public bool TryUseBeltSlot(int slotIndex)
        {
            if (IsInMainMenu || IsPaused || !IsLevelActive) return false;
            if (slotIndex < 0 || slotIndex >= 3) return false;

            var player = DeadDepthsPlayerController.Instance ?? FindAnyObjectByType<DeadDepthsPlayerController>();
            if (player == null || player.IsDead) return false;
            if (beltCooldownRemaining[slotIndex] > 0f) return false;

            switch (slotIndex)
            {
                case 0: // [1] Bandage
                    if (BandageCount <= 0 || player.CurrentHealth >= player.MaxHealth - 0.1f) return false;
                    BandageCount--;
                    beltCooldownRemaining[0] = beltCooldownDuration[0];
                    player.Heal(managerConfig.BandageHeal);
                    PlayerPerformanceTracker.Instance?.RecordResourceConsumed(0.5f);
                    AudioManager.Instance?.PlayCue(SfxCue.Pickup, player.transform.position);
                    CombatVfxManager.Instance?.PlayHealBurst(player.transform.position);
                    DamagePopupManager.Instance?.ShowHeal(player.transform.position + Vector3.up * 1.9f, Mathf.RoundToInt(managerConfig.BandageHeal));
                    OnBeltUpdated?.Invoke();
                    return true;

                case 1: // [2] Medkit
                    if (MedkitCount <= 0 || player.CurrentHealth >= player.MaxHealth - 0.1f) return false;
                    MedkitCount--;
                    beltCooldownRemaining[1] = beltCooldownDuration[1];
                    player.Heal(managerConfig.MedkitHeal);
                    PlayerPerformanceTracker.Instance?.RecordResourceConsumed(1.0f);
                    AudioManager.Instance?.PlayCue(SfxCue.Pickup, player.transform.position);
                    CombatVfxManager.Instance?.PlayHealBurst(player.transform.position);
                    DamagePopupManager.Instance?.ShowHeal(player.transform.position + Vector3.up * 1.9f, Mathf.RoundToInt(managerConfig.MedkitHeal));
                    OnBeltUpdated?.Invoke();
                    return true;

                case 2: // [3] Adrenaline
                    if (AdrenalineCount <= 0) return false;
                    AdrenalineCount--;
                    beltCooldownRemaining[2] = beltCooldownDuration[2];
                    AdrenalineBuffRemaining = managerConfig.AdrenalineDuration;
                    player.RestoreStamina(managerConfig.AdrenalineStamina);
                    PlayerPerformanceTracker.Instance?.RecordResourceConsumed(0.5f);
                    AudioManager.Instance?.PlayCue(SfxCue.Pickup, player.transform.position);
                    CombatVfxManager.Instance?.PlayHealBurst(player.transform.position);
                    DamagePopupManager.Instance?.ShowText(player.transform.position + Vector3.up * 2.0f, "ADRENALINE!", new Color(0.35f, 0.85f, 1f));
                    OnBeltUpdated?.Invoke();
                    return true;
            }

            return false;
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
            if (TotalScore > HighScore)
            {
                HighScore = TotalScore;
                IsNewHighScore = true;
                PlayerPrefs.SetInt(PrefHighScore, HighScore);
            }
            OnScoreChanged?.Invoke(TotalScore);
        }

        /// <summary>
        /// Direct safe-zone trigger preserved for automated verification tests.
        /// Gameplay extraction uses SafeZoneController (3/3 Supply Intel + 4.0s hold).
        /// </summary>
        public void OnPlayerEnteredSafeZone()
        {
            if (IsInMainMenu) return;
            CompleteLevel();
        }

        /// <summary>
        /// Called when the player extracts via helicopter in the safe zone.
        /// Evaluates DDA performance, awards level bonus, and shows Scoreboard.
        /// </summary>
        public void CompleteLevel()
        {
            if (!IsLevelActive) return;
            IsLevelActive = false;
            IsPaused = false;

            LevelsCleared++;
            LastLevelBonus = managerConfig.LevelClearBaseBonus * Mathf.Max(1, CurrentLevel);
            AddScore(LastLevelBonus);
            PlayerPrefs.Save();

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
