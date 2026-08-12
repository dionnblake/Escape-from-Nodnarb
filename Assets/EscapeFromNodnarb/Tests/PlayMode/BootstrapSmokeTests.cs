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

        [SetUp]
        public void PreserveLocalProgress()
        {
            savedProgress = PlayerPrefs.GetString(LocalProgress.SaveKey, string.Empty);
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

            PlayerPrefs.Save();
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
                "Title startup should warm a small projectile set and grow it only when combat needs it.");
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
            Assert.That(LocalProgress.Load().OnboardingComplete, Is.True);

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
            Assert.That(captain.position.z, Is.LessThan(firstSoldier.position.z - 0.75f), "Captain must be in front of Crew_1 on the combat Z axis.");
            Assert.That(captain.position.z, Is.LessThan(secondSoldier.position.z - 0.75f), "Captain must be in front of Crew_2 on the combat Z axis.");
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

            EnemyAgent regular = game.GetComponentsInChildren<EnemyAgent>(true)[0];
            Assert.That(regular.HasHealthFeedback, Is.True);
            float before = regular.HealthRatio;
            Bounds regularBounds = GetBounds(regular.HealthFeedback);
            Assert.That(regularBounds.min.y, Is.GreaterThan(0.45f));
            Assert.That(regularBounds.max.y, Is.LessThan(4.2f));
            regular.ApplyDamage(1f);
            yield return null;
            Assert.That(regular.HealthRatio, Is.LessThan(before));

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
        public IEnumerator ReplayAfterResultReturnsToStoryAndStartsCleanRun()
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

            game.ReplayAfterResult();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Story));
            Assert.That(game.Model, Is.Null);

            game.BeginPendingRun();
            yield return null;
            Assert.That(game.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(game.Model.SoldierCount, Is.EqualTo(RunModel.StartingSoldiers));
            Assert.That(game.TotalFriendlyShotsFired, Is.EqualTo(0));
            game.AbortRun();
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

            weapon.ApplyDamage(0.8f);
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
            Assert.That(hud.Find("LeftRail").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("WEAPON LANE"));
            Assert.That(hud.Find("RightRail").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("CREW LANE"));
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
            yield return null;
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
    }
}
