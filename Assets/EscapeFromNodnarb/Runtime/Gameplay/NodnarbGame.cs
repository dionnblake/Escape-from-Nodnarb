using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private static NodnarbGame instance;

        private readonly List<EnemyAgent> enemies = new List<EnemyAgent>(40);
        private readonly List<TargetCard> cards = new List<TargetCard>(8);
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
        private ResultViewData lastResult;
        private bool runtimeReady;
        private bool runtimeInitializationStarted;
        private int previousSleepTimeout = SleepTimeout.SystemSetting;

        public static NodnarbGame Instance
        {
            get { return instance; }
        }

        public bool CombatActive
        {
            get { return state == GameFlowState.Playing && !paused && model != null && !model.IsEnded; }
        }

        public float SquadX
        {
            get { return squad == null ? 0f : squad.X; }
        }

        public float SquadRelativeX
        {
            get { return squad == null ? 0f : squad.RelativeX; }
        }

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

        public int TotalBossTelegraphs { get; private set; }

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
            get { return currentLevel == null ? CampaignCatalog.Get(selectedLevel).Route : currentLevel.Route; }
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
            Input.multiTouchEnabled = false;

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
            CreateProjectilePool();
            CreateAudioSystem();
            hud.Initialize(this);
            LoadProgress();
            runtimeReady = true;
            ShowTitle();
        }

        private IEnumerator EnsureRuntimeReadyRoutine()
        {
            if (runtimeReady || runtimeInitializationStarted)
            {
                yield break;
            }

            runtimeInitializationStarted = true;
            CreateProjectilePool();
            yield return null;

            CreateAudioSystem();
            yield return null;

            yield return hud.InitializeRoutine(this);
            yield return null;

            LoadProgress();
            runtimeReady = true;
            ShowTitle();
        }

        private void CreateProjectilePool()
        {
            GameObject projectileObject = new GameObject("ProjectilePool");
            projectileObject.transform.SetParent(transform, false);
            projectiles = projectileObject.AddComponent<ProjectilePool>();
            projectiles.Initialize(this);
        }

        private void CreateAudioSystem()
        {
            GameObject audioObject = new GameObject("ProceduralAudio");
            audioObject.transform.SetParent(transform, false);
            audioSystem = audioObject.AddComponent<ProceduralAudio>();
            audioSystem.Initialize();
        }

        private void LoadProgress()
        {
            progress = LocalProgress.Load();
            selectedLevel = progress.UnlockedLevel;
        }

        private void Update()
        {
            if (!runtimeReady)
            {
                return;
            }

            HandleBackAndPauseInput();
            if (state == GameFlowState.Playing && model != null && model.IsEnded)
            {
                FinishRun();
                return;
            }

            if (!CombatActive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            hitAudioTimer = Mathf.Max(0f, hitAudioTimer - deltaTime);
            model.Tick(deltaTime);
            squad.Refresh(model.SoldierCount);
            squad.TickInput(deltaTime, false, LaneRoute.CenterX(CurrentRoute, GameTheme.CaptainZ));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ActivateRapidFire();
            }

            fireTimer -= deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer += model.FireInterval;
                FireSquadVolley();
            }

            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f && (endless || elapsed < currentLevel.DurationSeconds - 3f))
            {
                SpawnWave();
            }

            cardTimer -= deltaTime;
            if (cardTimer <= 0f)
            {
                SpawnCardPair();
            }

            if (!endless)
            {
                if (!bossSpawned && elapsed >= currentLevel.DurationSeconds - 12f)
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
            hud.UpdateGameplay(model, currentLevel, elapsed, endless, bossActive, squad.ShooterCount);
            if (model.IsEnded)
            {
                FinishRun();
            }
        }

        private void OnApplicationPause(bool applicationPaused)
        {
            if (applicationPaused && state == GameFlowState.Playing && !paused)
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
            Time.timeScale = 1f;
            paused = false;
            state = GameFlowState.Title;
            CleanupRunObjects();
            currentLevel = null;
            pendingLevel = null;
            progress = LocalProgress.Load();
            selectedLevel = Mathf.Clamp(progress.UnlockedLevel, 1, 10);
            // The title artwork is already a full-screen backdrop. Avoid building
            // the imported terrain set before the first Android frame is stable.
            // The selected sector is built when the player opens its story screen.
            world.Clear();
            gameCamera.backgroundColor = GameTheme.Void;
            hud.ShowTitle(progress);
        }

        public void ContinueCampaign()
        {
            EnsureRuntimeReady();
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
            hud.ShowLevelSelect(progress, selectedLevel);
        }

        public void SelectPreviousLevel()
        {
            selectedLevel = selectedLevel <= 1 ? 10 : selectedLevel - 1;
            world.Build(CampaignCatalog.Get(selectedLevel), gameCamera);
            hud.RefreshLevelSelect(progress, selectedLevel);
        }

        public void SelectNextLevel()
        {
            selectedLevel = selectedLevel >= 10 ? 1 : selectedLevel + 1;
            world.Build(CampaignCatalog.Get(selectedLevel), gameCamera);
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

            StartRun(pendingLevel, pendingEndless);
        }

        public void ActivateRapidFire()
        {
            if (CombatActive && model.ActivateRapidFire())
            {
                audioSystem.Ability();
            }
        }

        public void TogglePause()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused)
            {
                hud.ShowPause();
            }
            else
            {
                hud.HidePause();
            }
        }

        public void RetryRun()
        {
            Time.timeScale = 1f;
            paused = false;
            StartRun(currentLevel, endless);
        }

        public void AbortRun()
        {
            Time.timeScale = 1f;
            paused = false;
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
            OpenStory(replayLevel, lastResult.Endless);
        }

        public void AdvanceAfterResult()
        {
            if (lastResult == null || !lastResult.CanAdvance)
            {
                return;
            }

            OpenStory(CampaignCatalog.Get(lastResult.Level + 1), false);
        }

        public void RegisterEnemy(EnemyAgent enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
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

        public void RegisterCard(TargetCard card)
        {
            if (card != null && !cards.Contains(card))
            {
                cards.Add(card);
            }
        }

        public void UnregisterCard(TargetCard card)
        {
            cards.Remove(card);
        }

        public void OnEnemyKilled(EnemyAgent enemy, int score, int salvage)
        {
            if (model == null || model.IsEnded)
            {
                return;
            }

            model.RegisterKill(score, salvage);
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
            if (!CombatActive || activated == null)
            {
                return;
            }

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
            }
            else
            {
                model.UpgradeWeapon();
                TotalWeaponPickups++;
                audioSystem.Upgrade();
            }

            model.AddScore(50);
            hud.ShowPickup(activated.Kind, model);
        }

        public void OnFrontLineBreached(float contactDamage)
        {
            if (!CombatActive)
            {
                return;
            }

            model.BreachFrontLine();
            hud.PulseDamage();
            audioSystem.Damage();
        }

        public void FireHostile(Vector3 origin, Vector3 target, float damage)
        {
            if (CombatActive)
            {
                projectiles.FireHostile(origin, target, damage);
            }
        }

        public void NotifyBossTelegraph()
        {
            if (currentLevel == null)
            {
                return;
            }

            TotalBossTelegraphs++;
            hud.ShowBossTelegraph(currentLevel.BossName);
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
                if (card == null || !card.IsAlive)
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
            hud.PulseDamage();
            audioSystem.Damage();
            return true;
        }

        private void BuildCameraAndLight()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Destroy(cameras[i].gameObject);
            }

            GameObject cameraObject = new GameObject("GameCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 9.25f, -9.35f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.40f, 5.60f));
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.fieldOfView = 49f;
            gameCamera.nearClipPlane = 0.2f;
            gameCamera.farClipPlane = 60f;
            gameCamera.allowHDR = false;
            gameCamera.allowMSAA = true;
            cameraObject.tag = "MainCamera";

            GameObject lightObject = new GameObject("SignalLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.Lerp(GameTheme.Text, GameTheme.Signal, 0.10f);
            light.intensity = 1.28f;
            light.shadows = LightShadows.None;

            GameObject rimObject = new GameObject("CanyonRimLight");
            rimObject.transform.SetParent(transform, false);
            rimObject.transform.rotation = Quaternion.Euler(32f, 152f, 0f);
            Light rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = Color.Lerp(GameTheme.CanyonHighlight, GameTheme.Text, 0.24f);
            rim.intensity = 0.72f;
            rim.shadows = LightShadows.None;
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
            hud.ShowStory(level, isEndless);
        }

        private void StartRun(LevelDefinition level, bool isEndless)
        {
            CleanupRunObjects();
            attemptSequence++;
            currentLevel = level;
            endless = isEndless;
            paused = false;
            state = GameFlowState.Playing;
            Time.timeScale = 1f;
            model = new RunModel(RunModel.StartingSoldiers, progress.SelectedWeapon);
            random = new System.Random(level.Seed ^ attemptSequence * 7919);
            elapsed = 0f;
            spawnTimer = 0.65f;
            cardTimer = 3.2f;
            fireTimer = 0.28f;
            pairSequence = 0;
            waveSequence = 0;
            bossSpawned = false;
            bossAgent = null;
            endlessBossCycle = 0;
            LastVolleyShooterCount = 0;
            TotalFriendlyShotsFired = 0;
            TotalFriendlyHits = 0;
            TotalRecruitPickups = 0;
            TotalWeaponPickups = 0;
            TotalBossTelegraphs = 0;
            world.Build(level, gameCamera);
            squad.Build(model, progress.SelectedSuit);
            projectiles.ClearActive();
            hud.ShowGameplay();
            hud.UpdateGameplay(model, currentLevel, elapsed, endless, false, squad.ShooterCount);
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
                Elapsed = elapsed,
                CanAdvance = victory && !endless && currentLevel.Index < 10
            };

            if (victory)
            {
                audioSystem.Victory();
            }
            else
            {
                audioSystem.Defeat();
            }

            projectiles.ClearActive();
            hud.ShowResult(lastResult);
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
        }

        private Vector3 FindFriendlyTarget(Vector3 origin, int shooterIndex)
        {
            IFriendlyDamageable best = null;
            float bestScore = float.MaxValue;

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                TargetCard card = cards[index];
                if (card == null || !card.IsAlive || card.HitPosition.z < origin.z - 0.25f)
                {
                    continue;
                }

                Vector3 target = card.HitPosition;
                float score = (target - origin).sqrMagnitude * 0.78f + (card.Kind == CardKind.Recruit ? -0.20f : 0f);
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
            float difficulty = CurrentDifficulty();
            float progress01 = endless ? Mathf.Clamp01(elapsed / 150f) : Mathf.Clamp01(elapsed / Mathf.Max(1f, currentLevel.DurationSeconds));
            waveSequence++;
            int batch = ResolveWaveBatch(currentLevel.Pattern, progress01, difficulty, waveSequence);

            if (enemies.Count > 24)
            {
                batch = 1;
            }

            for (int i = 0; i < batch; i++)
            {
                EnemyKind kind = ChooseEnemyKind(currentLevel.Pattern, progress01, difficulty, waveSequence);
                float x = ResolveWaveX(currentLevel.Pattern, waveSequence, i);
                SpawnEnemy(kind, x, difficulty);
            }

            float baseInterval = GetWaveInterval(currentLevel.Pattern, difficulty);
            spawnTimer = baseInterval * Range(0.82f, 1.18f);
        }

        private int ResolveWaveBatch(WavePattern pattern, float progress01, float difficulty, int sequence)
        {
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

                    return random.NextDouble() < 0.55f ? 2 : 1;
                default:
                    int batch = random.NextDouble() < difficulty * 0.58f ? 2 : 1;
                    if (random.NextDouble() < progress01 * 0.36f)
                    {
                        batch++;
                    }

                    return batch;
            }
        }

        private float ResolveWaveX(WavePattern pattern, int sequence, int index)
        {
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
                    return baseInterval * 0.92f;
                case WavePattern.Crossfire:
                    return baseInterval * 1.08f;
                case WavePattern.ArmorColumns:
                    return baseInterval * 1.20f;
                case WavePattern.SwarmPulse:
                    return baseInterval * 0.64f;
                default:
                    return baseInterval * 0.82f;
            }
        }

        private EnemyKind ChooseEnemyKind(WavePattern pattern, float progress01, float difficulty, int sequence)
        {
            double roll = random.NextDouble();
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
            GameObject enemyObject = new GameObject(kind.ToString());
            enemyObject.transform.SetParent(transform, false);
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Initialize(this, kind, difficulty, x, bossBehavior);
            return enemy;
        }

        private void SpawnCardPair()
        {
            if (cards.Count > 0)
            {
                cardTimer = 1.4f;
                return;
            }

            pairSequence++;
            GameObject weaponObject = new GameObject("WeaponCardPair_" + pairSequence);
            weaponObject.transform.SetParent(transform, false);
            weaponObject.AddComponent<TargetCard>().Initialize(this, pairSequence, CardKind.Weapon, CurrentDifficulty(), currentLevel.CardPath);

            GameObject recruitObject = new GameObject("RecruitCardPair_" + pairSequence);
            recruitObject.transform.SetParent(transform, false);
            recruitObject.AddComponent<TargetCard>().Initialize(this, pairSequence, CardKind.Recruit, CurrentDifficulty(), currentLevel.CardPath);
            cardTimer = Range(8.2f, 11.2f);
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
            float ramp = endless ? Mathf.Min(0.95f, elapsed / 240f) : Mathf.Clamp01(elapsed / Mathf.Max(1f, currentLevel.DurationSeconds)) * 0.18f;
            return Mathf.Clamp(currentLevel.Difficulty + ramp, 0.1f, 1.65f);
        }

        private float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
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
