using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace EscapeFromNodnarb
{
    public enum GameFlowState
    {
        Title,
        Story,
        Playing,
        Result,
        LevelSelect,
        Loadout
    }

    [DefaultExecutionOrder(-100)]
    public sealed class NodnarbGame : MonoBehaviour
    {
        private const int MaxActiveNormalEnemies = 24;
        private const int MaxActiveEnemiesIncludingBoss = 25;
        private const int MaxActiveCards = 2;
        private const int MaxPooledEnemies = 32;
        private const int MaxPooledCards = 4;
        private const float TargetFrameSeconds = 1f / 60f;
        private const float MaxSimulationStepSeconds = 0.10f;
        private const int FrameSampleCapacity = 512;
        private const float LiveBudgetReportIntervalSeconds = 5f;
        private const float CardTapPaddingPixels = 42f;
        private static NodnarbGame instance;
        private static readonly EnemyKind[] CombatWarmupEnemyKinds =
        {
            EnemyKind.Melee,
            EnemyKind.Melee,
            EnemyKind.Melee,
            EnemyKind.Melee,
            EnemyKind.Melee,
            EnemyKind.Melee,
            EnemyKind.Ranged,
            EnemyKind.Ranged,
            EnemyKind.Ranged,
            EnemyKind.Ranged,
            EnemyKind.Armored,
            EnemyKind.Armored,
            EnemyKind.Armored,
            EnemyKind.Armored,
            EnemyKind.Boss
        };
        private static readonly CardKind[] CombatWarmupCardKinds =
        {
            CardKind.Weapon,
            CardKind.Recruit
        };

        private readonly List<EnemyAgent> enemies = new List<EnemyAgent>(40);
        private readonly List<TargetCard> cards = new List<TargetCard>(8);
        private readonly List<EnemyAgent> enemyPool = new List<EnemyAgent>(MaxPooledEnemies);
        private readonly List<TargetCard> cardPool = new List<TargetCard>(MaxPooledCards);
        private readonly List<Vector3> muzzlePositions = new List<Vector3>(16);

        private Camera gameCamera;
        private ProceduralWorld world;
        private CaptainSquad squad;
        private ProjectilePool projectiles;
        private ProceduralAudio audioSystem;
        private NodnarbHud hud;
        private ProgressData progress;
        private RunModel model;
        private LevelDefinition currentLevel;
        private LevelDefinition pendingLevel;
        private System.Random random;
        private GameFlowState state;
        private bool endless;
        private bool pendingEndless;
        private bool paused;
        private bool awaitingExplicitResume;
        private bool bossSpawned;
        private EnemyAgent bossAgent;
        private int pairSequence;
        private int waveSequence;
        private int selectedLevel = 1;
        private int attemptSequence;
        private int endlessBossCycle;
        private float elapsed;
        private float spawnTimer;
        private float cardTimer;
        private float fireTimer;
        private float hitAudioTimer;
        private int randomSeed;
        private int randomDrawCount;
        private bool nextRunIsReplay;
        private ResultViewData lastResult;
        private bool runtimeReady;
        private bool runtimeInitializationStarted;
        private bool combatVisualWarmupStarted;
        private Coroutine queuedRunStart;
        private int runStartRequestVersion;
        private int previousSleepTimeout = SleepTimeout.SystemSetting;
        private int peakEnemyCount;
        private int peakCardCount;
        private float worstFrameSeconds;
        private readonly float[] frameSamples = new float[FrameSampleCapacity];
        private readonly float[] frameSortScratch = new float[FrameSampleCapacity];
        private int frameSampleCount;
        private int frameSampleWriteIndex;
        private int overBudgetFrameCount;
        private float frameTimeP95Seconds;
        private float nextLiveBudgetReportElapsed;
        private float runtimeStartRealtime;
        private float runtimeReadyMilliseconds;
        private int comebackCardPairCount;
        private float modelSampleMilliseconds;
        private float inputSampleMilliseconds;
        private float volleySampleMilliseconds;
        private float waveSampleMilliseconds;
        private float cardSampleMilliseconds;
        private float modelWorstMilliseconds;
        private float inputWorstMilliseconds;
        private float volleyWorstMilliseconds;
        private float waveWorstMilliseconds;
        private float cardWorstMilliseconds;
        private string lastGestureReport;
        private bool firstVolleyReported;
        private Vector3 cameraRestPosition;
        private float cameraShakeTime;
        private float cameraShakeDuration;
        private float cameraShakeStrength;

        public static NodnarbGame Instance
        {
            get { return instance; }
        }

        public bool CombatActive
        {
            get { return state == GameFlowState.Playing && !paused && model != null && !model.IsEnded; }
        }

        public bool SquadAccessibilityVisualApplied
        {
            get { return squad != null && squad.AccessibilityVisualApplied; }
        }

        public bool ProjectileAccessibilityVisualApplied
        {
            get { return projectiles != null && projectiles.AccessibilityVisualApplied; }
        }

        public float SquadX
        {
            get { return squad == null ? 0f : squad.X; }
        }

        public float SquadRelativeX
        {
            get { return squad == null ? 0f : squad.RelativeX; }
        }

        public float SquadTargetX
        {
            get { return squad == null ? 0f : squad.TargetRelativeX; }
        }

        public float SimulationDeltaTime { get; private set; }

        public float RouteWorldX(float lateralX, float z)
        {
            return LaneRoute.CenterX(CurrentRoute, z) + lateralX;
        }

        public Vector3 CaptainPosition
        {
            get { return squad == null ? new Vector3(0f, 0f, GameTheme.CaptainZ) : squad.CaptainPosition; }
        }

        public int LastVolleyShooterCount { get; private set; }

        public int TotalFriendlyShotsFired { get; private set; }

        public int TotalFriendlyHits { get; private set; }

        public int TotalRecruitPickups { get; private set; }

        public int TotalWeaponPickups { get; private set; }

        public int TotalCardChoices { get; private set; }

        public int TotalCardPairsSpawned
        {
            get { return pairSequence; }
        }

        public CardActivationSource LastCardActivationSource { get; private set; }

        public string LastCardSelectionReport { get; private set; }

        public int TotalCombatFeedbackEvents
        {
            get { return projectiles == null ? 0 : projectiles.TotalFeedbackEvents; }
        }

        public int ActiveFriendlyProjectiles
        {
            get { return projectiles == null ? 0 : projectiles.ActiveFriendlyCount; }
        }

        public int ActiveCombatFeedback
        {
            get { return projectiles == null ? 0 : projectiles.ActiveFeedbackCount; }
        }

        public int ActiveProjectiles
        {
            get { return projectiles == null ? 0 : projectiles.ActiveProjectileCount; }
        }

        public string LastVisualBudgetReport { get; private set; }

        public float CameraShakeRemaining
        {
            get { return cameraShakeTime; }
        }

        public int TotalBossTelegraphs { get; private set; }

        public int ActiveEnemyCount
        {
            get { return enemies.Count; }
        }

        public int ActiveCardCount
        {
            get { return cards.Count; }
        }

        public int PeakEnemyCount { get { return peakEnemyCount; } }

        public int PeakCardCount { get { return peakCardCount; } }

        public int PooledEnemyCount { get { return enemyPool.Count; } }

        public int PooledCardCount { get { return cardPool.Count; } }

        public bool CombatVisualPoolsWarmed { get; private set; }

        public int CombatVisualWarmupObjects { get; private set; }

        public float WorstFrameSeconds { get { return worstFrameSeconds; } }

        public float FrameTimeP95Seconds { get { return frameTimeP95Seconds; } }

        public int OverBudgetFrameCount { get { return overBudgetFrameCount; } }

        public int FrameSampleCount { get { return frameSampleCount; } }

        public string LastBudgetReport { get; private set; }

        public string LastBalanceReport { get; private set; }

        public string LastReadabilityReport { get; private set; }

        public string LastStabilityReport { get; private set; }

        public string LastGestureReport
        {
            get { return lastGestureReport; }
        }

        public float RunElapsed { get { return elapsed; } }

        public float RunSpawnTimer { get { return spawnTimer; } }

        public float RunCardTimer { get { return cardTimer; } }

        public int MaxActiveEnemyCount { get { return MaxActiveEnemiesIncludingBoss; } }

        public int MaxActiveCardCount { get { return MaxActiveCards; } }

        public RunModel Model
        {
            get { return model; }
        }

        public GameFlowState State
        {
            get { return state; }
        }

        public int CurrentLevelIndex
        {
            get
            {
                if (state == GameFlowState.Playing && currentLevel != null)
                {
                    return currentLevel.Index;
                }

                if (state == GameFlowState.Story && pendingLevel != null)
                {
                    return pendingLevel.Index;
                }

                return selectedLevel;
            }
        }

        private RouteShape CurrentRoute
        {
            get
            {
                if (currentLevel != null)
                {
                    return currentLevel.Route;
                }

                if (pendingLevel != null)
                {
                    return pendingLevel.Route;
                }

                return CampaignCatalog.Get(selectedLevel).Route;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.Portrait;
            previousSleepTimeout = Screen.sleepTimeout;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            BuildCameraAndLight();
            world = new ProceduralWorld(transform);
            squad = new CaptainSquad(transform);
            hud = new NodnarbHud();
        }

        private IEnumerator Start()
        {
            // Let the Android activity receive its first frame before building
            // the pooled combat objects, UI hierarchy, and title resources.
            yield return null;
            if (Application.platform == RuntimePlatform.Android)
            {
                yield return EnsureRuntimeReadyRoutine();
            }
            else
            {
                EnsureRuntimeReady();
            }
        }

        private void EnsureRuntimeReady()
        {
            if (runtimeReady || runtimeInitializationStarted)
            {
                return;
            }

            runtimeInitializationStarted = true;
            runtimeStartRealtime = Time.realtimeSinceStartup;
            Debug.Log("NODNARB_STARTUP stage=runtime_sync_begin");
            CreateProjectilePool(true);
            CreateAudioSystem(true);
            EmitVisualBudgetReport();
            hud.Initialize(this);
            LoadProgress();
            runtimeReady = true;
            runtimeReadyMilliseconds = (Time.realtimeSinceStartup - runtimeStartRealtime) * 1000f;
            Debug.Log("NODNARB_STARTUP stage=runtime_sync_ready elapsed_ms="
                + runtimeReadyMilliseconds.ToString("F0"));
            ShowTitle();
        }

        private IEnumerator EnsureRuntimeReadyRoutine()
        {
            if (runtimeReady || runtimeInitializationStarted)
            {
                yield break;
            }

            runtimeInitializationStarted = true;
            runtimeStartRealtime = Time.realtimeSinceStartup;
            Debug.Log("NODNARB_STARTUP stage=runtime_begin");
            CreateProjectilePool(false);
            yield return projectiles.InitializeRoutine(this);
            Debug.Log("NODNARB_STARTUP stage=projectile_pool_ready");
            EmitVisualBudgetReport();
            yield return null;

            CreateAudioSystem(false);
            yield return audioSystem.InitializeRoutine();
            Debug.Log("NODNARB_STARTUP stage=audio_ready");
            yield return null;

            yield return hud.InitializeRoutine(this);
            Debug.Log("NODNARB_STARTUP stage=title_ready");
            yield return null;

            LoadProgress();
            ShowTitle();
            yield return hud.ContinueInitializeRoutine();
            runtimeReady = true;
            hud.SetTitleInteractive(true);
            runtimeReadyMilliseconds = (Time.realtimeSinceStartup - runtimeStartRealtime) * 1000f;
            Debug.Log("NODNARB_STARTUP stage=runtime_ready elapsed_ms="
                + runtimeReadyMilliseconds.ToString("F0"));
            ShowTitle();
        }

        private void CreateProjectilePool(bool initializeNow)
        {
            GameObject projectileObject = new GameObject("ProjectilePool");
            projectileObject.transform.SetParent(transform, false);
            projectiles = projectileObject.AddComponent<ProjectilePool>();
            if (initializeNow)
            {
                projectiles.Initialize(this);
            }
        }

        private void CreateAudioSystem(bool initializeNow)
        {
            GameObject audioObject = new GameObject("ProceduralAudio");
            audioObject.transform.SetParent(transform, false);
            audioSystem = audioObject.AddComponent<ProceduralAudio>();
            if (initializeNow)
            {
                audioSystem.Initialize();
            }
        }

        private void LoadProgress()
        {
            progress = LocalProgress.Load();
            selectedLevel = progress.UnlockedLevel;
            Debug.Log("NODNARB_STARTUP stage=progress_loaded pending_run=" + (progress.PendingRun != null));
        }

        private void Update()
        {
            if (!runtimeReady)
            {
                return;
            }

            HandleBackAndPauseInput();
            UpdateCameraFeedback();
            if (state == GameFlowState.Playing && model != null && model.IsEnded)
            {
                FinishRun();
                return;
            }

            if (!CombatActive)
            {
                return;
            }

            float rawFrameSeconds = Mathf.Max(Time.unscaledDeltaTime, Time.deltaTime);
            RecordFrameTime(rawFrameSeconds);
            float deltaTime = Mathf.Min(Time.deltaTime, MaxSimulationStepSeconds);
            SimulationDeltaTime = deltaTime;
            elapsed += deltaTime;
            if (elapsed >= nextLiveBudgetReportElapsed)
            {
                EmitLiveBudgetReport();
            }
            hitAudioTimer = Mathf.Max(0f, hitAudioTimer - deltaTime);
            float sampleStart = Time.realtimeSinceStartup;
            BeginRuntimeSample("Nodnarb.Update.Model");
            model.Tick(deltaTime);
            squad.Refresh(model.SoldierCount);
            EndRuntimeSample();
            float phaseMilliseconds = (Time.realtimeSinceStartup - sampleStart) * 1000f;
            modelSampleMilliseconds += phaseMilliseconds;
            modelWorstMilliseconds = Mathf.Max(modelWorstMilliseconds, phaseMilliseconds);
            bool cardInputConsumed = HandleCardSelectionInput();
            sampleStart = Time.realtimeSinceStartup;
            BeginRuntimeSample("Nodnarb.Update.Input");
            squad.TickInput(deltaTime, cardInputConsumed, LaneRoute.CenterX(CurrentRoute, GameTheme.CaptainZ));
            EndRuntimeSample();
            phaseMilliseconds = (Time.realtimeSinceStartup - sampleStart) * 1000f;
            inputSampleMilliseconds += phaseMilliseconds;
            inputWorstMilliseconds = Mathf.Max(inputWorstMilliseconds, phaseMilliseconds);
            if (squad.IsDragging)
            {
                hud.NoteOnboardingAction(0);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ActivateRapidFire();
            }

            fireTimer -= deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer += model.FireInterval;
                sampleStart = Time.realtimeSinceStartup;
                BeginRuntimeSample("Nodnarb.Update.Volley");
                FireSquadVolley();
                EndRuntimeSample();
                phaseMilliseconds = (Time.realtimeSinceStartup - sampleStart) * 1000f;
                volleySampleMilliseconds += phaseMilliseconds;
                volleyWorstMilliseconds = Mathf.Max(volleyWorstMilliseconds, phaseMilliseconds);
            }

            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f && (endless || elapsed < currentLevel.DurationSeconds - 3f))
            {
                sampleStart = Time.realtimeSinceStartup;
                BeginRuntimeSample("Nodnarb.Update.Wave");
                SpawnWave();
                EndRuntimeSample();
                phaseMilliseconds = (Time.realtimeSinceStartup - sampleStart) * 1000f;
                waveSampleMilliseconds += phaseMilliseconds;
                waveWorstMilliseconds = Mathf.Max(waveWorstMilliseconds, phaseMilliseconds);
            }

            cardTimer -= deltaTime;
            if (cardTimer <= 0f)
            {
                sampleStart = Time.realtimeSinceStartup;
                BeginRuntimeSample("Nodnarb.Update.Cards");
                SpawnCardPair();
                EndRuntimeSample();
                phaseMilliseconds = (Time.realtimeSinceStartup - sampleStart) * 1000f;
                cardSampleMilliseconds += phaseMilliseconds;
                cardWorstMilliseconds = Mathf.Max(cardWorstMilliseconds, phaseMilliseconds);
            }

            if (!endless)
            {
                if (!bossSpawned && elapsed >= currentLevel.DurationSeconds - currentLevel.BossLeadSeconds)
                {
                    SpawnBoss();
                }

                if (bossSpawned && (bossAgent == null || !bossAgent.IsAlive) && elapsed >= currentLevel.DurationSeconds)
                {
                    model.SecureExtraction();
                }
            }
            else
            {
                int desiredCycle = 1 + Mathf.FloorToInt(elapsed / 52f);
                if (desiredCycle > endlessBossCycle)
                {
                    endlessBossCycle = desiredCycle;
                    SpawnBoss();
                }
            }

            bool bossActive = bossAgent != null && bossAgent.IsAlive;
            if (bossActive)
            {
                audioSystem.SetMusic("boss", 0.92f);
            }
            else if (!endless && elapsed >= currentLevel.DurationSeconds - 8f)
            {
                audioSystem.SetMusic("extraction", 1f);
            }
            else
            {
                audioSystem.SetMusic("combat", Mathf.Clamp01(0.32f + CurrentDifficulty() * 0.55f));
            }
            hud.UpdateGameplay(model, currentLevel, elapsed, endless, bossActive, squad.ShooterCount, cards.Count > 0, SquadRelativeX, cardTimer);
            if (model.IsEnded)
            {
                FinishRun();
            }
        }

        private void OnApplicationPause(bool applicationPaused)
        {
            Debug.Log("NODNARB_LIFECYCLE application_pause=" + applicationPaused
                + " state=" + state + " paused=" + paused);
            if (applicationPaused)
            {
                SaveAndPauseForLifecycle();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log("NODNARB_LIFECYCLE application_focus=" + hasFocus
                + " state=" + state + " paused=" + paused);
            if (!hasFocus)
            {
                SaveAndPauseForLifecycle();
            }
        }

        private void SaveAndPauseForLifecycle()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            if (model != null && model.IsEnded)
            {
                FinishRun();
                return;
            }

            SavePendingRun();
            if (!paused)
            {
                TogglePause();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            Screen.sleepTimeout = previousSleepTimeout;
            Time.timeScale = 1f;
        }

        public void ShowTitle()
        {
            EnsureRuntimeReady();
            runStartRequestVersion++;
            if (queuedRunStart != null)
            {
                StopCoroutine(queuedRunStart);
                queuedRunStart = null;
            }
            Time.timeScale = 1f;
            paused = false;
            awaitingExplicitResume = false;
            state = GameFlowState.Title;
            CleanupRunObjects();
            currentLevel = null;
            pendingLevel = null;
            progress = LocalProgress.Load();
            selectedLevel = Mathf.Clamp(progress.UnlockedLevel, 1, 10);
            nextRunIsReplay = false;
            if (projectiles != null)
            {
                projectiles.TrimToWarmSet();
            }
            // The title artwork is already a full-screen backdrop. Avoid building
            // the imported terrain set before the first Android frame is stable.
            // The selected sector is built when the player opens its story screen.
            world.Clear();
            audioSystem.SetMusic("title", 0.18f);
            gameCamera.backgroundColor = GameTheme.Void;
            hud.ShowTitle(progress);
        }

        public void ContinueCampaign()
        {
            EnsureRuntimeReady();
            if (progress.PendingRun != null)
            {
                ResumePendingRun();
                return;
            }

            OpenStory(CampaignCatalog.Get(progress.UnlockedLevel), false);
        }

        public void OpenEndlessStory()
        {
            EnsureRuntimeReady();
            OpenStory(CampaignCatalog.CreateEndless(Environment.TickCount), true);
        }

        public void OpenLevelSelect()
        {
            EnsureRuntimeReady();
            state = GameFlowState.LevelSelect;
            pendingLevel = null;
            selectedLevel = Mathf.Clamp(selectedLevel, 1, 10);
            world.Build(CampaignCatalog.Get(selectedLevel), gameCamera);
            EmitVisualBudgetReport();
            hud.ShowLevelSelect(progress, selectedLevel);
        }

        public void SelectPreviousLevel()
        {
            selectedLevel = selectedLevel <= 1 ? 10 : selectedLevel - 1;
            world.Build(CampaignCatalog.Get(selectedLevel), gameCamera);
            EmitVisualBudgetReport();
            hud.RefreshLevelSelect(progress, selectedLevel);
        }

        public void SelectNextLevel()
        {
            selectedLevel = selectedLevel >= 10 ? 1 : selectedLevel + 1;
            world.Build(CampaignCatalog.Get(selectedLevel), gameCamera);
            EmitVisualBudgetReport();
            hud.RefreshLevelSelect(progress, selectedLevel);
        }

        public void OpenSelectedStory()
        {
            if (selectedLevel <= progress.UnlockedLevel)
            {
                OpenStory(CampaignCatalog.Get(selectedLevel), false);
            }
        }

        public void OpenLoadout()
        {
            EnsureRuntimeReady();
            state = GameFlowState.Loadout;
            hud.ShowLoadout(progress);
        }

        public void ToggleSound()
        {
            NodnarbSettings.SoundEnabled = !NodnarbSettings.SoundEnabled;
            if (audioSystem != null)
            {
                audioSystem.RefreshSettings();
            }

            hud.RefreshSettings();
        }

        public void ToggleMusic()
        {
            NodnarbSettings.MusicEnabled = !NodnarbSettings.MusicEnabled;
            if (audioSystem != null)
            {
                audioSystem.RefreshSettings();
                if (NodnarbSettings.MusicEnabled)
                {
                    audioSystem.SetMusic("title", 0.18f);
                }
            }

            hud.RefreshSettings();
        }

        public void ToggleHaptics()
        {
            NodnarbSettings.HapticsEnabled = !NodnarbSettings.HapticsEnabled;
            hud.RefreshSettings();
        }

        public void ToggleCaptions()
        {
            NodnarbSettings.CaptionsEnabled = !NodnarbSettings.CaptionsEnabled;
            hud.RefreshSettings();
        }

        public void ToggleHighContrast()
        {
            NodnarbSettings.HighContrastEnabled = !NodnarbSettings.HighContrastEnabled;
            ApplyAccessibilitySettings();
        }

        public void ToggleReducedMotion()
        {
            NodnarbSettings.ReducedMotionEnabled = !NodnarbSettings.ReducedMotionEnabled;
            ApplyAccessibilitySettings();
        }

        private void ApplyAccessibilitySettings()
        {
            hud.RefreshSettings();
            for (int index = 0; index < enemies.Count; index++)
            {
                if (enemies[index] != null)
                {
                    enemies[index].ApplyAccessibilitySettings();
                }
            }

            for (int index = 0; index < cards.Count; index++)
            {
                if (cards[index] != null)
                {
                    cards[index].ApplyAccessibilitySettings();
                }
            }

            for (int index = 0; index < enemyPool.Count; index++)
            {
                if (enemyPool[index] != null)
                {
                    enemyPool[index].ApplyAccessibilitySettings();
                }
            }

            for (int index = 0; index < cardPool.Count; index++)
            {
                if (cardPool[index] != null)
                {
                    cardPool[index].ApplyAccessibilitySettings();
                }
            }

            if (squad != null)
            {
                squad.ApplyAccessibilitySettings();
            }

            if (projectiles != null)
            {
                projectiles.ApplyAccessibilitySettings();
            }
        }

        public void SelectPreviousWeapon()
        {
            SelectWeapon(-1);
        }

        public void SelectNextWeapon()
        {
            SelectWeapon(1);
        }

        public void SelectPreviousSuit()
        {
            SelectSuit(-1);
        }

        public void SelectNextSuit()
        {
            SelectSuit(1);
        }

        public void BeginPendingRun()
        {
            if (pendingLevel == null)
            {
                return;
            }

            QueueRunStart(pendingLevel, pendingEndless, null);
        }

        public void ActivateRapidFire()
        {
            if (CombatActive && model.ActivateRapidFire())
            {
                if (hud.NoteOnboardingAction(2))
                {
                    CompleteOnboarding();
                }
                audioSystem.Ability();
                projectiles.SpawnBurst(CaptainPosition + Vector3.up * 0.7f, GameTheme.AccessibleSignalBright, 0.72f);
                RequestCameraFeedback(0.075f, 0.14f);
                hud.ShowAudioCaption("OVERDRIVE ACTIVE");
                DeviceFeedback.Pulse(DeviceFeedbackKind.Ability);
            }
        }

        public void TogglePause()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            bool wasPaused = paused;
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            Debug.Log("NODNARB_PAUSE paused=" + paused + " state=" + state);
            if (paused)
            {
                hud.ShowPause();
            }
            else
            {
                if (wasPaused && awaitingExplicitResume)
                {
                    awaitingExplicitResume = false;
                    ClearPendingRun();
                }
                hud.HidePause();
            }
        }

        public void RetryRun()
        {
            Time.timeScale = 1f;
            paused = false;
            ClearPendingRun();
            nextRunIsReplay = true;
            QueueRunStart(currentLevel, endless, null);
        }

        public void AbortRun()
        {
            Time.timeScale = 1f;
            paused = false;
            ClearPendingRun();
            ShowTitle();
        }

        public void ReplayAfterResult()
        {
            if (lastResult == null)
            {
                return;
            }

            LevelDefinition replayLevel = lastResult.Endless
                ? CampaignCatalog.CreateEndless(Environment.TickCount)
                : CampaignCatalog.Get(lastResult.Level);
            nextRunIsReplay = true;
            OpenStory(replayLevel, lastResult.Endless);
        }

        public void AdvanceAfterResult()
        {
            if (lastResult == null || !lastResult.CanAdvance)
            {
                return;
            }

            nextRunIsReplay = false;
            OpenStory(CampaignCatalog.Get(lastResult.Level + 1), false);
        }

        public void RegisterEnemy(EnemyAgent enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                peakEnemyCount = Mathf.Max(peakEnemyCount, enemies.Count);
            }
        }

        public void UnregisterEnemy(EnemyAgent enemy)
        {
            enemies.Remove(enemy);
            if (bossAgent == enemy)
            {
                bossAgent = null;
            }
        }

        public void ReleaseEnemy(EnemyAgent enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.transform.SetParent(transform, false);
            enemy.gameObject.SetActive(false);
            if (enemyPool.Contains(enemy))
            {
                return;
            }

            if (enemyPool.Count >= MaxPooledEnemies)
            {
                Destroy(enemy.gameObject);
                return;
            }

            enemyPool.Add(enemy);
        }

        public void RegisterCard(TargetCard card)
        {
            if (card != null && !cards.Contains(card) && cards.Count < MaxActiveCards)
            {
                cards.Add(card);
                peakCardCount = Mathf.Max(peakCardCount, cards.Count);
            }
        }

        public void UnregisterCard(TargetCard card)
        {
            cards.Remove(card);
        }

        public void ReleaseCard(TargetCard card)
        {
            if (card == null)
            {
                return;
            }

            card.transform.SetParent(transform, false);
            card.gameObject.SetActive(false);
            if (cardPool.Contains(card))
            {
                return;
            }

            if (cardPool.Count >= MaxPooledCards)
            {
                Destroy(card.gameObject);
                return;
            }

            cardPool.Add(card);
        }

        public void OnEnemyKilled(EnemyAgent enemy, int score, int salvage)
        {
            if (model == null || model.IsEnded)
            {
                return;
            }

            model.RegisterKill(score, salvage);
            if (enemy != null)
            {
                projectiles.SpawnBurst(enemy.HitPosition, enemy.IsBoss ? GameTheme.AccessibleDanger : GameTheme.AlienGlow,
                    enemy.IsBoss ? 0.78f : 0.38f);
                RequestCameraFeedback(enemy.IsBoss ? 0.12f : 0.045f, enemy.IsBoss ? 0.16f : 0.08f);
            }
            if (enemy.IsBoss)
            {
                model.AddScore(250);
            }

            if (hitAudioTimer <= 0f)
            {
                hitAudioTimer = 0.07f;
                audioSystem.Hit();
            }
        }

        public void OnCardActivated(TargetCard activated)
        {
            OnCardActivated(activated, CardActivationSource.AutoFire);
        }

        public void OnCardActivated(TargetCard activated, CardActivationSource source)
        {
            if (!CombatActive || activated == null)
            {
                return;
            }

            LastCardActivationSource = source;
            hud.NoteOnboardingAction(1);
            TotalCardChoices++;
            LastCardSelectionReport = "NODNARB_CARD pair=" + activated.PairId
                + " kind=" + activated.Kind
                + " source=" + source;
            Debug.Log(LastCardSelectionReport);

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                TargetCard other = cards[i];
                if (other != null && other.PairId == activated.PairId)
                {
                    other.Cancel();
                }
            }

            if (activated.Kind == CardKind.Recruit)
            {
                model.Recruit();
                squad.Refresh(model.SoldierCount);
                TotalRecruitPickups++;
                audioSystem.Recruit();
                hud.ShowAudioCaption("CREW RECRUITED");
            }
            else
            {
                model.UpgradeWeapon();
                TotalWeaponPickups++;
                audioSystem.Upgrade();
                hud.ShowAudioCaption("WEAPON UPGRADED");
            }

            model.AddScore(50);
            projectiles.SpawnBurst(activated.HitPosition,
                activated.Kind == CardKind.Recruit ? GameTheme.AccessibleSignalBright : GameTheme.AccessibleWeapon,
                0.58f);
            RequestCameraFeedback(0.08f, 0.12f);
            hud.ShowPickup(activated.Kind, model);
            DeviceFeedback.Pulse(DeviceFeedbackKind.Pickup);
        }

        public bool TrySelectCardAtScreenPosition(Vector2 screenPosition)
        {
            if (!CombatActive || gameCamera == null || squad == null || squad.IsDragging)
            {
                return false;
            }

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                TargetCard card = cards[index];
                if (card != null
                    && CardChoice.IsSideAligned(card.Kind, SquadRelativeX)
                    && card.IsScreenTarget(gameCamera, screenPosition, CardTapPaddingPixels))
                {
                    return card.SelectByPlayer();
                }
            }

            return false;
        }

        public void OnFrontLineBreached(float contactDamage)
        {
            if (!CombatActive)
            {
                return;
            }

            model.BreachFrontLine();
            squad.SetDamageFlash(true);
            StartCoroutine(ClearCaptainDamageFlashRoutine());
            hud.PulseDamage();
            hud.ShowAudioCaption("CAPTAIN DAMAGED");
            audioSystem.Damage();
            DeviceFeedback.Pulse(DeviceFeedbackKind.Damage);
        }

        public void FireHostile(Vector3 origin, Vector3 target, float damage)
        {
            if (CombatActive)
            {
                projectiles.FireHostile(origin, target, damage);
                projectiles.SpawnBurst(origin, GameTheme.ProjectileHostile, 0.20f);
            }
        }

        public void NotifyBossTelegraph()
        {
            if (currentLevel == null)
            {
                return;
            }

            TotalBossTelegraphs++;
            RequestCameraFeedback(0.085f, 0.20f);
            hud.ShowBossTelegraph(currentLevel.BossName);
            hud.ShowAudioCaption("INCOMING VOLLEY");
            audioSystem.BossTelegraph();
        }

        public bool TryHitFriendly(Vector3 position, float damage, float radius)
        {
            if (!CombatActive)
            {
                return false;
            }

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                TargetCard card = cards[i];
                if (card == null || !card.IsAlive || !CardChoice.IsSideAligned(card.Kind, SquadRelativeX))
                {
                    continue;
                }

                if (Overlaps(position, card.HitPosition, radius + card.HitRadius))
                {
                    card.ApplyDamage(damage);
                    TotalFriendlyHits++;
                    PlayHitTick();
                    return true;
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyAgent enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (Overlaps(position, enemy.HitPosition, radius + enemy.HitRadius))
                {
                    enemy.ApplyDamage(damage);
                    TotalFriendlyHits++;
                    PlayHitTick();
                    return true;
                }
            }

            return false;
        }

        public bool TryHitCaptain(Vector3 position, float damage, float radius)
        {
            if (!CombatActive)
            {
                return false;
            }

            Vector3 target = CaptainPosition + Vector3.up * 0.65f;
            if (!Overlaps(position, target, radius + 0.54f))
            {
                return false;
            }

            model.DamageCaptain(damage);
            squad.SetDamageFlash(true);
            StartCoroutine(ClearCaptainDamageFlashRoutine());
            projectiles.SpawnBurst(target, GameTheme.AccessibleDanger, 0.46f);
            RequestCameraFeedback(0.10f, 0.12f);
            hud.PulseDamage();
            audioSystem.Damage();
            DeviceFeedback.Pulse(DeviceFeedbackKind.Damage);
            return true;
        }

        private IEnumerator ClearCaptainDamageFlashRoutine()
        {
            yield return new WaitForSecondsRealtime(0.10f);
            if (squad != null)
            {
                squad.SetDamageFlash(false);
            }
        }

        private void BuildCameraAndLight()
        {
            bool mobileRuntime = Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer;
            // The active Android quality profile currently disables shadows globally.
            // Keep the mobile budget to one low-resolution key light, but do not let
            // that profile silently remove the depth cue the scene is authored for.
            if (QualitySettings.shadows == ShadowQuality.Disable)
            {
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 22f);
            }

            if (mobileRuntime)
            {
                // The portrait battlefield is already high-contrast and uses a
                // low-resolution key shadow. MSAA at 1080x2400 costs more than it
                // adds on the target mobile path, especially on software-rendered
                // test devices.
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 18f);
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Destroy(cameras[i].gameObject);
            }

            GameObject cameraObject = new GameObject("GameCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 8.15f, -7.45f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.00f, 4.10f));
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            gameCamera.fieldOfView = 45f;
            gameCamera.nearClipPlane = 0.2f;
            gameCamera.farClipPlane = 60f;
            gameCamera.allowHDR = false;
            gameCamera.allowMSAA = !mobileRuntime;
            cameraObject.tag = "MainCamera";
            cameraRestPosition = gameCamera.transform.localPosition;

            GameObject lightObject = new GameObject("SignalLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.Lerp(GameTheme.Text, GameTheme.Signal, 0.10f);
            light.intensity = 1.34f;
            light.shadows = QualitySettings.shadows == ShadowQuality.Disable
                ? LightShadows.None
                : LightShadows.Hard;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            light.shadowStrength = 0.72f;
            light.shadowBias = 0.06f;
            light.shadowNormalBias = 0.35f;

            GameObject rimObject = new GameObject("CanyonRimLight");
            rimObject.transform.SetParent(transform, false);
            rimObject.transform.rotation = Quaternion.Euler(32f, 152f, 0f);
            Light rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = Color.Lerp(GameTheme.CanyonHighlight, GameTheme.Text, 0.24f);
            rim.intensity = 0.78f;
            rim.shadows = LightShadows.None;
            EmitVisualBudgetReport();
        }

        public void RequestCameraFeedback(float strength, float duration)
        {
            if (NodnarbSettings.ReducedMotionEnabled || gameCamera == null)
            {
                return;
            }

            cameraShakeStrength = Mathf.Max(cameraShakeStrength, Mathf.Clamp(strength, 0f, 0.16f));
            cameraShakeDuration = Mathf.Max(cameraShakeDuration, Mathf.Clamp(duration, 0.02f, 0.30f));
            cameraShakeTime = Mathf.Max(cameraShakeTime, cameraShakeDuration);
        }

        private void UpdateCameraFeedback()
        {
            if (gameCamera == null)
            {
                return;
            }

            if (NodnarbSettings.ReducedMotionEnabled || cameraShakeTime <= 0f)
            {
                cameraShakeTime = 0f;
                cameraShakeStrength = 0f;
                cameraShakeDuration = 0f;
                gameCamera.transform.localPosition = cameraRestPosition;
                return;
            }

            cameraShakeTime = Mathf.Max(0f, cameraShakeTime - Time.unscaledDeltaTime);
            float normalized = cameraShakeDuration <= 0f ? 0f : cameraShakeTime / cameraShakeDuration;
            float strength = cameraShakeStrength * Mathf.Clamp01(normalized);
            float phase = Time.unscaledTime * 52f;
            gameCamera.transform.localPosition = cameraRestPosition + new Vector3(
                Mathf.Sin(phase) * strength,
                Mathf.Cos(phase * 1.23f) * strength * 0.52f,
                0f);
        }

        private void EmitVisualBudgetReport()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            HashSet<Material> materials = new HashSet<Material>();
            int activeRendererCount = 0;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                activeRendererCount++;
                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    if (sharedMaterials[materialIndex] != null)
                    {
                        materials.Add(sharedMaterials[materialIndex]);
                    }
                }
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            int activeUiGraphicCount = 0;
            for (int index = 0; index < graphics.Length; index++)
            {
                if (graphics[index] != null && graphics[index].enabled && graphics[index].gameObject.activeInHierarchy)
                {
                    activeUiGraphicCount++;
                }
            }

            Light[] lights = GetComponentsInChildren<Light>(true);
            int shadowCastingLights = 0;
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index] != null && lights[index].shadows != LightShadows.None)
                {
                    shadowCastingLights++;
                }
            }

            Component[] components = GetComponentsInChildren<Component>(true);
            int particleSystems = 0;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] != null && components[index].GetType().Name == "ParticleSystem")
                {
                    particleSystems++;
                }
            }
            LastVisualBudgetReport = NodnarbVisualBudget.FormatReport(
                activeRendererCount,
                materials.Count,
                lights.Length,
                shadowCastingLights,
                particleSystems,
                projectiles == null ? 0 : projectiles.ActiveProjectileCount,
                projectiles == null ? 0 : projectiles.ActiveFeedbackCount,
                activeUiGraphicCount,
                PrimitiveFactory.MaterialCacheCount);
            Debug.Log(LastVisualBudgetReport);
        }

        private void HandleBackAndPauseInput()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            HandleSystemBack();
        }

        public void HandleSystemBack()
        {
            EnsureRuntimeReady();
            if (state == GameFlowState.Playing)
            {
                TogglePause();
            }
            else if (state != GameFlowState.Title)
            {
                ShowTitle();
            }
        }

        private void ResumePendingRun()
        {
            PausedRunData snapshot = progress.PendingRun;
            if (snapshot == null || !snapshot.IsValid())
            {
                ClearPendingRun();
                OpenStory(CampaignCatalog.Get(progress.UnlockedLevel), false);
                return;
            }

            LevelDefinition level = snapshot.Endless
                ? CampaignCatalog.CreateEndless(snapshot.LevelSeed != 0 ? snapshot.LevelSeed : Environment.TickCount)
                : CampaignCatalog.Get(snapshot.LevelIndex);
            QueueRunStart(level, snapshot.Endless, snapshot);
        }

        private void QueueRunStart(LevelDefinition level, bool isEndless, PausedRunData snapshot)
        {
            if (level == null)
            {
                return;
            }

            // The editor path stays synchronous for deterministic PlayMode tests.
            // Android and iOS must wait for the staged visual warm-up so a fast
            // deploy or resume cannot create the first combat actors on a live
            // frame.
            if (!Application.isMobilePlatform || CombatVisualPoolsWarmed)
            {
                StartRun(level, isEndless, snapshot);
                return;
            }

            BeginCombatVisualWarmup(level);
            int requestVersion = ++runStartRequestVersion;
            if (queuedRunStart != null)
            {
                StopCoroutine(queuedRunStart);
            }

            queuedRunStart = StartCoroutine(StartRunAfterCombatWarmupRoutine(
                level,
                isEndless,
                snapshot,
                requestVersion));
        }

        private IEnumerator StartRunAfterCombatWarmupRoutine(
            LevelDefinition level,
            bool isEndless,
            PausedRunData snapshot,
            int requestVersion)
        {
            while (requestVersion == runStartRequestVersion && !CombatVisualPoolsWarmed)
            {
                yield return null;
            }

            if (requestVersion != runStartRequestVersion || !CombatVisualPoolsWarmed)
            {
                yield break;
            }

            queuedRunStart = null;
            StartRun(level, isEndless, snapshot);
        }

        private void SavePendingRun()
        {
            if (progress == null || model == null || currentLevel == null || model.IsEnded)
            {
                return;
            }

            progress.PendingRun = model.CreateSnapshot(currentLevel.Index, endless, elapsed);
            progress.PendingRun.SelectedSuit = progress.SelectedSuit;
            progress.PendingRun.SquadRelativeX = Mathf.Clamp(SquadRelativeX,
                -(GameTheme.ArenaHalfWidth - 0.35f), GameTheme.ArenaHalfWidth - 0.35f);
            progress.PendingRun.SquadTargetX = Mathf.Clamp(SquadTargetX,
                -(GameTheme.ArenaHalfWidth - 0.35f), GameTheme.ArenaHalfWidth - 0.35f);
            progress.PendingRun.LevelSeed = currentLevel.Seed;
            progress.PendingRun.RandomSeed = randomSeed;
            progress.PendingRun.RandomDrawCount = randomDrawCount;
            progress.PendingRun.PairSequence = pairSequence;
            progress.PendingRun.WaveSequence = waveSequence;
            progress.PendingRun.EndlessBossCycle = endlessBossCycle;
            progress.PendingRun.RecruitPickups = TotalRecruitPickups;
            progress.PendingRun.WeaponPickups = TotalWeaponPickups;
            progress.PendingRun.CardChoices = TotalCardChoices;
            progress.PendingRun.LastCardActivationSource = LastCardActivationSource;
            progress.PendingRun.LastCardSelectionReport = LastCardSelectionReport;
            progress.PendingRun.SpawnTimer = Mathf.Max(0f, spawnTimer);
            progress.PendingRun.CardTimer = Mathf.Max(0f, cardTimer);
            progress.PendingRun.FireTimer = Mathf.Max(0f, fireTimer);
            progress.PendingRun.HitAudioTimer = Mathf.Max(0f, hitAudioTimer);
            progress.PendingRun.BossSpawned = bossSpawned;
            progress.PendingRun.ResumePaused = true;
            progress.PendingRun.Enemies = CaptureEnemyCheckpoints();
            progress.PendingRun.Cards = CaptureCardCheckpoints();
            LocalProgress.Save(progress);
            if (progress.PendingRun == null)
            {
                Debug.LogWarning("NODNARB_LIFECYCLE pending_run_rejected level=" + currentLevel.Index);
                return;
            }

            Debug.Log("NODNARB_LIFECYCLE pending_run_saved level=" + currentLevel.Index
                + " elapsed=" + elapsed.ToString("0.00")
                + " squad_x=" + progress.PendingRun.SquadRelativeX.ToString("0.00")
                + " target_x=" + progress.PendingRun.SquadTargetX.ToString("0.00"));
        }

        private PausedEnemyData[] CaptureEnemyCheckpoints()
        {
            List<PausedEnemyData> checkpoints = new List<PausedEnemyData>(enemies.Count);
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyAgent enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                checkpoints.Add(new PausedEnemyData
                {
                    Kind = enemy.Kind,
                    BossBehavior = enemy.BossBehavior,
                    LaneX = enemy.LaneX,
                    PositionX = enemy.transform.position.x,
                    PositionZ = enemy.transform.position.z,
                    HealthRatio = enemy.HealthRatio,
                    Health = enemy.CurrentHealth,
                    MaxHealth = enemy.MaxHealth,
                    Speed = enemy.Speed,
                    RangedTimer = enemy.RangedTimer,
                    BossTelegraphTimer = enemy.BossTelegraphTimer,
                    BossTelegraphArmed = enemy.BossTelegraphArmed,
                    ExactState = true
                });
            }

            return checkpoints.ToArray();
        }

        private PausedCardData[] CaptureCardCheckpoints()
        {
            List<PausedCardData> checkpoints = new List<PausedCardData>(cards.Count);
            for (int index = 0; index < cards.Count; index++)
            {
                TargetCard card = cards[index];
                if (card == null || !card.IsAlive)
                {
                    continue;
                }

                checkpoints.Add(new PausedCardData
                {
                    PairId = card.PairId,
                    Kind = card.Kind,
                    PositionX = card.PositionX,
                    PositionZ = card.PositionZ,
                    HealthRatio = card.HealthRatio,
                    Health = card.CurrentHealth,
                    MaxHealth = card.MaxHealth,
                    Speed = card.Speed,
                    ExactState = true
                });
            }

            return checkpoints.ToArray();
        }

        private void ClearPendingRun()
        {
            awaitingExplicitResume = false;
            if (progress == null || progress.PendingRun == null)
            {
                return;
            }

            progress.PendingRun = null;
            LocalProgress.Save(progress);
        }

        private void OpenStory(LevelDefinition level, bool isEndless)
        {
            Time.timeScale = 1f;
            paused = false;
            CleanupRunObjects();
            currentLevel = null;
            pendingLevel = level;
            pendingEndless = isEndless;
            state = GameFlowState.Story;
            world.Build(level, gameCamera);
            BeginCombatVisualWarmup(level);
            audioSystem.SetMusic("title", 0.28f);
            hud.ShowStory(level, isEndless);
        }

        private void BeginCombatVisualWarmup(LevelDefinition level)
        {
            if (combatVisualWarmupStarted || CombatVisualPoolsWarmed || level == null)
            {
                return;
            }

            combatVisualWarmupStarted = true;
            StartCoroutine(PrewarmCombatVisualsRoutine(level));
        }

        private IEnumerator PrewarmCombatVisualsRoutine(LevelDefinition level)
        {
            float difficulty = Mathf.Clamp(level.Difficulty, 0.1f, 1.65f);
            for (int index = 0; index < CombatWarmupEnemyKinds.Length; index++)
            {
                EnemyAgent enemy = CreateEnemyObject(CombatWarmupEnemyKinds[index]);
                enemy.Initialize(this, CombatWarmupEnemyKinds[index], difficulty, 0f, level.BossBehavior);
                UnregisterEnemy(enemy);
                ReleaseEnemy(enemy);
                CombatVisualWarmupObjects++;
                yield return null;
            }

            for (int index = 0; index < CombatWarmupCardKinds.Length; index++)
            {
                TargetCard card = CreateCardObject(CombatWarmupCardKinds[index], 9000 + index);
                card.Initialize(this, 9000 + index, CombatWarmupCardKinds[index], difficulty, level.CardPath);
                UnregisterCard(card);
                ReleaseCard(card);
                CombatVisualWarmupObjects++;
                yield return null;
            }

            CombatVisualPoolsWarmed = true;
            Debug.Log("NODNARB_STARTUP stage=combat_visual_pool_ready objects=" + CombatVisualWarmupObjects
                + " pooled_enemies=" + enemyPool.Count + " pooled_cards=" + cardPool.Count);
        }

        private void StartRun(LevelDefinition level, bool isEndless, PausedRunData snapshot)
        {
            CleanupRunObjects();
            attemptSequence++;
            currentLevel = level;
            endless = isEndless;
            paused = false;
            state = GameFlowState.Playing;
            Time.timeScale = 1f;
            model = new RunModel(snapshot == null ? RunModel.StartingSoldiers : snapshot.SoldierCount,
                snapshot == null ? progress.SelectedWeapon : snapshot.SelectedWeapon);
            if (snapshot != null)
            {
                model.Restore(snapshot);
            }
            bool replay = nextRunIsReplay;
            nextRunIsReplay = false;
            if (snapshot == null)
            {
                progress.RecordRunStart(isEndless, replay);
            }
            bool resumed = snapshot != null && snapshot.IsValid();
            randomSeed = resumed && snapshot.RandomSeed != 0
                ? snapshot.RandomSeed
                : level.Seed ^ attemptSequence * 7919;
            random = new System.Random(randomSeed);
            randomDrawCount = 0;
            if (resumed)
            {
                RestoreRandomState(snapshot.RandomDrawCount);
            }

            elapsed = resumed ? Mathf.Max(0f, snapshot.Elapsed) : 0f;
            if (!endless && resumed)
            {
                elapsed = Mathf.Min(elapsed, Mathf.Max(0f, level.DurationSeconds - 0.1f));
            }

            spawnTimer = resumed ? Mathf.Max(0f, snapshot.SpawnTimer) : currentLevel.OpeningDelaySeconds;
            cardTimer = resumed ? Mathf.Max(0f, snapshot.CardTimer) : currentLevel.CardStartDelaySeconds;
            fireTimer = resumed ? Mathf.Max(0f, snapshot.FireTimer) : 0.28f;
            hitAudioTimer = resumed ? Mathf.Max(0f, snapshot.HitAudioTimer) : 0f;
            pairSequence = resumed ? snapshot.PairSequence : 0;
            waveSequence = resumed ? snapshot.WaveSequence : 0;
            bossSpawned = false;
            bossAgent = null;
            endlessBossCycle = resumed ? snapshot.EndlessBossCycle : 0;
            LastVolleyShooterCount = 0;
            TotalFriendlyShotsFired = 0;
            TotalFriendlyHits = 0;
            TotalRecruitPickups = resumed ? Mathf.Max(0, snapshot.RecruitPickups) : 0;
            TotalWeaponPickups = resumed ? Mathf.Max(0, snapshot.WeaponPickups) : 0;
            TotalCardChoices = resumed ? Mathf.Max(0, snapshot.CardChoices) : 0;
            LastCardActivationSource = resumed ? snapshot.LastCardActivationSource : CardActivationSource.AutoFire;
            LastCardSelectionReport = resumed && !string.IsNullOrEmpty(snapshot.LastCardSelectionReport)
                ? snapshot.LastCardSelectionReport
                : string.Empty;
            TotalBossTelegraphs = 0;
            LastBudgetReport = string.Empty;
            peakEnemyCount = 0;
            peakCardCount = 0;
            worstFrameSeconds = 0f;
            frameSampleCount = 0;
            frameSampleWriteIndex = 0;
            overBudgetFrameCount = 0;
            frameTimeP95Seconds = 0f;
            nextLiveBudgetReportElapsed = LiveBudgetReportIntervalSeconds;
            modelSampleMilliseconds = 0f;
            inputSampleMilliseconds = 0f;
            volleySampleMilliseconds = 0f;
            waveSampleMilliseconds = 0f;
            cardSampleMilliseconds = 0f;
            modelWorstMilliseconds = 0f;
            inputWorstMilliseconds = 0f;
            volleyWorstMilliseconds = 0f;
            waveWorstMilliseconds = 0f;
            cardWorstMilliseconds = 0f;
            firstVolleyReported = false;
            lastGestureReport = string.Empty;
            LastBalanceReport = string.Empty;
            LastReadabilityReport = string.Empty;
            LastStabilityReport = string.Empty;
            comebackCardPairCount = 0;
            awaitingExplicitResume = false;
            world.Build(level, gameCamera);
            EmitVisualBudgetReport();
            audioSystem.SetMusic("combat", Mathf.Clamp01(0.30f + level.Difficulty * 0.55f));
            squad.Build(model, snapshot == null ? progress.SelectedSuit : snapshot.SelectedSuit);
            if (resumed)
            {
                squad.RestoreRelativePosition(snapshot.SquadRelativeX, snapshot.SquadTargetX,
                    LaneRoute.CenterX(level.Route, GameTheme.CaptainZ));
                RestoreCombatCheckpoints(snapshot);
            }
            projectiles.ClearActive();
            bool showOnboarding = !progress.OnboardingComplete;
            hud.ShowGameplay(showOnboarding);
            bool bossActive = bossAgent != null && bossAgent.IsAlive;
            hud.UpdateGameplay(model, currentLevel, elapsed, endless, bossActive, squad.ShooterCount, cards.Count > 0, SquadRelativeX, cardTimer);
            paused = resumed && snapshot.ResumePaused;
            awaitingExplicitResume = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused)
            {
                hud.ShowPause();
            }
            else
            {
                progress.PendingRun = null;
                LocalProgress.Save(progress);
            }
            Debug.Log("NODNARB_RUN_START level=" + level.Index + " endless=" + isEndless + " resumed=" + resumed
                + " seed=" + randomSeed);
            Debug.Log("NODNARB_STARTUP stage=gameplay_ready level=" + level.Index + " resumed=" + resumed);
            Debug.Log("NODNARB_REPLAY campaign_runs=" + progress.CampaignRuns
                + " endless_runs=" + progress.EndlessRuns
                + " replay_runs=" + progress.ReplayRuns
                + " replay=" + replay);
        }

        private void CompleteOnboarding()
        {
            if (progress == null || progress.OnboardingComplete)
            {
                return;
            }

            progress.OnboardingComplete = true;
            LocalProgress.Save(progress);
            Debug.Log("NODNARB_ONBOARDING complete=true");
        }

        private void RestoreRandomState(int drawCount)
        {
            int safeDrawCount = Mathf.Clamp(drawCount, 0, 1000000);
            for (int index = 0; index < safeDrawCount; index++)
            {
                random.NextDouble();
            }

            randomDrawCount = safeDrawCount;
        }

        private void RestoreCombatCheckpoints(PausedRunData snapshot)
        {
            bool restoredBoss = false;
            if (snapshot.Enemies != null)
            {
                for (int index = 0; index < snapshot.Enemies.Length; index++)
                {
                    PausedEnemyData checkpoint = snapshot.Enemies[index];
                    BossBehavior behavior = checkpoint.Kind == EnemyKind.Boss
                        ? checkpoint.ExactState
                            ? checkpoint.BossBehavior
                            : endless ? CampaignCatalog.GetEndlessBossBehavior(endlessBossCycle) : currentLevel.BossBehavior
                        : BossBehavior.Crusher;
                    EnemyAgent enemy = SpawnEnemy(checkpoint.Kind, checkpoint.LaneX, CurrentDifficulty(), behavior);
                    enemy.RestoreCheckpoint(checkpoint);
                    if (checkpoint.Kind == EnemyKind.Boss)
                    {
                        bossAgent = enemy;
                        restoredBoss = true;
                    }
                }
            }

            bossSpawned = restoredBoss || snapshot.BossSpawned;
            if (snapshot.Cards != null)
            {
                for (int index = 0; index < snapshot.Cards.Length; index++)
                {
                    PausedCardData checkpoint = snapshot.Cards[index];
                    TargetCard card = SpawnCard(checkpoint.PairId, checkpoint.Kind);
                    card.RestoreCheckpoint(checkpoint);
                }
            }

            peakEnemyCount = enemies.Count;
            peakCardCount = cards.Count;
        }

        private void FinishRun()
        {
            if (state != GameFlowState.Playing || model == null)
            {
                return;
            }

            state = GameFlowState.Result;
            Time.timeScale = 1f;
            paused = false;
            bool victory = model.EndReason == RunEndReason.ExtractionSecured;
            int earned = model.Salvage + Mathf.Max(2, model.Score / 180) + (victory ? 18 + currentLevel.Index * 3 : 3);
            if (endless)
            {
                progress.RecordEndlessResult(model.Score, earned);
            }
            else
            {
                progress.RecordCampaignResult(currentLevel.Index, model.Score, earned, victory);
            }

            progress.PendingRun = null;
            LocalProgress.Save(progress);
            lastResult = new ResultViewData
            {
                Reason = model.EndReason,
                Victory = victory,
                Endless = endless,
                Level = currentLevel.Index,
                Score = model.Score,
                Kills = model.Kills,
                Salvage = earned,
                Squad = model.SoldierCount,
                Overflow = model.OverflowRecruits,
                WeaponPickups = TotalWeaponPickups,
                RecruitPickups = TotalRecruitPickups,
                Elapsed = elapsed,
                CanAdvance = victory && !endless && currentLevel.Index < 10
            };

            if (victory)
            {
                audioSystem.Victory();
                hud.ShowAudioCaption("EXTRACTION SECURED");
            }
            else
            {
                audioSystem.Defeat();
                hud.ShowAudioCaption("SIGNAL LOST");
            }

            CalculateFramePercentile();
            LastBudgetReport = "NODNARB_BUDGET peak_enemies=" + peakEnemyCount + "/" + MaxActiveEnemiesIncludingBoss
                + " peak_cards=" + peakCardCount + "/" + MaxActiveCards
                + " runtime_ready_ms=" + runtimeReadyMilliseconds.ToString("F1")
                + " projectile_slots=" + (projectiles == null ? 0 : projectiles.TotalProjectileSlots)
                + " active_feedback=" + (projectiles == null ? 0 : projectiles.ActiveFeedbackCount)
                + " worst_frame_ms=" + (worstFrameSeconds * 1000f).ToString("F1")
                + " frame_p95_ms=" + (frameTimeP95Seconds * 1000f).ToString("F1")
                + " over_budget_frames=" + overBudgetFrameCount
                + " frame_samples=" + frameSampleCount
                + " model_ms=" + modelSampleMilliseconds.ToString("F1")
                + " model_worst_ms=" + modelWorstMilliseconds.ToString("F1")
                + " input_ms=" + inputSampleMilliseconds.ToString("F1")
                + " input_worst_ms=" + inputWorstMilliseconds.ToString("F1")
                + " volley_ms=" + volleySampleMilliseconds.ToString("F1")
                + " volley_worst_ms=" + volleyWorstMilliseconds.ToString("F1")
                + " wave_ms=" + waveSampleMilliseconds.ToString("F1")
                + " wave_worst_ms=" + waveWorstMilliseconds.ToString("F1")
                + " card_ms=" + cardSampleMilliseconds.ToString("F1")
                + " card_worst_ms=" + cardWorstMilliseconds.ToString("F1");
            Debug.Log(LastBudgetReport);
            LastBalanceReport = "NODNARB_BALANCE level=" + currentLevel.Index
                + " endless=" + endless
                + " victory=" + victory
                + " elapsed_s=" + elapsed.ToString("F1")
                + " difficulty=" + CurrentDifficulty().ToString("F2")
                + " kills=" + model.Kills
                + " weapon_pickups=" + TotalWeaponPickups
                + " crew_pickups=" + TotalRecruitPickups
                + " card_pairs=" + TotalCardPairsSpawned
                + " card_choices=" + TotalCardChoices
                + " final_squad=" + model.SoldierCount
                + " overflow=" + model.OverflowRecruits
                + " comeback_pairs=" + comebackCardPairCount
                + " replay_runs=" + progress.ReplayRuns
                + " credits=" + progress.Credits
                + " campaign_runs=" + progress.CampaignRuns
                + " campaign_victories=" + progress.CampaignVictories
                + " endless_runs=" + progress.EndlessRuns;
            Debug.Log(LastBalanceReport);
            LastReadabilityReport = "NODNARB_READABILITY peak_enemies=" + peakEnemyCount
                + " peak_cards=" + peakCardCount
                + " shots=" + TotalFriendlyShotsFired
                + " hits=" + TotalFriendlyHits
                + " card_choices=" + TotalCardChoices
                + " boss_telegraphs=" + TotalBossTelegraphs
                + " visible_squad=" + model.SoldierCount + "/" + RunModel.VisibleSoldierCap
                + " visible_shooters=" + model.VisibleShooterCount
                + " high_contrast=" + NodnarbSettings.HighContrastEnabled
                + " reduced_motion=" + NodnarbSettings.ReducedMotionEnabled
                + " captions=" + NodnarbSettings.CaptionsEnabled;
            Debug.Log(LastReadabilityReport);
            DeviceFeedback.Pulse(DeviceFeedbackKind.Result);
            projectiles.SpawnBurst(CaptainPosition + Vector3.up * 0.5f,
                victory ? GameTheme.AccessibleSignalBright : GameTheme.AccessibleDanger,
                victory ? 0.92f : 0.70f);

            projectiles.ClearActive();
            projectiles.TrimToWarmSet();
            LastStabilityReport = "NODNARB_STABILITY active_enemies=" + enemies.Count
                + " active_cards=" + cards.Count
                + " pooled_enemies=" + enemyPool.Count
                + " pooled_cards=" + cardPool.Count
                + " projectile_slots=" + projectiles.TotalProjectileSlots
                + " feedback_slots=" + projectiles.TotalFeedbackSlots;
            Debug.Log(LastStabilityReport);
            hud.ShowResult(lastResult);
        }

        private bool HandleCardSelectionInput()
        {
            if (squad == null || squad.IsDragging)
            {
                return false;
            }

            if (Input.touchCount > 0)
            {
                for (int index = 0; index < Input.touchCount; index++)
                {
                    Touch touch = Input.GetTouch(index);
                    if (touch.phase == TouchPhase.Began
                        && !squad.IsDragging)
                    {
                        bool pointerOverUi = IsPointerOverUi(touch.fingerId);
                        bool canBeginWorldGesture = NodnarbInputPolicy.CanBeginWorldGesture(
                            touch.position,
                            Screen.safeArea,
                            Screen.height,
                            pointerOverUi);
                        if (canBeginWorldGesture && TrySelectCardAtScreenPosition(touch.position))
                        {
                            lastGestureReport = "NODNARB_GESTURE pointer=touch" + touch.fingerId + " result=card_selected";
                            Debug.Log(lastGestureReport);
                            return true;
                        }

                        lastGestureReport = "NODNARB_GESTURE pointer=touch" + touch.fingerId
                            + " result=" + (pointerOverUi ? "ui_owned"
                                : NodnarbInputPolicy.IsInBottomActionRail(touch.position, Screen.safeArea, Screen.height)
                                    ? "action_rail"
                                    : "world_unclaimed");
                    }
                }

                return false;
            }

            if (Input.GetMouseButtonDown(0)
                && !squad.IsDragging)
            {
                bool pointerOverUi = IsPointerOverUi(-1);
                bool canBeginWorldGesture = NodnarbInputPolicy.CanBeginWorldGesture(
                    Input.mousePosition,
                    Screen.safeArea,
                    Screen.height,
                    pointerOverUi);
                if (canBeginWorldGesture && TrySelectCardAtScreenPosition(Input.mousePosition))
                {
                    lastGestureReport = "NODNARB_GESTURE pointer=mouse result=card_selected";
                    Debug.Log(lastGestureReport);
                    return true;
                }

                lastGestureReport = "NODNARB_GESTURE pointer=mouse result="
                    + (pointerOverUi ? "ui_owned"
                        : NodnarbInputPolicy.IsInBottomActionRail(Input.mousePosition, Screen.safeArea, Screen.height)
                            ? "action_rail"
                            : "world_unclaimed");
            }

            return false;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void CleanupRunObjects()
        {
            projectiles?.ClearActive();
            if (squad != null)
            {
                squad.Clear();
            }

            while (cards.Count > 0)
            {
                TargetCard card = cards[cards.Count - 1];
                if (card == null)
                {
                    cards.RemoveAt(cards.Count - 1);
                }
                else
                {
                    card.Cancel();
                }
            }

            while (enemies.Count > 0)
            {
                EnemyAgent enemy = enemies[enemies.Count - 1];
                if (enemy == null)
                {
                    enemies.RemoveAt(enemies.Count - 1);
                }
                else
                {
                    enemy.Cancel();
                }
            }

            model = null;
            bossAgent = null;
        }

        private void FireSquadVolley()
        {
            squad.GetMuzzlePositions(muzzlePositions);
            LastVolleyShooterCount = muzzlePositions.Count;
            TotalFriendlyShotsFired += muzzlePositions.Count;
            for (int i = 0; i < muzzlePositions.Count; i++)
            {
                Vector3 origin = muzzlePositions[i];
                projectiles.FireFriendly(origin, FindFriendlyTarget(origin, i), model.ShotDamage);
            }

            squad.PulseMuzzleFlash();
            squad.Recoil();
            audioSystem.Shot();
            if (!firstVolleyReported)
            {
                firstVolleyReported = true;
                Debug.Log("NODNARB_STARTUP stage=first_volley shooters=" + LastVolleyShooterCount);
            }
        }

        private Vector3 FindFriendlyTarget(Vector3 origin, int shooterIndex)
        {
            IFriendlyDamageable best = null;
            float bestScore = float.MaxValue;

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                TargetCard card = cards[index];
                if (card == null || !card.IsAlive || card.HitPosition.z < origin.z - 0.25f
                    || !CardChoice.IsSideAligned(card.Kind, SquadRelativeX))
                {
                    continue;
                }

                Vector3 target = card.HitPosition;
                float score = (target - origin).sqrMagnitude * 0.78f
                    + Mathf.Abs(target.x - origin.x) * 0.40f
                    + CardChoice.TargetBias(card.Kind, SquadRelativeX);
                if (score < bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            for (int index = enemies.Count - 1; index >= 0; index--)
            {
                EnemyAgent enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || enemy.HitPosition.z < origin.z - 0.25f)
                {
                    continue;
                }

                Vector3 target = enemy.HitPosition;
                float sideBias = Mathf.Abs(target.x - origin.x) * (shooterIndex == 0 ? 0.18f : 0.12f);
                float score = (target - origin).sqrMagnitude + sideBias;
                if (score < bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }

            return best == null ? origin + Vector3.forward * 18f : best.HitPosition;
        }

        private void SpawnWave()
        {
            int normalEnemyCount = enemies.Count - (bossAgent != null && bossAgent.IsAlive ? 1 : 0);
            if (normalEnemyCount >= MaxActiveNormalEnemies)
            {
                spawnTimer = Mathf.Max(0.6f, GetWaveInterval(currentLevel.Pattern, CurrentDifficulty()) * 0.5f);
                return;
            }

            float difficulty = CurrentDifficulty();
            float progress01 = endless ? Mathf.Clamp01(elapsed / 150f) : Mathf.Clamp01(elapsed / Mathf.Max(1f, currentLevel.DurationSeconds));
            waveSequence++;
            int batch = ResolveWaveBatch(currentLevel.Pattern, progress01, difficulty, waveSequence);
            batch = Mathf.Min(batch, MaxActiveNormalEnemies - normalEnemyCount);

            for (int i = 0; i < batch; i++)
            {
                EnemyKind kind = ChooseEnemyKind(currentLevel.Pattern, progress01, difficulty, waveSequence);
                float x = ResolveWaveX(currentLevel.Pattern, waveSequence, i);
                SpawnEnemy(kind, x, difficulty);
            }

            audioSystem.Wave(currentLevel.Ecology, batch);

            float baseInterval = GetWaveInterval(currentLevel.Pattern, difficulty);
            spawnTimer = baseInterval * Range(0.82f, 1.18f);
        }

        private int ResolveWaveBatch(WavePattern pattern, float progress01, float difficulty, int sequence)
        {
            if (currentLevel != null)
            {
                switch (currentLevel.Ecology)
                {
                    case EcologyProfile.BroodSurge:
                    case EcologyProfile.ShardSwarm:
                        return sequence % 3 == 0 ? 4 : 2;
                    case EcologyProfile.CarrierRing:
                        return sequence % 4 == 0 ? 3 : 2;
                    case EcologyProfile.CanyonAmbush:
                        return sequence % 3 == 0 ? 3 : 1;
                }
            }

            switch (pattern)
            {
                case WavePattern.RushLanes:
                    return progress01 > 0.22f && sequence % 3 == 0 ? 2 : 1;
                case WavePattern.Crossfire:
                    return progress01 > 0.18f && sequence % 2 == 0 ? 2 : 1;
                case WavePattern.ArmorColumns:
                    return 2;
                case WavePattern.SwarmPulse:
                    if (sequence % 3 == 0)
                    {
                        return 3;
                    }

                    return NextRandomDouble() < 0.55f ? 2 : 1;
                default:
                    int batch = NextRandomDouble() < difficulty * 0.58f ? 2 : 1;
                    if (NextRandomDouble() < progress01 * 0.36f)
                    {
                        batch++;
                    }

                    return batch;
            }
        }

        private float ResolveWaveX(WavePattern pattern, int sequence, int index)
        {
            if (currentLevel != null)
            {
                switch (currentLevel.Ecology)
                {
                    case EcologyProfile.CanyonAmbush:
                        return (sequence + index) % 2 == 0 ? Range(-3.7f, -1.6f) : Range(1.6f, 3.7f);
                    case EcologyProfile.SporeBloom:
                        return Mathf.Sin((sequence + index) * 1.7f) * 2.9f + Range(-0.35f, 0.35f);
                    case EcologyProfile.SnowStalker:
                        return (index % 2 == 0 ? -1f : 1f) * Range(1.2f, 3.7f);
                    case EcologyProfile.BroodSurge:
                        return Range(-3.85f, 3.85f);
                    case EcologyProfile.ShardSwarm:
                        return (index % 2 == 0 ? -1f : 1f) * Range(2.0f, 3.8f);
                    case EcologyProfile.CarrierRing:
                        return Mathf.Sin(sequence * 0.8f + index) * 3.4f;
                }
            }

            switch (pattern)
            {
                case WavePattern.RushLanes:
                    int rushSide = (sequence + index) % 2 == 0 ? -1 : 1;
                    return rushSide * Range(2.05f, 3.25f);
                case WavePattern.Crossfire:
                    return index % 2 == 0 ? Range(-3.45f, -2.55f) : Range(2.55f, 3.45f);
                case WavePattern.ArmorColumns:
                    return (index % 2 == 0 ? -2.25f : 2.25f) + Range(-0.32f, 0.32f);
                case WavePattern.SwarmPulse:
                    return Range(-3.3f, 3.3f);
                default:
                    return Range(-3.55f, 3.55f);
            }
        }

        private float GetWaveInterval(WavePattern pattern, float difficulty)
        {
            float baseInterval = Mathf.Lerp(1.45f, 0.58f, Mathf.Clamp01(difficulty));
            switch (pattern)
            {
                case WavePattern.RushLanes:
                    return baseInterval * 0.92f * CurrentWaveCadence();
                case WavePattern.Crossfire:
                    return baseInterval * 1.08f * CurrentWaveCadence();
                case WavePattern.ArmorColumns:
                    return baseInterval * 1.20f * CurrentWaveCadence();
                case WavePattern.SwarmPulse:
                    return baseInterval * 0.64f * CurrentWaveCadence();
                default:
                    return baseInterval * 0.82f * CurrentWaveCadence();
            }
        }

        private float CurrentWaveCadence()
        {
            return currentLevel == null ? 1f : currentLevel.WaveCadenceMultiplier;
        }

        private EnemyKind ChooseEnemyKind(WavePattern pattern, float progress01, float difficulty, int sequence)
        {
            double roll = NextRandomDouble();
            if (currentLevel != null)
            {
                switch (currentLevel.Ecology)
                {
                    case EcologyProfile.CrashNest:
                        return roll < 0.16 && progress01 > 0.35f ? EnemyKind.Armored : EnemyKind.Melee;
                    case EcologyProfile.CanyonAmbush:
                        return sequence % 3 == 0 && roll < 0.48 ? EnemyKind.Ranged : EnemyKind.Melee;
                    case EcologyProfile.SporeBloom:
                        return roll < 0.42 ? EnemyKind.Ranged : EnemyKind.Melee;
                    case EcologyProfile.SnowStalker:
                        return roll < 0.24 ? EnemyKind.Ranged : EnemyKind.Melee;
                    case EcologyProfile.RelayNest:
                        return roll < 0.62 ? EnemyKind.Ranged : EnemyKind.Armored;
                    case EcologyProfile.ShardSwarm:
                        return roll < 0.30 ? EnemyKind.Armored : EnemyKind.Melee;
                    case EcologyProfile.BroodSurge:
                        return roll < 0.12 ? EnemyKind.Ranged : EnemyKind.Melee;
                    case EcologyProfile.NightPack:
                        return roll < 0.32 ? EnemyKind.Ranged : EnemyKind.Melee;
                    case EcologyProfile.BeaconSiege:
                        return roll < 0.45 ? EnemyKind.Ranged : EnemyKind.Armored;
                    case EcologyProfile.CarrierRing:
                        return roll < 0.40 ? EnemyKind.Armored : EnemyKind.Melee;
                }
            }

            switch (pattern)
            {
                case WavePattern.RushLanes:
                    if (progress01 > 0.42f && sequence % 5 == 0 && roll < 0.24)
                    {
                        return EnemyKind.Ranged;
                    }

                    return EnemyKind.Melee;
                case WavePattern.Crossfire:
                    if (progress01 > 0.10f && sequence % 2 == 0 && roll < 0.70)
                    {
                        return EnemyKind.Ranged;
                    }

                    return roll > 0.82 ? EnemyKind.Armored : EnemyKind.Melee;
                case WavePattern.ArmorColumns:
                    if (roll < 0.64 + progress01 * 0.08f)
                    {
                        return EnemyKind.Armored;
                    }

                    return roll > 0.82 ? EnemyKind.Ranged : EnemyKind.Melee;
                case WavePattern.SwarmPulse:
                    return roll > 0.86 && progress01 > 0.30f ? EnemyKind.Ranged : EnemyKind.Melee;
                case WavePattern.SiegeMix:
                    if (roll < 0.28 + difficulty * 0.05f)
                    {
                        return EnemyKind.Armored;
                    }

                    if (roll < 0.58 + difficulty * 0.05f)
                    {
                        return EnemyKind.Ranged;
                    }

                    return EnemyKind.Melee;
                default:
                    if (progress01 > 0.32f && roll < 0.18)
                    {
                        return EnemyKind.Ranged;
                    }

                    return EnemyKind.Melee;
            }
        }

        private void SpawnBoss()
        {
            if (bossAgent != null && bossAgent.IsAlive)
            {
                return;
            }

            while (enemies.Count >= MaxActiveEnemiesIncludingBoss)
            {
                EnemyAgent removable = null;
                for (int index = 0; index < enemies.Count; index++)
                {
                    if (enemies[index] != null && !enemies[index].IsBoss)
                    {
                        removable = enemies[index];
                        break;
                    }
                }

                if (removable == null)
                {
                    break;
                }

                removable.Cancel();
            }

            bossSpawned = true;
            BossBehavior behavior = endless
                ? CampaignCatalog.GetEndlessBossBehavior(endlessBossCycle)
                : currentLevel.BossBehavior;
            bossAgent = SpawnEnemy(EnemyKind.Boss, Range(-1.25f, 1.25f), CurrentDifficulty(), behavior);
        }

        private EnemyAgent SpawnEnemy(EnemyKind kind, float x, float difficulty)
        {
            return SpawnEnemy(kind, x, difficulty, BossBehavior.Crusher);
        }

        private EnemyAgent SpawnEnemy(EnemyKind kind, float x, float difficulty, BossBehavior bossBehavior)
        {
            EnemyAgent enemy = null;
            for (int index = enemyPool.Count - 1; index >= 0; index--)
            {
                EnemyAgent candidate = enemyPool[index];
                if (candidate == null)
                {
                    enemyPool.RemoveAt(index);
                    continue;
                }

                if (candidate.Kind == kind)
                {
                    enemy = candidate;
                    enemyPool.RemoveAt(index);
                    break;
                }
            }

            if (enemy == null)
            {
                enemy = CreateEnemyObject(kind);
            }

            enemy.Initialize(this, kind, difficulty, x, bossBehavior);
            return enemy;
        }

        private EnemyAgent CreateEnemyObject(EnemyKind kind)
        {
            GameObject enemyObject = new GameObject(kind.ToString());
            enemyObject.transform.SetParent(transform, false);
            return enemyObject.AddComponent<EnemyAgent>();
        }

        private void SpawnCardPair()
        {
            if (cards.Count > 0)
            {
                cardTimer = 1.4f;
                return;
            }

            pairSequence++;
            SpawnCard(pairSequence, CardKind.Weapon);
            SpawnCard(pairSequence, CardKind.Recruit);
            bool comebackAssist = model != null && model.TryConsumeComebackAssist();
            float cadence = comebackAssist ? 0.72f : 1f;
            if (comebackAssist)
            {
                comebackCardPairCount++;
            }

            cardTimer = Mathf.Max(4.6f, Range(currentLevel.CardIntervalMinSeconds, currentLevel.CardIntervalMaxSeconds) * cadence);
        }

        private TargetCard SpawnCard(int pairId, CardKind kind)
        {
            return SpawnCard(pairId, kind, CurrentDifficulty(), currentLevel.CardPath);
        }

        private TargetCard SpawnCard(int pairId, CardKind kind, float difficulty, CardPathPattern cardPath)
        {
            TargetCard card = null;
            for (int index = cardPool.Count - 1; index >= 0; index--)
            {
                TargetCard candidate = cardPool[index];
                if (candidate == null)
                {
                    cardPool.RemoveAt(index);
                    continue;
                }

                if (candidate.Kind == kind)
                {
                    card = candidate;
                    cardPool.RemoveAt(index);
                    break;
                }
            }

            if (card == null)
            {
                card = CreateCardObject(kind, pairId);
            }

            card.name = kind + "CardPair_" + pairId;
            card.Initialize(this, pairId, kind, difficulty, cardPath);
            return card;
        }

        private TargetCard CreateCardObject(CardKind kind, int pairId)
        {
            GameObject cardObject = new GameObject(kind + "CardPair_" + pairId);
            cardObject.transform.SetParent(transform, false);
            return cardObject.AddComponent<TargetCard>();
        }

        private void SelectWeapon(int direction)
        {
            int highest = progress.HighestCompletedLevel();
            int candidate = progress.SelectedWeapon;
            for (int attempt = 0; attempt < LoadoutCatalog.Weapons.Length; attempt++)
            {
                candidate = (candidate + direction + LoadoutCatalog.Weapons.Length) % LoadoutCatalog.Weapons.Length;
                if (LoadoutCatalog.IsWeaponUnlocked(candidate, highest))
                {
                    progress.SelectedWeapon = candidate;
                    LocalProgress.Save(progress);
                    break;
                }
            }

            hud.RefreshLoadout(progress);
        }

        private void SelectSuit(int direction)
        {
            int highest = progress.HighestCompletedLevel();
            int candidate = progress.SelectedSuit;
            for (int attempt = 0; attempt < LoadoutCatalog.Suits.Length; attempt++)
            {
                candidate = (candidate + direction + LoadoutCatalog.Suits.Length) % LoadoutCatalog.Suits.Length;
                if (LoadoutCatalog.IsSuitUnlocked(candidate, highest))
                {
                    progress.SelectedSuit = candidate;
                    LocalProgress.Save(progress);
                    break;
                }
            }

            hud.RefreshLoadout(progress);
        }

        private float CurrentDifficulty()
        {
            float ramp = endless ? Mathf.Min(0.95f, elapsed / 240f) : Mathf.Clamp01(elapsed / Mathf.Max(1f, currentLevel.DurationSeconds)) * currentLevel.DifficultyRamp;
            return Mathf.Clamp(currentLevel.Difficulty + ramp, 0.1f, 1.65f);
        }

        private void RecordFrameTime(float seconds)
        {
            worstFrameSeconds = Mathf.Max(worstFrameSeconds, seconds);
            if (seconds > TargetFrameSeconds)
            {
                overBudgetFrameCount++;
            }

            frameSamples[frameSampleWriteIndex] = seconds;
            frameSampleWriteIndex = (frameSampleWriteIndex + 1) % FrameSampleCapacity;
            frameSampleCount = Mathf.Min(FrameSampleCapacity, frameSampleCount + 1);
        }

        private static void BeginRuntimeSample(string sampleName)
        {
#if DEVELOPMENT_BUILD
            Profiler.BeginSample(sampleName);
#endif
        }

        private static void EndRuntimeSample()
        {
#if DEVELOPMENT_BUILD
            Profiler.EndSample();
#endif
        }

        private void CalculateFramePercentile()
        {
            if (frameSampleCount <= 0)
            {
                frameTimeP95Seconds = 0f;
                return;
            }

            int oldestIndex = frameSampleCount == FrameSampleCapacity ? frameSampleWriteIndex : 0;
            for (int index = 0; index < frameSampleCount; index++)
            {
                frameSortScratch[index] = frameSamples[(oldestIndex + index) % FrameSampleCapacity];
            }

            Array.Sort(frameSortScratch, 0, frameSampleCount);
            int percentileIndex = Mathf.Clamp(Mathf.CeilToInt(frameSampleCount * 0.95f) - 1, 0, frameSampleCount - 1);
            frameTimeP95Seconds = frameSortScratch[percentileIndex];
        }

        private void EmitLiveBudgetReport()
        {
            CalculateFramePercentile();
            LastBudgetReport = "NODNARB_BUDGET phase=live elapsed_s=" + elapsed.ToString("F1")
                + " peak_enemies=" + peakEnemyCount + "/" + MaxActiveEnemiesIncludingBoss
                + " peak_cards=" + peakCardCount + "/" + MaxActiveCards
                + " projectile_slots=" + (projectiles == null ? 0 : projectiles.TotalProjectileSlots)
                + " active_feedback=" + (projectiles == null ? 0 : projectiles.ActiveFeedbackCount)
                + " worst_frame_ms=" + (worstFrameSeconds * 1000f).ToString("F1")
                + " frame_p95_ms=" + (frameTimeP95Seconds * 1000f).ToString("F1")
                + " over_budget_frames=" + overBudgetFrameCount
                + " frame_samples=" + frameSampleCount;
            Debug.Log(LastBudgetReport);
            nextLiveBudgetReportElapsed = elapsed + LiveBudgetReportIntervalSeconds;
        }

        private float Range(float min, float max)
        {
            return min + (float)NextRandomDouble() * (max - min);
        }

        private double NextRandomDouble()
        {
            randomDrawCount++;
            return random.NextDouble();
        }

        private void PlayHitTick()
        {
            if (hitAudioTimer <= 0f)
            {
                hitAudioTimer = 0.055f;
                audioSystem.Hit();
            }
        }

        private static bool Overlaps(Vector3 first, Vector3 second, float radius)
        {
            float deltaX = first.x - second.x;
            float deltaZ = first.z - second.z;
            return deltaX * deltaX + deltaZ * deltaZ <= radius * radius;
        }
    }
}
