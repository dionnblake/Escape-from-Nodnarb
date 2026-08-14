using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EscapeFromNodnarb.Tests
{
    public sealed class BootstrapSmokeTests
    {
        private string savedProgress;
        private string savedProgressBackup;
        private int savedSoundSetting;
        private int savedMusicSetting;
        private int savedHapticsSetting;
        private int savedCaptionsSetting;
        private int savedHighContrastSetting;
        private int savedReducedMotionSetting;

        [SetUp]
        public void PreserveLocalProgress()
        {
            savedProgress = PlayerPrefs.GetString(LocalProgress.SaveKey, string.Empty);
            savedProgressBackup = PlayerPrefs.GetString(LocalProgress.BackupSaveKey, string.Empty);
            savedSoundSetting = PlayerPrefs.GetInt(NodnarbSettings.SoundKey, -1);
            savedMusicSetting = PlayerPrefs.GetInt(NodnarbSettings.MusicKey, -1);
            savedHapticsSetting = PlayerPrefs.GetInt(NodnarbSettings.HapticsKey, -1);
            savedCaptionsSetting = PlayerPrefs.GetInt(NodnarbSettings.CaptionsKey, -1);
            savedHighContrastSetting = PlayerPrefs.GetInt(NodnarbSettings.HighContrastKey, -1);
            savedReducedMotionSetting = PlayerPrefs.GetInt(NodnarbSettings.ReducedMotionKey, -1);
            PlayerPrefs.SetInt(NodnarbSettings.SoundKey, 1);
            PlayerPrefs.SetInt(NodnarbSettings.MusicKey, 1);
            PlayerPrefs.SetInt(NodnarbSettings.HapticsKey, 1);
            PlayerPrefs.SetInt(NodnarbSettings.CaptionsKey, 1);
            PlayerPrefs.SetInt(NodnarbSettings.HighContrastKey, 0);
            PlayerPrefs.SetInt(NodnarbSettings.ReducedMotionKey, 0);
        }

        [TearDown]
        public void RestoreLocalProgress()
        {
            if (string.IsNullOrEmpty(savedProgress))
            {
                PlayerPrefs.DeleteKey(LocalProgress.SaveKey);
            }
            else
            {
                PlayerPrefs.SetString(LocalProgress.SaveKey, savedProgress);
            }

            if (string.IsNullOrEmpty(savedProgressBackup))
            {
                PlayerPrefs.DeleteKey(LocalProgress.BackupSaveKey);
            }
            else
            {
                PlayerPrefs.SetString(LocalProgress.BackupSaveKey, savedProgressBackup);
            }

            RestoreSetting(NodnarbSettings.SoundKey, savedSoundSetting);
            RestoreSetting(NodnarbSettings.MusicKey, savedMusicSetting);
            RestoreSetting(NodnarbSettings.HapticsKey, savedHapticsSetting);
            RestoreSetting(NodnarbSettings.CaptionsKey, savedCaptionsSetting);
            RestoreSetting(NodnarbSettings.HighContrastKey, savedHighContrastSetting);
            RestoreSetting(NodnarbSettings.ReducedMotionKey, savedReducedMotionSetting);

            PlayerPrefs.Save();
        }

        private static void RestoreSetting(string key, int value)
        {
            if (value < 0)
            {
                PlayerPrefs.DeleteKey(key);
            }
            else
            {
                PlayerPrefs.SetInt(key, value);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeBootCreatesTitleFlow()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            Assert.That(game, Is.Not.Null);
            Assert.That(NodnarbGame.Instance, Is.SameAs(game));
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
            Assert.That(Resources.Load<Texture2D>("Art/title-screen"), Is.Not.Null);
            Transform titleArtwork = game.transform.Find("Interface/SafeArea/TitleScreen/TitleArtwork");
            Assert.That(titleArtwork, Is.Not.Null);
            Assert.That(titleArtwork.GetComponent<UnityEngine.UI.RawImage>().texture, Is.Not.Null);
            Transform titleConsole = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole");
            Assert.That(titleConsole, Is.Not.Null);
            Assert.That(titleConsole.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1f),
                "Functional title console must fully cover baked decorative controls in the artwork.");

            ProjectilePool projectilePool = game.GetComponentInChildren<ProjectilePool>(true);
            Assert.That(projectilePool, Is.Not.Null);
            Assert.That(projectilePool.TotalProjectileSlots, Is.EqualTo(16),
                "Title startup should trim the pool back to its small warm set after combat.");
            Assert.That(UnityEngine.Object.FindObjectsOfType<AudioListener>(), Has.Length.EqualTo(1));
            if (Application.isMobilePlatform)
            {
                Assert.That(Input.multiTouchEnabled, Is.True, "Gameplay must keep multitouch enabled so the active drag finger cannot be stolen by a second touch.");
            }
            Assert.That(game.LastVisualBudgetReport, Does.Contain("NODNARB_VISUAL_BUDGET"));
            Assert.That(game.GetComponentInChildren<Camera>(true), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GameplayVisualFeedbackIsBoundedAndReducedMotionStopsShake()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Assert.That(game.LastVisualBudgetReport, Does.Contain("NODNARB_VISUAL_BUDGET"));
            Assert.That(game.ActiveCombatFeedback, Is.LessThanOrEqualTo(NodnarbVisualBudget.MaxActiveCombatFeedback));

            game.RequestCameraFeedback(0.12f, 0.2f);
            Assert.That(game.CameraShakeRemaining, Is.GreaterThan(0f));
            NodnarbSettings.ReducedMotionEnabled = true;
            game.RequestCameraFeedback(0.12f, 0.2f);
            yield return null;
            Assert.That(game.CameraShakeRemaining, Is.EqualTo(0f));

            NodnarbSettings.ReducedMotionEnabled = false;
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingRunResumesFromTheTitleAction()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            ProgressData data = new ProgressData
            {
                PendingRun = new PausedRunData
                {
                    LevelIndex = 1,
                    CaptainHealth = 74f,
                    SoldierCount = 4,
                    SelectedWeapon = 0,
                    SelectedSuit = 0,
                    Elapsed = 18f,
                    Score = 91,
                    Kills = 5,
                    Salvage = 7
                }
            };
            LocalProgress.Save(data);

            game.ShowTitle();
            Assert.That(game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/Continue")
                .GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("RESUME SIGNAL"));
            game.ContinueCampaign();
            yield return null;

            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.Model.Score, Is.EqualTo(91));
            Assert.That(game.Model.SoldierCount, Is.EqualTo(4));
            Assert.That(LocalProgress.Load().PendingRun, Is.Null);

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BackgroundResumePreservesSquadRailPosition()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform squadRoot = game.transform.Find("CaptainSquad");
            Assert.That(squadRoot, Is.Not.Null);
            squadRoot.position = new Vector3(1.2f, squadRoot.position.y, squadRoot.position.z);
            game.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);

            ProgressData saved = LocalProgress.Load();
            Assert.That(saved.PendingRun, Is.Not.Null);
            Assert.That(saved.PendingRun.SquadRelativeX, Is.GreaterThan(0.5f));
            float savedRelativeX = saved.PendingRun.SquadRelativeX;
            float savedTargetX = saved.PendingRun.SquadTargetX;

            game.ShowTitle();
            game.ContinueCampaign();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.SquadRelativeX, Is.EqualTo(savedRelativeX).Within(0.001f));
            Assert.That(game.SquadTargetX, Is.EqualTo(savedTargetX).Within(0.001f));
            Assert.That(game.CombatActive, Is.False, "A lifecycle resume must remain paused until explicit resume.");
            Assert.That(LocalProgress.Load().PendingRun, Is.Not.Null,
                "The paused checkpoint must remain durable until explicit resume.");

            game.TogglePause();
            Assert.That(LocalProgress.Load().PendingRun, Is.Null,
                "Explicit resume may consume the paused checkpoint.");

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingRunRestoresTargetAndCombatCheckpointsBeforeExplicitResume()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            float enemyX = 0.35f;
            float enemyZ = GameTheme.CaptainZ + 5f;
            float cardX = -0.45f;
            float cardZ = GameTheme.CaptainZ + 7f;
            LocalProgress.Save(new ProgressData
            {
                PendingRun = new PausedRunData
                {
                    LevelIndex = 1,
                    Endless = false,
                    ResumePaused = true,
                    Elapsed = 18f,
                    CaptainHealth = 74f,
                    SquadRelativeX = 0.8f,
                    SquadTargetX = 1.1f,
                    SoldierCount = 4,
                    SelectedWeapon = 0,
                    SelectedSuit = 0,
                    Score = 91,
                    Kills = 5,
                    Salvage = 7,
                    LevelSeed = CampaignCatalog.Get(1).Seed,
                    RandomSeed = 6123,
                    RandomDrawCount = 4,
                    PairSequence = 17,
                    WaveSequence = 3,
                    SpawnTimer = 1.5f,
                    CardTimer = 2.5f,
                    FireTimer = 0.4f,
                    Enemies = new[]
                    {
                        new PausedEnemyData
                        {
                            Kind = EnemyKind.Boss,
                            BossBehavior = BossBehavior.Striker,
                            LaneX = enemyX,
                            PositionX = enemyX,
                            PositionZ = enemyZ,
                            HealthRatio = 0.55f,
                            Health = 70f,
                            MaxHealth = 120f,
                            Speed = 0.8f,
                            RangedTimer = 0.7f,
                            BossTelegraphTimer = 0.45f,
                            BossTelegraphArmed = true,
                            ExactState = true
                        }
                    },
                    Cards = new[]
                    {
                        new PausedCardData
                        {
                            PairId = 17,
                            Kind = CardKind.Recruit,
                            PositionX = cardX,
                            PositionZ = cardZ,
                            HealthRatio = 0.6f,
                            Health = 2f,
                            MaxHealth = 3f,
                            Speed = 2.1f,
                            ExactState = true
                        }
                    }
                }
            });

            game.ShowTitle();
            game.ContinueCampaign();
            yield return null;

            Assert.That(game.CombatActive, Is.False);
            Assert.That(game.SquadTargetX, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(game.ActiveEnemyCount, Is.EqualTo(1));
            Assert.That(game.ActiveCardCount, Is.EqualTo(1));

            EnemyAgent restoredEnemy = game.GetComponentsInChildren<EnemyAgent>(true)
                .Single(candidate => candidate.IsAlive && candidate.gameObject.activeInHierarchy);
            Assert.That(restoredEnemy.IsBoss, Is.True);
            Assert.That(restoredEnemy.CurrentHealth, Is.EqualTo(70f).Within(0.001f));
            Assert.That(restoredEnemy.transform.position.x, Is.EqualTo(enemyX).Within(0.001f));
            Assert.That(restoredEnemy.transform.position.z, Is.EqualTo(enemyZ).Within(0.001f));
            Assert.That(restoredEnemy.BossTelegraphArmed, Is.True);
            Assert.That(restoredEnemy.BossTelegraphTimer, Is.EqualTo(0.45f).Within(0.001f));

            TargetCard restoredCard = game.GetComponentsInChildren<TargetCard>(true)
                .Single(candidate => candidate.IsAlive && candidate.gameObject.activeInHierarchy);
            Assert.That(restoredCard.PairId, Is.EqualTo(17));
            Assert.That(restoredCard.Kind, Is.EqualTo(CardKind.Recruit));
            Assert.That(restoredCard.CurrentHealth, Is.EqualTo(2f).Within(0.001f));
            Assert.That(restoredCard.transform.position.x, Is.EqualTo(cardX).Within(0.001f));
            Assert.That(restoredCard.transform.position.z, Is.EqualTo(cardZ).Within(0.001f));
            Assert.That(LocalProgress.Load().PendingRun, Is.Not.Null);

            game.TogglePause();
            Assert.That(LocalProgress.Load().PendingRun, Is.Null);
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TitleScreenUsesCalmButtonHierarchy()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            Transform console = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole");
            Assert.That(console, Is.Not.Null);
            RectTransform consoleRect = console.GetComponent<RectTransform>();
            Assert.That(consoleRect.anchorMin.y, Is.GreaterThan(0.02f));
            Assert.That(consoleRect.anchorMax.y, Is.LessThan(0.28f));
            Assert.That(consoleRect.anchorMin.x, Is.GreaterThan(0.05f));
            Assert.That(consoleRect.anchorMax.x, Is.LessThan(0.95f));

            RectTransform continueRect = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/Continue").GetComponent<RectTransform>();
            RectTransform levelsRect = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/Levels").GetComponent<RectTransform>();
            RectTransform endlessRect = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/Endless").GetComponent<RectTransform>();
            RectTransform loadoutRect = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/Loadout").GetComponent<RectTransform>();

            Assert.That(continueRect.anchorMin.y, Is.GreaterThan(levelsRect.anchorMax.y));
            Assert.That(continueRect.anchorMin.x, Is.LessThan(continueRect.anchorMax.x));
            Assert.That(levelsRect.anchorMax.x, Is.LessThan(endlessRect.anchorMin.x));
            Assert.That(endlessRect.anchorMax.x, Is.LessThan(loadoutRect.anchorMin.x));
            Assert.That(levelsRect.anchorMin.y, Is.GreaterThan(0.08f));
            Assert.That(loadoutRect.anchorMax.y, Is.LessThan(0.38f));
        }

        [UnityTest]
        public IEnumerator StoryRunPauseAndAbortFlowReturnsToTitle()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Story));
            Transform storyRoute = game.transform.Find("Interface/SafeArea/StoryScreen/Route");
            Assert.That(storyRoute, Is.Not.Null);
            Assert.That(storyRoute.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("NIGHT SHELF"));
            Transform storyBiome = game.transform.Find("Interface/SafeArea/StoryScreen/Biome");
            Assert.That(storyBiome, Is.Not.Null);
            Assert.That(storyBiome.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("LOW ORBIT"));
            Assert.That(storyBiome.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("OBJECTIVE"));
            Transform radioMessage = game.transform.Find("Interface/SafeArea/StoryScreen/Radio/Message");
            Assert.That(radioMessage.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("MILESTONE"));
            Assert.That(radioMessage.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("THE SIGNAL NEVER STOPS"));
            Assert.That(radioMessage.GetComponent<UnityEngine.UI.Text>().text, Does.Not.Contain("THE HULL SPLITS"));
            Transform frameOne = game.transform.Find("Interface/SafeArea/StoryScreen/FrameOnePanel");
            Transform frameTwo = game.transform.Find("Interface/SafeArea/StoryScreen/FrameTwoPanel");
            Assert.That(frameOne, Is.Not.Null);
            Assert.That(frameTwo, Is.Not.Null);
            Assert.That(frameOne.Find("Caption").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("FRAME 01"));
            Assert.That(frameOne.Find("Caption").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("ENDLESS"));
            Assert.That(frameTwo.Find("Title").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("NO EXTRACTION VECTOR"));

            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.Model, Is.Not.Null);
            Assert.That(game.CombatActive, Is.True);

            game.ActivateRapidFire();
            Assert.That(game.Model.RapidFireActive, Is.True);

            game.TogglePause();
            Assert.That(game.CombatActive, Is.False);
            game.TogglePause();
            Assert.That(game.CombatActive, Is.True);

            game.AbortRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
        }

        [UnityTest]
        public IEnumerator StoryWarmupBuildsReusableCombatVisualsBeforeDeploy()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();

            for (int frame = 0; frame < 90 && !game.CombatVisualPoolsWarmed; frame++)
            {
                yield return null;
            }

            Assert.That(game.CombatVisualPoolsWarmed, Is.True);
            Assert.That(game.CombatVisualWarmupObjects, Is.GreaterThanOrEqualTo(17));
            Assert.That(game.PooledEnemyCount, Is.GreaterThanOrEqualTo(15));
            Assert.That(game.PooledCardCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(game.ActiveEnemyCount, Is.Zero);
            Assert.That(game.ActiveCardCount, Is.Zero);

            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManuallyPausedRunSavesBeforeApplicationBackground()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.TogglePause();
            Assert.That(game.CombatActive, Is.False);
            game.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);

            ProgressData saved = LocalProgress.Load();
            Assert.That(saved.PendingRun, Is.Not.Null);
            Assert.That(saved.PendingRun.Endless, Is.True);
            Assert.That(saved.PendingRun.ResumePaused, Is.True);
            Assert.That(saved.PendingRun.IsValid(), Is.True);

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusLossSavesAndPausesAnActiveRun()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);

            ProgressData saved = LocalProgress.Load();
            Assert.That(saved.PendingRun, Is.Not.Null);
            Assert.That(saved.PendingRun.Endless, Is.True);
            Assert.That(saved.PendingRun.ResumePaused, Is.True);
            Assert.That(saved.PendingRun.IsValid(), Is.True);
            Assert.That(game.CombatActive, Is.False);

            game.SendMessage("OnApplicationFocus", true, SendMessageOptions.RequireReceiver);
            Assert.That(game.CombatActive, Is.False, "Returning focus must keep the run paused until the player resumes it.");

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TerminalRunFinalizesBeforeLifecycleSave()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.Model.SecureExtraction();
            game.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);

            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
            Assert.That(LocalProgress.Load().PendingRun, Is.Null);
            Assert.That(game.LastStabilityReport, Does.StartWith("NODNARB_STABILITY"));

            game.ShowTitle();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BackgroundSavedRunRestoresBehindPauseUntilExplicitResume()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            ProgressData saved = LocalProgress.Load();
            Assert.That(saved.PendingRun, Is.Not.Null);
            Assert.That(saved.PendingRun.ResumePaused, Is.True);
            Assert.That(game.CombatActive, Is.False);

            game.ShowTitle();
            game.ContinueCampaign();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.CombatActive, Is.False);
            Assert.That(game.transform.Find("Interface/SafeArea/PauseScreen").gameObject.activeSelf, Is.True);

            game.TogglePause();
            Assert.That(game.CombatActive, Is.True);
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstRunOnboardingAndCardChoiceGuideAreVisible()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            LocalProgress.Save(new ProgressData());
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform onboarding = game.transform.Find("Interface/SafeArea/GameHud/OnboardingHint");
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(onboarding.gameObject.activeSelf, Is.True);
            Assert.That(onboarding.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("MOVE"));
            Assert.That(LocalProgress.Load().OnboardingComplete, Is.False);

            game.AbortRun();
            yield return null;
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;
            Assert.That(onboarding.gameObject.activeSelf, Is.True);
            Assert.That(LocalProgress.Load().OnboardingComplete, Is.False);

            GameObject onboardingCardObject = new GameObject("OnboardingEffectTestCard");
            TargetCard onboardingCard = onboardingCardObject.AddComponent<TargetCard>();
            onboardingCard.Initialize(game, 203, CardKind.Recruit, 0f);
            onboardingCard.ApplyDamage(100f);
            yield return null;
            Assert.That(onboarding.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("OVERDRIVE"));
            game.ActivateRapidFire();
            Assert.That(LocalProgress.Load().OnboardingComplete, Is.True);
            Assert.That(onboarding.gameObject.activeSelf, Is.False);
            UnityEngine.Object.Destroy(onboardingCardObject);

            yield return new WaitForSeconds(3.8f);
            Transform cardChoice = game.transform.Find("Interface/SafeArea/GameHud/CardChoiceHint");
            Assert.That(cardChoice, Is.Not.Null);
            Assert.That(cardChoice.gameObject.activeSelf, Is.True);
            Assert.That(cardChoice.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("CHOOSE ONE"));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SectorMapShowsSelectedRouteAndBossBrief()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenLevelSelect();

            Transform routeBrief = game.transform.Find("Interface/SafeArea/LevelSelect/RouteMap/RouteBrief");
            Assert.That(routeBrief, Is.Not.Null);
            Assert.That(routeBrief.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("PATH //"));
            Assert.That(routeBrief.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("BOSS //"));

            int selectedBefore = game.CurrentLevelIndex;
            game.SelectNextLevel();
            int expectedNext = selectedBefore >= CampaignCatalog.All.Count ? 1 : selectedBefore + 1;
            LevelDefinition expectedLevel = CampaignCatalog.Get(expectedNext);
            Assert.That(routeBrief.GetComponent<UnityEngine.UI.Text>().text, Does.Contain(CampaignCatalog.RouteBeat(expectedLevel)));
            Assert.That(routeBrief.GetComponent<UnityEngine.UI.Text>().text, Does.Contain(CampaignCatalog.BossBrief(expectedLevel)));
            game.HandleSystemBack();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SystemBackNavigatesMenusAndTogglesCombatPause()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Story));
            game.HandleSystemBack();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));

            game.OpenLevelSelect();
            Assert.That(game.State, Is.EqualTo(GameFlowState.LevelSelect));
            game.HandleSystemBack();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));

            game.OpenLoadout();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Loadout));
            game.HandleSystemBack();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));

            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            game.HandleSystemBack();
            Assert.That(game.CombatActive, Is.False);
            game.HandleSystemBack();
            Assert.That(game.CombatActive, Is.True);

            game.Model.DamageCaptain(1000f);
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
            game.HandleSystemBack();
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
        }

        [UnityTest]
        public IEnumerator InteractiveButtonsDisableAutomaticFocusNavigation()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            string[] buttonNames = { "Continue", "Levels", "Endless", "Loadout" };
            for (int index = 0; index < buttonNames.Length; index++)
            {
                Transform button = game.transform.Find("Interface/SafeArea/TitleScreen/TitleConsole/" + buttonNames[index]);
                Assert.That(button, Is.Not.Null);
                Assert.That(button.GetComponent<UnityEngine.UI.Button>().navigation.mode, Is.EqualTo(UnityEngine.UI.Navigation.Mode.None));
            }
        }

        [UnityTest]
        public IEnumerator ImportedCaptainVisualLoadsIntoRuntimeSquad()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform captain = game.transform.Find("CaptainSquad/Captain");
            GameObject captainPrefab = Resources.Load<GameObject>("Captain/CaptainVisual");
            Assert.That(captainPrefab, Is.Not.Null);
            Assert.That(captain, Is.Not.Null);
            Assert.That(captain.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
            Assert.That(captain.localScale.y, Is.GreaterThan(10f), "Imported Captain must preserve the FBX unit-conversion scale at runtime.");
            Quaternion expectedCaptainRotation = Quaternion.Euler(0f, 180f, 0f) * captainPrefab.transform.localRotation;
            Assert.That(Quaternion.Angle(captain.localRotation, expectedCaptainRotation), Is.LessThan(0.01f),
                "Imported Captain must preserve FBX axis conversion while facing incoming enemies.");
            Assert.That(captain.Find("Muzzle"), Is.Not.Null);

            Transform[] descendants = captain.GetComponentsInChildren<Transform>(true);
            bool hasMuzzlePoint = false;
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == "MuzzlePoint")
                {
                    hasMuzzlePoint = true;
                    break;
                }
            }

            Assert.That(hasMuzzlePoint, Is.True);
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartingSquadRendersCompanionsAndUsesEveryMuzzle()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform captain = game.transform.Find("CaptainSquad/Captain");
            Transform firstSoldier = game.transform.Find("CaptainSquad/Crew_1");
            Transform secondSoldier = game.transform.Find("CaptainSquad/Crew_2");
            Assert.That(game.Model.SoldierCount, Is.EqualTo(RunModel.StartingSoldiers));
            Assert.That(captain, Is.Not.Null);
            Assert.That(firstSoldier, Is.Not.Null);
            Assert.That(secondSoldier, Is.Not.Null);
            Assert.That(firstSoldier.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
            Assert.That(secondSoldier.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
            Assert.That(captain.Find("Muzzle/MuzzleFlash"), Is.Not.Null);
            Assert.That(firstSoldier.Find("Muzzle/MuzzleFlash"), Is.Not.Null);
            Assert.That(secondSoldier.Find("Muzzle/MuzzleFlash"), Is.Not.Null);
            Assert.That(Mathf.Abs(firstSoldier.position.x - captain.position.x), Is.GreaterThan(0.45f));
            Assert.That(Mathf.Abs(secondSoldier.position.x - captain.position.x), Is.GreaterThan(0.45f));
            Assert.That(Vector3.Distance(firstSoldier.position, secondSoldier.position), Is.GreaterThan(0.8f));
            Bounds captainBounds = GetBounds(captain);
            Bounds firstBounds = GetBounds(firstSoldier);
            Bounds secondBounds = GetBounds(secondSoldier);
            Debug.Log("NODNARB_LEADER_FRONT captainZ=" + captain.position.z + " crewZ=" + firstSoldier.position.z + "," + secondSoldier.position.z
                + " captainHeight=" + captainBounds.size.y + " crewHeight=" + firstBounds.size.y);
            Debug.Log("NODNARB_CAPTAIN_BOUNDS " + captainBounds);
            Debug.Log("NODNARB_SQUAD_BOUNDS first=" + firstBounds + " second=" + secondBounds);
            Assert.That(captain.position.z, Is.GreaterThan(firstSoldier.position.z + 0.75f), "Captain must be closest to the incoming horde on the combat Z axis.");
            Assert.That(captain.position.z, Is.GreaterThan(secondSoldier.position.z + 0.75f), "Captain must lead every crew row toward the incoming horde.");
            Assert.That(captainBounds.size.y, Is.InRange(1.2f, 1.7f), "Captain should read as a grounded human leader, not a giant actor.");
            Assert.That(firstBounds.size.y, Is.LessThan(captainBounds.size.y * 0.90f), "Crew_1 must remain visibly subordinate to the Captain.");
            Assert.That(secondBounds.size.y, Is.LessThan(captainBounds.size.y * 0.90f), "Crew_2 must remain visibly subordinate to the Captain.");
            Assert.That(firstBounds.min.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(secondBounds.min.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(firstBounds.size.y, Is.InRange(0.9f, 1.7f));
            Assert.That(secondBounds.size.y, Is.InRange(0.9f, 1.7f));
            Vector3 firstScreen = Camera.main.WorldToScreenPoint(firstSoldier.position + Vector3.up * 0.82f);
            Vector3 secondScreen = Camera.main.WorldToScreenPoint(secondSoldier.position + Vector3.up * 0.82f);
            Debug.Log("NODNARB_SQUAD_VISIBILITY first=" + firstScreen + " second=" + secondScreen);
            Assert.That(firstScreen.z, Is.GreaterThan(0f));
            Assert.That(secondScreen.z, Is.GreaterThan(0f));
            Assert.That(firstScreen.x, Is.InRange(Screen.width * 0.08f, Screen.width * 0.92f));
            Assert.That(secondScreen.x, Is.InRange(Screen.width * 0.08f, Screen.width * 0.92f));
            Assert.That(firstScreen.y, Is.GreaterThan(Screen.height * 0.18f));
            Assert.That(secondScreen.y, Is.GreaterThan(Screen.height * 0.18f));

            yield return new WaitForSeconds(0.52f);
            Assert.That(game.LastVolleyShooterCount, Is.EqualTo(game.Model.VisibleShooterCount));
            Assert.That(game.TotalFriendlyShotsFired, Is.GreaterThanOrEqualTo(game.Model.VisibleShooterCount));

            yield return new WaitForSeconds(1.45f);
            Assert.That(game.TotalFriendlyHits, Is.GreaterThan(0), "Auto-fire must connect with a live enemy or pickup target.");
            Assert.That(game.TotalCombatFeedbackEvents, Is.GreaterThan(0), "A connected shot must create visible impact feedback.");
            Assert.That(game.ActiveFriendlyProjectiles, Is.LessThanOrEqualTo(90), "Friendly projectile pool must stay bounded.");
            Assert.That(game.ActiveCombatFeedback, Is.LessThanOrEqualTo(64), "Combat feedback pool must stay bounded.");

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SquadUnitsReactToHorizontalMovement()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform squadRoot = game.transform.Find("CaptainSquad");
            Transform captain = game.transform.Find("CaptainSquad/Captain");
            Transform firstSoldier = game.transform.Find("CaptainSquad/Crew_1");
            Assert.That(squadRoot, Is.Not.Null);
            Assert.That(captain, Is.Not.Null);
            Assert.That(firstSoldier, Is.Not.Null);

            Vector3 captainBasePosition = captain.localPosition;
            Quaternion captainBaseRotation = captain.localRotation;
            Vector3 soldierBasePosition = firstSoldier.localPosition;
            Quaternion soldierBaseRotation = firstSoldier.localRotation;

            squadRoot.position += Vector3.right * 1.4f;
            yield return null;

            float captainLean = Quaternion.Angle(captain.localRotation, captainBaseRotation);
            float soldierLean = Quaternion.Angle(firstSoldier.localRotation, soldierBaseRotation);
            float captainBob = Mathf.Abs(captain.localPosition.y - captainBasePosition.y);
            float soldierBob = Mathf.Abs(firstSoldier.localPosition.y - soldierBasePosition.y);
            Debug.Log("NODNARB_MOVEMENT_FEEDBACK captainLean=" + captainLean + " soldierLean=" + soldierLean
                + " captainBob=" + captainBob + " soldierBob=" + soldierBob);
            Assert.That(captainLean, Is.GreaterThan(0.01f), "Captain should lean when the squad moves horizontally.");
            Assert.That(soldierLean, Is.GreaterThan(0.01f), "Crew should lean with the moving squad.");
            Assert.That(captainBob + soldierBob, Is.GreaterThan(0.0001f), "Movement should add visible vertical footfall feedback.");

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatFocalUnitsStayInsidePortraitActionBand()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform[] focalUnits =
            {
                game.transform.Find("CaptainSquad/Captain"),
                game.transform.Find("CaptainSquad/Crew_1"),
                game.transform.Find("CaptainSquad/Crew_2")
            };
            Assert.That(Camera.main, Is.Not.Null);
            for (int index = 0; index < focalUnits.Length; index++)
            {
                Assert.That(focalUnits[index], Is.Not.Null);
                Bounds bounds = GetBounds(focalUnits[index]);
                Vector3 feet = Camera.main.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                Vector3 head = Camera.main.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
                Debug.Log("NODNARB_COMBAT_FOCAL screen=" + Screen.width + "x" + Screen.height + " unit=" + focalUnits[index].name + " feet=" + feet + " head=" + head);
                Assert.That(feet.z, Is.GreaterThan(0f));
                Assert.That(head.z, Is.GreaterThan(0f));
                Assert.That(head.y, Is.GreaterThan(Screen.height * 0.18f), focalUnits[index].name + " is too low for the action band");
                Assert.That(feet.y, Is.LessThan(Screen.height * 0.90f), focalUnits[index].name + " is hidden behind the action rail");
            }

            game.AbortRun();
            yield return null;
        }

        private static Bounds GetBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        [UnityTest]
        public IEnumerator GeneratedEnemyVisualsLoadAboveGround()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return new WaitForSeconds(1.4f);

            EnemyAgent[] enemies = game.GetComponentsInChildren<EnemyAgent>(true);
            Assert.That(enemies.Length, Is.GreaterThan(0));
            for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
            {
                Renderer[] renderers = enemies[enemyIndex].GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.min.y, Is.GreaterThan(-0.1f), enemies[enemyIndex].Kind + " visual is below the lane");
            }

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyDamageFeedbackReflectsHitsAndStaysAboveGround()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return new WaitForSeconds(1.4f);

            GameObject regularObject = new GameObject("HealthFeedbackRegularTest");
            regularObject.transform.SetParent(game.transform, false);
            EnemyAgent regular = regularObject.AddComponent<EnemyAgent>();
            regular.Initialize(game, EnemyKind.Melee, 0.12f, 50f);
            regular.transform.position = new Vector3(50f, 0f, 50f);
            Assert.That(regular.HasHealthFeedback, Is.True);
            Assert.That(regular.HealthFeedback.gameObject.activeSelf, Is.False);
            float before = regular.HealthRatio;
            Bounds regularBounds = GetBounds(regular.HealthFeedback);
            Assert.That(regularBounds.min.y, Is.GreaterThan(0.45f));
            Assert.That(regularBounds.max.y, Is.LessThan(4.2f));
            regular.ApplyDamage(1f);
            yield return null;
            Assert.That(regular.HealthRatio, Is.LessThan(before));
            Assert.That(regular.HealthFeedback.gameObject.activeSelf, Is.True);
            yield return new WaitForSeconds(1.35f);
            Assert.That(regular.HealthFeedback.gameObject.activeSelf, Is.False);

            GameObject bossObject = new GameObject("HealthFeedbackBossTest");
            bossObject.transform.SetParent(game.transform, false);
            EnemyAgent boss = bossObject.AddComponent<EnemyAgent>();
            boss.Initialize(game, EnemyKind.Boss, 0.72f, 0f, BossBehavior.Carrier);
            Assert.That(boss.HasHealthFeedback, Is.True);
            Bounds bossBounds = GetBounds(boss.HealthFeedback);
            Assert.That(bossBounds.min.y, Is.GreaterThan(2.0f));
            Assert.That(bossBounds.max.y, Is.LessThan(5.0f));
            float bossBefore = boss.HealthRatio;
            boss.ApplyDamage(10f);
            Assert.That(boss.HealthRatio, Is.LessThan(bossBefore));
            boss.Cancel();
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TerminalRunTransitionsToResultInsteadOfStalling()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));

            game.Model.DamageCaptain(1000f);
            yield return null;

            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
            game.ShowTitle();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
        }

        [UnityTest]
        public IEnumerator ExtractionRunTransitionsToVictoryResult()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));

            game.Model.SecureExtraction();
            yield return null;

            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
            Assert.That(game.Model.EndReason, Is.EqualTo(RunEndReason.ExtractionSecured));
            game.ShowTitle();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
        }

        [UnityTest]
        public IEnumerator ResultPrimaryActionOffersRetryAndCampaignProgression()
        {
            string savedProgress = PlayerPrefs.GetString(LocalProgress.SaveKey, string.Empty);
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.Model.DamageCaptain(1000f);
            yield return null;
            Transform resultNext = game.transform.Find("Interface/SafeArea/ResultScreen/Next");
            Assert.That(resultNext, Is.Not.Null);
            Assert.That(resultNext.gameObject.activeSelf, Is.True);
            Assert.That(resultNext.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("RETRY SIGNAL"));
            resultNext.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            game.AbortRun();

            game.ShowTitle();
            game.ContinueCampaign();
            game.BeginPendingRun();
            yield return null;
            int completedLevel = game.CurrentLevelIndex;
            game.Model.SecureExtraction();
            yield return null;

            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
            if (completedLevel < CampaignCatalog.All.Count)
            {
                Assert.That(resultNext.gameObject.activeSelf, Is.True);
                Assert.That(resultNext.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("NEXT SECTOR"));
                resultNext.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return null;
                Assert.That(game.State, Is.EqualTo(GameFlowState.Story));
                Assert.That(game.CurrentLevelIndex, Is.EqualTo(completedLevel + 1));
            }

            game.ShowTitle();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
            if (string.IsNullOrEmpty(savedProgress))
            {
                PlayerPrefs.DeleteKey(LocalProgress.SaveKey);
            }
            else
            {
                PlayerPrefs.SetString(LocalProgress.SaveKey, savedProgress);
            }
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator FinalCampaignVictoryShowsTerminalResult()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            ProgressData unlocked = new ProgressData
            {
                UnlockedLevel = CampaignCatalog.All.Count,
                CompletedLevelMask = (1 << CampaignCatalog.All.Count) - 1
            };
            LocalProgress.Save(unlocked);
            game.ShowTitle();
            game.OpenLevelSelect();
            Assert.That(game.CurrentLevelIndex, Is.EqualTo(CampaignCatalog.All.Count));
            game.OpenSelectedStory();
            game.BeginPendingRun();
            yield return null;
            game.Model.SecureExtraction();
            yield return null;

            Transform result = game.transform.Find("Interface/SafeArea/ResultScreen");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Find("Status").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("EXTRACTION SECURED"));
            Assert.That(result.Find("Next").gameObject.activeSelf, Is.False);
            Assert.That(game.LastBudgetReport, Does.StartWith("NODNARB_BUDGET"));
            Assert.That(game.LastBudgetReport, Does.Contain("peak_enemies="));
            Assert.That(game.LastBudgetReport, Does.Contain("frame_p95_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("over_budget_frames="));
            Assert.That(game.LastBudgetReport, Does.Contain("model_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("input_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("volley_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("wave_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("card_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("runtime_ready_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("model_worst_ms="));
            Assert.That(game.LastBudgetReport, Does.Contain("wave_worst_ms="));
            Assert.That(game.LastBalanceReport, Does.StartWith("NODNARB_BALANCE"));
            Assert.That(game.LastBalanceReport, Does.Contain("credits="));
            Assert.That(game.LastReadabilityReport, Does.StartWith("NODNARB_READABILITY"));
            Assert.That(game.LastReadabilityReport, Does.Contain("boss_telegraphs="));
            Assert.That(game.LastReadabilityReport, Does.Contain("visible_squad="));

            game.ShowTitle();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplayAfterResultReturnsToStoryAndStartsCleanRun()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            int replayRunsBefore = LocalProgress.Load().ReplayRuns;

            game.Model.DamageCaptain(1000f);
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Result));

            game.ReplayAfterResult();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Story));
            Assert.That(game.Model, Is.Null);

            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.Model.SoldierCount, Is.EqualTo(RunModel.StartingSoldiers));
            Assert.That(game.TotalFriendlyShotsFired, Is.EqualTo(0));
            Assert.That(LocalProgress.Load().ReplayRuns, Is.GreaterThan(replayRunsBefore));
            game.AbortRun();
        }

        [UnityTest]
        public IEnumerator RepeatedRunsTrimWarmPoolsAndPublishStabilityEvidence()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            ProjectilePool projectiles = game.GetComponentInChildren<ProjectilePool>(true);
            Assert.That(projectiles, Is.Not.Null);

            for (int run = 0; run < 3; run++)
            {
                game.ShowTitle();
                game.OpenEndlessStory();
                game.BeginPendingRun();
                yield return null;
                game.Model.DamageCaptain(1000f);
                yield return null;

                Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
                Assert.That(game.LastStabilityReport, Does.StartWith("NODNARB_STABILITY"));
                Assert.That(game.LastStabilityReport, Does.Contain("feedback_slots=0"));
                game.ShowTitle();
                yield return null;
                Assert.That(projectiles.TotalProjectileSlots, Is.EqualTo(16));
                Assert.That(projectiles.TotalFeedbackSlots, Is.Zero);
                Assert.That(game.PooledEnemyCount, Is.LessThanOrEqualTo(32));
                Assert.That(game.PooledCardCount, Is.LessThanOrEqualTo(4));
            }
        }

        [UnityTest]
        public IEnumerator ImportedCrewExposesSeparatedLimbMotionParts()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform crew = game.transform.Find("CaptainSquad/Crew_1");
            Assert.That(crew, Is.Not.Null);
            Transform[] parts = crew.GetComponentsInChildren<Transform>(true);
            Assert.That(parts.Any(part => part.name == "Thigh"), Is.True);
            Assert.That(parts.Any(part => part.name == "Thigh.001"), Is.True);
            Assert.That(parts.Any(part => part.name == "UpperArm"), Is.True);
            Assert.That(parts.Any(part => part.name == "UpperArm.001"), Is.True);

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetCardLabelsFaceThePlayer()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            GameObject cardObject = new GameObject("LabelFacingTestCard");
            TargetCard card = cardObject.AddComponent<TargetCard>();
            card.Initialize(game, 99, CardKind.Recruit, 1f);

            Transform label = card.transform.Find("Label");
            Assert.That(label, Is.Not.Null);
            Assert.That(Quaternion.Angle(label.localRotation, Quaternion.identity), Is.LessThan(0.01f));

            card.Cancel();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticCardPairShowsChoiceBandAndBothLabels()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();

            for (int frame = 0; frame < 420 && game.ActiveCardCount < 2; frame++)
            {
                yield return null;
            }

            Camera cardCamera = Camera.main;
            if (cardCamera == null)
            {
                Assert.Fail("A live card pair requires the gameplay camera.");
                yield break;
            }

            Rect originalPixelRect = cardCamera.pixelRect;
            float originalAspect = cardCamera.aspect;
            Rect portraitViewport = new Rect(0f, 0f, 360f, 780f);
            float edgeTouchMargin = portraitViewport.width * (96f / 1440f);
            cardCamera.pixelRect = portraitViewport;
            cardCamera.aspect = portraitViewport.width / portraitViewport.height;

            try
            {
                Assert.That(portraitViewport.height, Is.GreaterThan(portraitViewport.width),
                    "The card viewport regression must execute against a portrait device shape.");
                Assert.That(portraitViewport.width / portraitViewport.height, Is.EqualTo(1440f / 3120f).Within(0.001f));
                yield return null;

            Transform cardBand = game.transform.Find("Interface/SafeArea/GameHud/CardChoiceBand");
            Transform weaponLabel = game.transform.Find("WeaponCardPair_1/Label");
            Transform recruitLabel = game.transform.Find("RecruitCardPair_1/Label");
            bool initialPairVisible = game.ActiveCardCount == 2;
            bool initialBandVisible = cardBand != null && cardBand.gameObject.activeSelf;
            bool labelsReadable = weaponLabel != null && recruitLabel != null
                && weaponLabel.GetComponent<TextMesh>().text == "WEAPON\n+1"
                && recruitLabel.GetComponent<TextMesh>().text == "+1\nCREW";
            bool cardsOnScreen = weaponLabel != null && recruitLabel != null
                && weaponLabel.parent != null && recruitLabel.parent != null
                && IsScreenVisible(cardCamera, weaponLabel, portraitViewport)
                && IsScreenVisible(cardCamera, recruitLabel, portraitViewport)
                && IsRendererBoundsVisible(cardCamera, weaponLabel.parent)
                && IsRendererBoundsVisible(cardCamera, recruitLabel.parent);
            Vector3 weaponCardScreenPosition = weaponLabel == null || weaponLabel.parent == null
                ? Vector3.zero
                : cardCamera.WorldToScreenPoint(weaponLabel.parent.position);
            Vector3 recruitCardScreenPosition = recruitLabel == null || recruitLabel.parent == null
                ? Vector3.zero
                : cardCamera.WorldToScreenPoint(recruitLabel.parent.position);

            yield return new WaitForSeconds(5f);
            bool cardsPersist = game.ActiveCardCount == 2;
            bool bandPersists = cardBand != null && cardBand.gameObject.activeSelf;
            bool cardsOnScreenAfterDrift = weaponLabel != null && recruitLabel != null
                && weaponLabel.parent != null && recruitLabel.parent != null
                && IsScreenVisible(cardCamera, weaponLabel, portraitViewport)
                && IsScreenVisible(cardCamera, recruitLabel, portraitViewport)
                && IsRendererBoundsVisible(cardCamera, weaponLabel.parent)
                && IsRendererBoundsVisible(cardCamera, recruitLabel.parent);
            Vector3 weaponCardScreenPositionAfterDrift = weaponLabel == null || weaponLabel.parent == null
                ? Vector3.zero
                : cardCamera.WorldToScreenPoint(weaponLabel.parent.position);
            Vector3 recruitCardScreenPositionAfterDrift = recruitLabel == null || recruitLabel.parent == null
                ? Vector3.zero
                : cardCamera.WorldToScreenPoint(recruitLabel.parent.position);

            game.AbortRun();
            yield return null;

            Assert.That(initialPairVisible, Is.True, "A live run must expose a complete left/right card pair.");
            Assert.That(initialBandVisible, Is.True, "The choice band must be visible while cards are active.");
            Assert.That(labelsReadable, Is.True, "The left and right card labels must remain readable.");
            Assert.That(cardsOnScreen, Is.True, "Both card targets must be in the active camera viewport.");
            Assert.That(weaponCardScreenPosition.x, Is.GreaterThan(edgeTouchMargin),
                "The weapon card must keep a physical touch margin from the left display edge.");
            Assert.That(recruitCardScreenPosition.x, Is.LessThan(portraitViewport.width - edgeTouchMargin),
                "The recruit card must keep a physical touch margin from the right display edge.");
            Assert.That(cardsOnScreenAfterDrift, Is.True,
                "Wide-sweep card motion must keep both card targets fully inside the portrait viewport.");
            Assert.That(weaponCardScreenPositionAfterDrift.x, Is.GreaterThan(edgeTouchMargin),
                "The weapon card must retain its touch margin after lateral drift.");
            Assert.That(recruitCardScreenPositionAfterDrift.x, Is.LessThan(portraitViewport.width - edgeTouchMargin),
                "The recruit card must retain its touch margin after lateral drift.");
            Assert.That(cardsPersist, Is.True, "Centered auto-fire must leave both cards available until the player commits to a side.");
            Assert.That(bandPersists, Is.True);
            }
            finally
            {
                if (game != null && game.CombatActive)
                {
                    game.AbortRun();
                }
                cardCamera.pixelRect = originalPixelRect;
                cardCamera.aspect = originalAspect;
            }
        }

        [UnityTest]
        public IEnumerator LoadoutExposesLocalSoundMusicAndHapticsControls()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenLoadout();
            yield return null;

            Transform loadout = game.transform.Find("Interface/SafeArea/Loadout");
            Assert.That(loadout.Find("SoundSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("SFX // ON"));
            Assert.That(loadout.Find("MusicSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("MUSIC // ON"));
            Assert.That(loadout.Find("HapticsSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("HAPTICS // ON"));
            Assert.That(loadout.Find("CaptionsSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("CAPTIONS // ON"));
            Assert.That(loadout.Find("HighContrastSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("CONTRAST // OFF"));
            Assert.That(loadout.Find("ReducedMotionSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("MOTION // ON"));

            loadout.Find("SoundSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            loadout.Find("MusicSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            loadout.Find("HapticsSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            loadout.Find("CaptionsSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            loadout.Find("HighContrastSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            loadout.Find("ReducedMotionSetting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(loadout.Find("SoundSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("SFX // OFF"));
            Assert.That(loadout.Find("MusicSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("MUSIC // OFF"));
            Assert.That(loadout.Find("HapticsSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("HAPTICS // OFF"));
            Assert.That(loadout.Find("CaptionsSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("CAPTIONS // OFF"));
            Assert.That(loadout.Find("HighContrastSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("CONTRAST // ON"));
            Assert.That(loadout.Find("ReducedMotionSetting/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("MOTION // OFF"));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AccessibilitySettingsReachActiveAndPooledCombatVisuals()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            GameObject enemyObject = new GameObject("AccessibilityEnemy");
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Initialize(game, EnemyKind.Melee, 0.2f, -0.8f);
            Renderer shadowRenderer = enemy.transform.Find("EnemyShadow").GetComponent<Renderer>();
            Renderer healthBackRenderer = enemy.transform.Find("HealthBand/HealthBack").GetComponent<Renderer>();
            Color propertyMarker = new Color(0.12f, 0.23f, 0.34f, 1f);
            MaterialPropertyBlock markerBlock = new MaterialPropertyBlock();
            markerBlock.SetColor("_Color", propertyMarker);
            shadowRenderer.SetPropertyBlock(markerBlock);
            healthBackRenderer.SetPropertyBlock(markerBlock);
            GameObject cardObject = new GameObject("AccessibilityCard");
            TargetCard card = cardObject.AddComponent<TargetCard>();
            card.Initialize(game, 901, CardKind.Recruit, 0.2f);

            game.ToggleHighContrast();
            game.ToggleReducedMotion();
            yield return null;
            Assert.That(NodnarbSettings.HighContrastEnabled, Is.True);
            Assert.That(NodnarbSettings.ReducedMotionEnabled, Is.True);
            Assert.That(enemy.AccessibilityVisualApplied, Is.True);
            Assert.That(card.AccessibilityVisualApplied, Is.True);
            Assert.That(game.ProjectileAccessibilityVisualApplied, Is.True);
            Assert.That(game.SquadAccessibilityVisualApplied, Is.True);
            MaterialPropertyBlock shadowAfter = new MaterialPropertyBlock();
            MaterialPropertyBlock healthBackAfter = new MaterialPropertyBlock();
            shadowRenderer.GetPropertyBlock(shadowAfter);
            healthBackRenderer.GetPropertyBlock(healthBackAfter);
            Assert.That(shadowAfter.GetColor("_Color").r, Is.EqualTo(propertyMarker.r).Within(0.001f));
            Assert.That(healthBackAfter.GetColor("_Color").g, Is.EqualTo(propertyMarker.g).Within(0.001f));

            enemy.Cancel();
            card.Cancel();
            game.ToggleHighContrast();
            game.ToggleHighContrast();
            yield return null;
            Assert.That(enemy.AccessibilityVisualApplied, Is.True);
            Assert.That(card.AccessibilityVisualApplied, Is.True);
            Assert.That(game.ProjectileAccessibilityVisualApplied, Is.True);
            Assert.That(game.SquadAccessibilityVisualApplied, Is.True);

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetCardsApplyRecruitAndWeaponEffects()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            int startingSoldiers = game.Model.SoldierCount;
            GameObject recruitObject = new GameObject("RecruitEffectTestCard");
            TargetCard recruit = recruitObject.AddComponent<TargetCard>();
            recruit.Initialize(game, 201, CardKind.Recruit, 0f);
            recruit.ApplyDamage(100f);
            yield return null;
            Assert.That(game.Model.SoldierCount, Is.EqualTo(startingSoldiers + 1));
            Assert.That(game.TotalRecruitPickups, Is.EqualTo(1));
            Assert.That(game.transform.Find("CaptainSquad/Crew_3"), Is.Not.Null, "+1 CREW must add a visible companion immediately.");
            UnityEngine.UI.Text pickupFeedback = game.transform.Find("Interface/SafeArea/GameHud/PickupFeedback").GetComponent<UnityEngine.UI.Text>();
            Assert.That(pickupFeedback.text, Does.Contain("SQUAD 03/12"));

            GameObject weaponObject = new GameObject("WeaponEffectTestCard");
            TargetCard weapon = weaponObject.AddComponent<TargetCard>();
            weapon.Initialize(game, 202, CardKind.Weapon, 0f);
            weapon.ApplyDamage(100f);
            yield return null;
            Assert.That(game.Model.WeaponLevel, Is.EqualTo(1));
            Assert.That(game.TotalWeaponPickups, Is.EqualTo(1));
            Assert.That(game.transform.Find("Interface/SafeArea/GameHud/RunStats/Power").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("RATE"));
            Assert.That(pickupFeedback.text, Does.Contain("PWR 02"));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCanTapCardDirectlyAndSeeSelectionSource()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform squadRoot = game.transform.Find("CaptainSquad");
            Assert.That(squadRoot, Is.Not.Null);
            squadRoot.position += Vector3.right * 1.4f;

            GameObject offRailObject = new GameObject("OffRailWeaponCard");
            TargetCard offRailWeapon = offRailObject.AddComponent<TargetCard>();
            offRailWeapon.Initialize(game, 239, CardKind.Weapon, 0f);
            Vector2 offRailPosition = Camera.main.WorldToScreenPoint(offRailWeapon.HitPosition);
            Assert.That(game.TrySelectCardAtScreenPosition(offRailPosition), Is.False,
                "A weapon card must not be selectable while the squad is on the recruit rail.");
            Assert.That(offRailWeapon.IsAlive, Is.True);
            offRailWeapon.Cancel();

            GameObject cardObject = new GameObject("DirectTapCard");
            TargetCard card = cardObject.AddComponent<TargetCard>();
            card.Initialize(game, 240, CardKind.Recruit, 0f);
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(card.HitPosition);

            bool selected = game.TrySelectCardAtScreenPosition(screenPosition);
            yield return null;

            Assert.That(selected, Is.True, "A visible card must accept a direct screen tap target.");
            Assert.That(game.LastCardActivationSource, Is.EqualTo(CardActivationSource.Touch));
            Assert.That(game.LastCardSelectionReport, Does.Contain("source=Touch"));
            Assert.That(game.TotalRecruitPickups, Is.EqualTo(1));
            Assert.That(game.TotalCardChoices, Is.EqualTo(1));
            Assert.That(card.IsAlive, Is.False);

            game.Model.DamageCaptain(1000f);
            yield return null;
            UnityEngine.UI.Text pickupResult = game.transform.Find("Interface/SafeArea/ResultScreen/SpecSheet/Value5")
                .GetComponent<UnityEngine.UI.Text>();
            Assert.That(pickupResult.text, Is.EqualTo("CREW 01 // WEAPON 00"));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BackgroundResumePreservesCardPayoffCounters()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform squadRoot = game.transform.Find("CaptainSquad");
            Assert.That(squadRoot, Is.Not.Null);
            squadRoot.position += Vector3.right * 1.4f;

            GameObject cardObject = new GameObject("PersistedCardPayoff");
            TargetCard card = cardObject.AddComponent<TargetCard>();
            card.Initialize(game, 241, CardKind.Recruit, 0f);
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(card.HitPosition);
            Assert.That(game.TrySelectCardAtScreenPosition(screenPosition), Is.True);
            Assert.That(game.TotalRecruitPickups, Is.EqualTo(1));

            game.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            ProgressData saved = LocalProgress.Load();
            Assert.That(saved.PendingRun, Is.Not.Null);
            Assert.That(saved.PendingRun.RecruitPickups, Is.EqualTo(1));
            Assert.That(saved.PendingRun.CardChoices, Is.EqualTo(1));
            Assert.That(saved.PendingRun.LastCardActivationSource, Is.EqualTo(CardActivationSource.Touch));
            Assert.That(saved.PendingRun.LastCardSelectionReport, Does.Contain("source=Touch"));

            game.ShowTitle();
            game.ContinueCampaign();
            yield return null;
            Assert.That(game.CombatActive, Is.False);
            Assert.That(game.TotalRecruitPickups, Is.EqualTo(1));
            Assert.That(game.TotalCardChoices, Is.EqualTo(1));
            Assert.That(game.LastCardActivationSource, Is.EqualTo(CardActivationSource.Touch));
            Assert.That(game.LastCardSelectionReport, Does.Contain("source=Touch"));

            game.TogglePause();
            game.Model.DamageCaptain(1000f);
            yield return null;
            UnityEngine.UI.Text pickupResult = game.transform.Find("Interface/SafeArea/ResultScreen/SpecSheet/Value5")
                .GetComponent<UnityEngine.UI.Text>();
            Assert.That(pickupResult.text, Is.EqualTo("CREW 01 // WEAPON 00"));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PickupCardsUseDistinctSignalTrimAndLabels()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();

            GameObject weaponObject = new GameObject("WeaponReadabilityCard");
            TargetCard weapon = weaponObject.AddComponent<TargetCard>();
            weapon.Initialize(game, 301, CardKind.Weapon, 0f);
            GameObject recruitObject = new GameObject("RecruitReadabilityCard");
            TargetCard recruit = recruitObject.AddComponent<TargetCard>();
            recruit.Initialize(game, 302, CardKind.Recruit, 0f);

            Assert.That(weapon.transform.Find("CardSignalGlow"), Is.Not.Null);
            Assert.That(recruit.transform.Find("CardSignalGlow"), Is.Not.Null);
            Assert.That(weapon.transform.Find("Label").GetComponent<TextMesh>().text, Is.EqualTo("WEAPON\n+1"));
            Assert.That(recruit.transform.Find("Label").GetComponent<TextMesh>().text, Is.EqualTo("+1\nCREW"));
            Assert.That(ColorUtility.ToHtmlStringRGB(weapon.AccentColor), Is.Not.EqualTo(ColorUtility.ToHtmlStringRGB(recruit.AccentColor)));

            Transform weaponPanel = weapon.transform.Find("WeaponCard");
            Vector3 weaponPanelBaseScale = weaponPanel.localScale;
            weapon.ApplyDamage(0.8f);
            Assert.That(weaponPanel.localScale.y / weaponPanel.localScale.x,
                Is.EqualTo(weaponPanelBaseScale.y / weaponPanelBaseScale.x).Within(0.001f));
            weapon.Initialize(game, 303, CardKind.Weapon, 0f);
            Assert.That(weaponPanel.localScale.x, Is.EqualTo(weaponPanelBaseScale.x).Within(0.001f));
            Assert.That(weaponPanel.localScale.y, Is.EqualTo(weaponPanelBaseScale.y).Within(0.001f));
            recruit.ApplyDamage(0.8f);
            yield return null;
            Assert.That(weapon.transform.Find("CardSignalGlow"), Is.Not.Null);
            Assert.That(recruit.transform.Find("CardSignalGlow"), Is.Not.Null);

            weapon.Cancel();
            recruit.Cancel();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatHudPresentsReadableActionRail()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform hud = game.transform.Find("Interface/SafeArea/GameHud");
            Assert.That(hud, Is.Not.Null);
            RectTransform actionRail = hud.Find("ActionRail").GetComponent<RectTransform>();
            Assert.That(actionRail, Is.Not.Null);
            Assert.That(actionRail.anchorMax.y, Is.LessThanOrEqualTo(0.11f));
            Assert.That(hud.Find("InputHint").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("VOLLEY 03"));
            Assert.That(hud.Find("InputHint").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("AUTO-FIRE"));
            Assert.That(hud.Find("LeftRail").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("WEAPON +1"));
            Assert.That(hud.Find("RightRail").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("+1 CREW"));
            Assert.That(hud.Find("Score").GetComponent<UnityEngine.UI.Text>().text, Does.StartWith("SCORE "));
            RectTransform inputHint = hud.Find("InputHint").GetComponent<RectTransform>();
            Assert.That(inputHint.anchorMin.y, Is.GreaterThan(actionRail.anchorMax.y));
            Assert.That(hud.Find("RapidFire/Text").GetComponent<UnityEngine.UI.Text>().fontSize, Is.LessThanOrEqualTo(21));
            Assert.That(hud.Find("TopRail/Pause/Text").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("PAUSE"));
            Assert.That(hud.Find("TopRail/Pause").GetComponent<RectTransform>().anchorMin.x, Is.GreaterThanOrEqualTo(0.84f));

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatHudVolleySizeTracksVisibleSquad()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            Transform inputHint = game.transform.Find("Interface/SafeArea/GameHud/InputHint");
            Assert.That(inputHint.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("VOLLEY 03"));
            Assert.That(game.LastVolleyShooterCount, Is.EqualTo(0));

            game.Model.Recruit();
            yield return new WaitForSeconds(0.12f);
            Assert.That(inputHint.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("VOLLEY 04"));

            yield return new WaitForSeconds(0.52f);
            Assert.That(game.LastVolleyShooterCount, Is.EqualTo(game.Model.VisibleShooterCount));
            Assert.That(game.LastVolleyShooterCount, Is.EqualTo(4));
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossTelegraphShowsWarningAndStaysBounded()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return null;

            game.NotifyBossTelegraph();
            Assert.That(game.TotalBossTelegraphs, Is.EqualTo(1));
            Transform warning = game.transform.Find("Interface/SafeArea/GameHud/CombatWarning");
            Assert.That(warning, Is.Not.Null);
            Assert.That(warning.gameObject.activeSelf, Is.True);
            Assert.That(warning.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("INCOMING VOLLEY"));

            yield return new WaitForSeconds(0.85f);
            Assert.That(warning.gameObject.activeSelf, Is.False);
            Assert.That(game.ActiveCombatFeedback, Is.LessThanOrEqualTo(64));
            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SectorMapShowsTenRouteNodes()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;
            game.ShowTitle();
            game.OpenLevelSelect();
            yield return null;

            Assert.That(game.State, Is.EqualTo(GameFlowState.LevelSelect));
            Transform routeMap = game.transform.Find("Interface/SafeArea/LevelSelect/RouteMap");
            Assert.That(routeMap, Is.Not.Null);
            float[] mapHeights = new float[CampaignCatalog.All.Count];
            for (int index = 1; index <= CampaignCatalog.All.Count; index++)
            {
                Assert.That(routeMap.Find("Node" + index.ToString("00")), Is.Not.Null);
                Assert.That(routeMap.Find("NodeLabel" + index.ToString("00")), Is.Not.Null);
                mapHeights[index - 1] = routeMap.Find("Node" + index.ToString("00")).GetComponent<RectTransform>().anchorMin.y;
                if (index < CampaignCatalog.All.Count)
                {
                    Assert.That(routeMap.Find("Link" + index.ToString("00")), Is.Not.Null);
                }
            }

            Assert.That(mapHeights.Distinct().Count(), Is.EqualTo(10));
            Assert.That(mapHeights[2], Is.LessThan(mapHeights[1]));
            Assert.That(mapHeights[9], Is.GreaterThan(mapHeights[6]));

            game.ShowTitle();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Title));
        }

        [UnityTest]
        public IEnumerator SnowlineBuildIncludesGroundedImportedLandmark()
        {
            GameObject owner = new GameObject("WorldLandmarkTestOwner");
            GameObject cameraObject = new GameObject("WorldLandmarkTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);
            world.Build(CampaignCatalog.Get(4), camera);
            yield return null;

            Transform landmark = owner.transform.Find("ProceduralWorld/SnowArchLandmark");
            Assert.That(landmark, Is.Not.Null);
            Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            Assert.That(bounds.min.y, Is.GreaterThan(-0.05f), "Imported SnowArch must be grounded on the playable lane.");
            Transform markers = owner.transform.Find("ProceduralWorld/SnowlineMarkers");
            Assert.That(markers, Is.Not.Null, "Snowline must include readable route markers.");
            Assert.That(markers.childCount, Is.GreaterThanOrEqualTo(15), "Snowline marker set is incomplete.");
            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BiomeBuildsIncludeReadableSignatureLandmarks()
        {
            GameObject owner = new GameObject("BiomeSignatureTestOwner");
            GameObject cameraObject = new GameObject("BiomeSignatureTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);
            LevelDefinition[] levels = { CampaignCatalog.Get(2), CampaignCatalog.Get(3), CampaignCatalog.Get(6) };
            string[] landmarkNames = { "CanyonDebrisLandmark", "HiveGrowthLandmark", "CrystalClusterLandmark" };

            for (int index = 0; index < levels.Length; index++)
            {
                world.Build(levels[index], camera);
                yield return null;

                Transform landmark = owner.transform.Find("ProceduralWorld/" + landmarkNames[index]);
                Assert.That(landmark, Is.Not.Null, landmarkNames[index] + " missing from biome build");
                Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.size.y, Is.GreaterThan(0.35f), landmarkNames[index] + " is not readable at runtime");
                Assert.That(bounds.min.y, Is.GreaterThan(-0.05f), landmarkNames[index] + " is below the lane");

                if (levels[index].Index == 3)
                {
                    Transform sporeArch = owner.transform.Find("ProceduralWorld/SporeArchLandmark");
                    Assert.That(sporeArch, Is.Not.Null, "SporeArch missing from N-03 / Unknown March");
                    Renderer[] sporeRenderers = sporeArch.GetComponentsInChildren<Renderer>(true);
                    Assert.That(sporeRenderers.Length, Is.GreaterThan(0), "SporeArch has no renderers");
                    Bounds sporeBounds = sporeRenderers[0].bounds;
                    for (int rendererIndex = 1; rendererIndex < sporeRenderers.Length; rendererIndex++)
                    {
                        sporeBounds.Encapsulate(sporeRenderers[rendererIndex].bounds);
                    }

                    Assert.That(sporeBounds.size.y, Is.GreaterThan(0.55f), "SporeArch is not readable at runtime");
                    Assert.That(sporeBounds.min.y, Is.GreaterThan(-0.05f), "SporeArch is below the lane");
                }
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstFiveStagesHaveDistinctEnvironmentIdentitySets()
        {
            GameObject owner = new GameObject("EnvironmentIdentityTestOwner");
            GameObject cameraObject = new GameObject("EnvironmentIdentityTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);
            string[] expectedNames =
            {
                "CrashHullRib_00",
                "CanyonSpire_00",
                "SporeLanternCore_00",
                "SnowPillar_00",
                "RelayPylon_00"
            };

            for (int index = 0; index < expectedNames.Length; index++)
            {
                world.Build(CampaignCatalog.Get(index + 1), camera);
                yield return null;

                Transform identity = owner.transform.Find("ProceduralWorld/EnvironmentIdentity_" + (index + 1).ToString("00"));
                Assert.That(identity, Is.Not.Null, "Stage " + (index + 1) + " identity root missing");
                Transform signature = identity.Find(expectedNames[index]);
                Assert.That(signature, Is.Not.Null, "Stage " + (index + 1) + " identity prop missing");
                Assert.That(identity.childCount, Is.GreaterThanOrEqualTo(6), "Stage " + (index + 1) + " identity set is too small");

                Renderer[] renderers = identity.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(6));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.size.y, Is.GreaterThan(1.5f), "Stage " + (index + 1) + " identity is not readable");
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.05f), "Stage " + (index + 1) + " identity is below ground");
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstFiveStagesHaveReadableBiomeFrames()
        {
            GameObject owner = new GameObject("BiomeFrameTestOwner");
            GameObject cameraObject = new GameObject("BiomeFrameTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            for (int index = 0; index < 5; index++)
            {
                world.Build(CampaignCatalog.Get(index + 1), camera);
                yield return null;

                Transform frame = owner.transform.Find("ProceduralWorld/BiomeFrame_" + (index + 1).ToString("00"));
                Assert.That(frame, Is.Not.Null, "Stage " + (index + 1) + " biome frame missing");
                Assert.That(frame.childCount, Is.GreaterThanOrEqualTo(18), "Stage " + (index + 1) + " frame is too sparse");
                Transform gate = frame.Find("BiomeGateTop");
                Assert.That(gate, Is.Not.Null, "Stage " + (index + 1) + " gate marker missing");
                Renderer[] renderers = frame.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(18));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.size.y, Is.GreaterThan(2.2f), "Stage " + (index + 1) + " frame is not tall enough to read");
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.05f), "Stage " + (index + 1) + " frame is below ground");
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstFiveStagesHaveGroundedRouteCenterlineCues()
        {
            GameObject owner = new GameObject("RouteCenterlineTestOwner");
            GameObject cameraObject = new GameObject("RouteCenterlineTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            for (int index = 1; index <= 5; index++)
            {
                world.Build(CampaignCatalog.Get(index), camera);
                yield return null;

                Transform centerline = owner.transform.Find("ProceduralWorld/RouteCenterline_" + index.ToString("00"));
                Assert.That(centerline, Is.Not.Null, "Route centerline missing for stage " + index);
                Assert.That(centerline.childCount, Is.GreaterThanOrEqualTo(18), "Route cue set is incomplete for stage " + index);
                Renderer[] renderers = centerline.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(18));
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Assert.That(renderers[rendererIndex].bounds.min.y, Is.GreaterThan(0f),
                        "Route cue is below the playable lane for stage " + index);
                }
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstFiveStagesHaveGroundedNearFieldIdentityAnchors()
        {
            GameObject owner = new GameObject("NearFieldIdentityTestOwner");
            GameObject cameraObject = new GameObject("NearFieldIdentityTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            for (int index = 1; index <= 5; index++)
            {
                world.Build(CampaignCatalog.Get(index), camera);
                yield return null;

                Transform anchors = owner.transform.Find("ProceduralWorld/NearFieldIdentity_" + index.ToString("00"));
                Assert.That(anchors, Is.Not.Null, "Near-field identity missing for stage " + index);
                Assert.That(anchors.childCount, Is.GreaterThanOrEqualTo(6), "Near-field identity is too sparse for stage " + index);
                Renderer[] renderers = anchors.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(6));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.size.y, Is.GreaterThan(0.70f), "Near-field identity is too flat for stage " + index);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.05f), "Near-field identity is below ground for stage " + index);
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaterBiomesIncludeReadableOriginalLandmarks()
        {
            GameObject owner = new GameObject("LaterLandmarkTestOwner");
            GameObject cameraObject = new GameObject("LaterLandmarkTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);
            LevelDefinition[] levels =
            {
                CampaignCatalog.Get(5),
                CampaignCatalog.Get(7),
                CampaignCatalog.Get(8),
                CampaignCatalog.Get(9),
                CampaignCatalog.Get(10)
            };
            string[] landmarkNames =
            {
                "RuinGateLandmark",
                "HiveObeliskLandmark",
                "RuinGateLandmark",
                "ExtractionBeaconLandmark",
                "ExtractionBeaconLandmark"
            };

            for (int index = 0; index < levels.Length; index++)
            {
                world.Build(levels[index], camera);
                yield return null;

                Transform landmark = owner.transform.Find("ProceduralWorld/" + landmarkNames[index]);
                Assert.That(landmark, Is.Not.Null, landmarkNames[index] + " missing from later biome build");
                Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0));
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Assert.That(bounds.size.y, Is.GreaterThan(0.50f), landmarkNames[index] + " is not readable at runtime");
                Assert.That(bounds.min.y, Is.GreaterThan(-0.05f), landmarkNames[index] + " is below the lane");
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryCampaignRouteBuildIncludesSignatureMarkers()
        {
            GameObject owner = new GameObject("RouteSignatureTestOwner");
            GameObject cameraObject = new GameObject("RouteSignatureTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            for (int index = 0; index < CampaignCatalog.All.Count; index++)
            {
                LevelDefinition level = CampaignCatalog.All[index];
                world.Build(level, camera);
                yield return null;

                Transform signature = owner.transform.Find("ProceduralWorld/RouteSignature_" + level.Route);
                Assert.That(signature, Is.Not.Null, level.Route + " route signature missing");
                Assert.That(signature.childCount, Is.GreaterThanOrEqualTo(5), level.Route + " route signature is incomplete");
            }

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WildernessBuildHasOrganicGroundAndNoRoadObjects()
        {
            GameObject owner = new GameObject("WildernessGroundTestOwner");
            GameObject cameraObject = new GameObject("WildernessGroundTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            world.Build(CampaignCatalog.Get(3), camera);
            yield return null;

            Transform terrain = owner.transform.Find("ProceduralWorld");
            Assert.That(terrain, Is.Not.Null);
            Assert.That(terrain.Find("AlienGroundPatch"), Is.Not.Null);
            Assert.That(terrain.Find("AlienGroundCrust"), Is.Not.Null);
            Assert.That(terrain.Find("AlienGroundMound"), Is.Not.Null);
            Assert.That(terrain.Find("RouteCenterline_03/OrganicPathMarker"), Is.Not.Null);
            Assert.That(terrain.Find("ArenaSegment"), Is.Null);
            Assert.That(terrain.Find("LanePlate"), Is.Null);
            Assert.That(terrain.Find("SurfaceStripe"), Is.Null);

            world.Clear();
            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllTenCampaignStagesBuildWildernessTerrainAndKeepTheirEcology()
        {
            GameObject owner = new GameObject("AllStageWildernessTestOwner");
            GameObject cameraObject = new GameObject("AllStageWildernessTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);

            for (int index = 0; index < CampaignCatalog.All.Count; index++)
            {
                LevelDefinition level = CampaignCatalog.All[index];
                world.Build(level, camera);
                yield return null;

                Transform terrain = owner.transform.Find("ProceduralWorld");
                Assert.That(terrain, Is.Not.Null, "Procedural terrain missing for stage " + level.Index);
                Assert.That(terrain.Find("AlienGroundPatch"), Is.Not.Null,
                    "Organic ground missing for stage " + level.Index);
                Assert.That(terrain.Find("RouteCenterline_" + level.Index.ToString("00") + "/OrganicPathMarker"), Is.Not.Null,
                    "Organic route marker missing for stage " + level.Index);
                Assert.That(terrain.Find("ArenaSegment"), Is.Null, "Road segment leaked into stage " + level.Index);
                Assert.That(terrain.Find("LanePlate"), Is.Null, "Lane plate leaked into stage " + level.Index);
                Assert.That(terrain.Find("SurfaceStripe"), Is.Null, "Road stripe leaked into stage " + level.Index);
                Assert.That(CampaignCatalog.EcologyBrief(level.Ecology), Does.Contain("//"));
                world.Clear();
            }

            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProceduralAudioBuildsLayeredMusicAndCombatEffects()
        {
            GameObject audioObject = new GameObject("ProceduralAudioTestObject");
            GameObject listenerObject = new GameObject("ProceduralAudioTestListener");
            listenerObject.AddComponent<AudioListener>();
            ProceduralAudio audio = audioObject.AddComponent<ProceduralAudio>();
            audio.Initialize();
            audio.SetMusic("combat", 0.5f);
            yield return null;

            Assert.That(audio.EffectClipCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(audio.MusicLayerCount, Is.EqualTo(4));
            Assert.That(audio.ActiveMusicMode, Does.Contain("combat"));

            Object.Destroy(audioObject);
            Object.Destroy(listenerObject);
            yield return null;
        }

        private static bool IsScreenVisible(Camera camera, Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 point = camera.WorldToScreenPoint(target.position);
            return point.z > 0f && point.x >= 0f && point.x <= Screen.width
                && point.y >= 0f && point.y <= Screen.height;
        }

        private static bool IsScreenVisible(Camera camera, Transform target, Rect viewport)
        {
            if (camera == null || target == null)
            {
                return false;
            }

            Vector3 point = camera.WorldToScreenPoint(target.position);
            return point.z > 0f && point.x >= viewport.xMin && point.x <= viewport.xMax
                && point.y >= viewport.yMin && point.y <= viewport.yMax;
        }

        private static bool IsRendererBoundsVisible(Camera camera, Transform target)
        {
            if (camera == null || target == null)
            {
                return false;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
                    new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z)
                };
                for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(corners[cornerIndex]);
                    if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator LaterFiveStagesHaveDistinctGroundedNearFieldIdentityAnchors()
        {
            GameObject owner = new GameObject("LaterNearFieldIdentityTestOwner");
            GameObject cameraObject = new GameObject("LaterNearFieldIdentityTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ProceduralWorld world = new ProceduralWorld(owner.transform);
            string[] expectedNames = { "NearFieldFaultBase", "NearFieldHiveNode", "NearFieldNightMarker", "NearFieldBeaconBase", "NearFieldRingSegment" };

            for (int index = 6; index <= 10; index++)
            {
                world.Build(CampaignCatalog.Get(index), camera);
                Transform anchors = owner.transform.Find("ProceduralWorld/NearFieldIdentity_" + index.ToString("00"));
                Assert.That(anchors, Is.Not.Null, "Later near-field identity missing for stage " + index);
                Assert.That(anchors.childCount, Is.GreaterThanOrEqualTo(6), "Later near-field identity is too sparse for stage " + index);
                Assert.That(anchors.Find(expectedNames[index - 6] + "_00"), Is.Not.Null,
                    "Stage " + index + " is missing its authored near-field marker");

                Renderer[] renderers = anchors.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.InRange(6, 12), "Later near-field renderer budget exceeded for stage " + index);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Bounds bounds = renderers[rendererIndex].bounds;
                    Assert.That(bounds.min.y, Is.GreaterThan(-0.08f),
                        "Later near-field identity is below the lane for stage " + index);
                    Assert.That(bounds.max.y, Is.LessThan(3.2f),
                        "Later near-field identity is too tall for portrait readability on stage " + index);
                }

                world.Clear();
            }

            Object.Destroy(owner);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllCampaignLevelsOpenTheirRuntimeStoryAndCombatPath()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            ProgressData unlocked = new ProgressData
            {
                UnlockedLevel = CampaignCatalog.All.Count,
                CompletedLevelMask = (1 << CampaignCatalog.All.Count) - 1
            };
            LocalProgress.Save(unlocked);
            game.ShowTitle();

            for (int level = 1; level <= CampaignCatalog.All.Count; level++)
            {
                game.OpenLevelSelect();
                for (int step = 0; step < level; step++)
                {
                    game.SelectNextLevel();
                }

                game.OpenSelectedStory();
                Assert.That(game.State, Is.EqualTo(GameFlowState.Story), "Story did not open for level " + level);
                Assert.That(game.CurrentLevelIndex, Is.EqualTo(level));
                yield return null;

                game.BeginPendingRun();
                Assert.That(game.State, Is.EqualTo(GameFlowState.Playing), "Combat did not start for level " + level);
                Assert.That(game.ActiveEnemyCount, Is.LessThanOrEqualTo(game.MaxActiveEnemyCount));
                Assert.That(game.ActiveCardCount, Is.LessThanOrEqualTo(game.MaxActiveCardCount));
                game.AbortRun();
                yield return null;
            }

            game.AbortRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SustainedRunKeepsEnemyAndCardCapsBounded()
        {
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            game.ShowTitle();
            game.OpenEndlessStory();
            game.BeginPendingRun();
            yield return new WaitForSeconds(9f);

            Assert.That(game.ActiveEnemyCount, Is.LessThanOrEqualTo(game.MaxActiveEnemyCount));
            Assert.That(game.PeakEnemyCount, Is.LessThanOrEqualTo(game.MaxActiveEnemyCount));
            Assert.That(game.ActiveCardCount, Is.LessThanOrEqualTo(game.MaxActiveCardCount));
            Assert.That(game.PeakCardCount, Is.LessThanOrEqualTo(game.MaxActiveCardCount));

            game.AbortRun();
            yield return null;
            Assert.That(game.PooledEnemyCount, Is.LessThanOrEqualTo(32));
            Assert.That(game.PooledCardCount, Is.LessThanOrEqualTo(4));
        }

        [UnityTest]
        public IEnumerator AllCampaignLevelsCompleteTheirRuntimeTerminalPath()
        {
            string savedProgress = PlayerPrefs.GetString(LocalProgress.SaveKey, string.Empty);
            string savedProgressBackup = PlayerPrefs.GetString(LocalProgress.BackupSaveKey, string.Empty);
            NodnarbGame game = NodnarbBootstrap.EnsureGame();
            yield return null;

            try
            {
                LocalProgress.Save(new ProgressData
                {
                    UnlockedLevel = CampaignCatalog.All.Count,
                    CompletedLevelMask = (1 << CampaignCatalog.All.Count) - 1
                });
                game.ShowTitle();

                for (int level = 1; level <= CampaignCatalog.All.Count; level++)
                {
                    game.OpenLevelSelect();
                    int navigationSteps = 0;
                    while (game.CurrentLevelIndex != level && navigationSteps < CampaignCatalog.All.Count)
                    {
                        game.SelectNextLevel();
                        navigationSteps++;
                    }

                    Assert.That(game.CurrentLevelIndex, Is.EqualTo(level));
                    game.OpenSelectedStory();
                    Assert.That(game.CurrentLevelIndex, Is.EqualTo(level));
                    game.BeginPendingRun();
                    yield return null;
                    Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
                    game.Model.SecureExtraction();
                    yield return null;
                    Assert.That(game.State, Is.EqualTo(GameFlowState.Result));
                    Assert.That(game.LastBalanceReport, Does.Contain("level=" + level));
                }
            }
            finally
            {
                game.ShowTitle();
                RestoreString(LocalProgress.SaveKey, savedProgress);
                RestoreString(LocalProgress.BackupSaveKey, savedProgressBackup);
                PlayerPrefs.Save();
            }
        }

        private static void RestoreString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.DeleteKey(key);
            }
            else
            {
                PlayerPrefs.SetString(key, value);
            }
        }
    }
}
