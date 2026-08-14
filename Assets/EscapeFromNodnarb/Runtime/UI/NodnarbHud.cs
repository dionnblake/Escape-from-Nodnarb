using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromNodnarb
{
    public sealed class ResultViewData
    {
        public RunEndReason Reason;
        public bool Victory;
        public bool Endless;
        public int Level;
        public int Score;
        public int Kills;
        public int Salvage;
        public int WeaponPickups;
        public int RecruitPickups;
        public int Squad;
        public int Overflow;
        public float Elapsed;
        public bool CanAdvance;
    }

    public sealed class NodnarbHud
    {
        private NodnarbGame game;
        private RectTransform canvasRoot;
        private RectTransform titleScreen;
        private RectTransform storyScreen;
        private RectTransform gameHud;
        private RectTransform pauseScreen;
        private RectTransform resultScreen;
        private RectTransform loadoutScreen;
        private RectTransform levelScreen;
        private CanvasGroup titleInteraction;

        private Text titleContinueText;
        private Text titleProgressText;
        private Text storySector;
        private Text storyTitle;
        private Text storyRoute;
        private Text storyBiome;
        private Text storyMessage;
        private RectTransform storyFrameOnePanel;
        private RectTransform storyFrameTwoPanel;
        private Text hudHealth;
        private Image hudHealthFill;
        private Text hudSector;
        private Text hudTimer;
        private Text hudSquad;
        private Text hudPower;
        private Text hudScore;
        private Text hudThreat;
        private Text inputHint;
        private Text leftCardRail;
        private Text rightCardRail;
        private Text cardChoiceHint;
        private RectTransform cardChoiceBand;
        private Image cardWeaponPanel;
        private Image cardCrewPanel;
        private Image cardWeaponAccent;
        private Image cardCrewAccent;
        private Image cardChoiceCenterRule;
        private Text cardWeaponTitle;
        private Text cardCrewTitle;
        private Text onboardingHint;
        private Text combatWarning;
        private Text abilityText;
        private Button abilityButton;
        private Image hitOverlay;
        private float hitOverlayAlpha;
        private Text pickupFeedback;
        private Color pickupFeedbackColor;
        private float pickupFeedbackTimer;
        private float combatWarningTimer;
        private float onboardingTimer;
        private int onboardingStep;
        private const float OnboardingDuration = 12f;

        private Text resultStatus;
        private Text resultReason;
        private Text resultTime;
        private Text resultKills;
        private Text resultSquad;
        private Text resultScore;
        private Text resultSalvage;
        private Text resultPickups;
        private Button resultNextButton;
        private Text resultNextText;

        private Text levelSector;
        private Text levelName;
        private Text levelState;
        private Text levelBest;
        private Text levelBrief;
        private Button levelDeployButton;
        private Text levelRouteCaption;
        private RectTransform levelRouteMap;
        private readonly Image[] levelRouteNodes = new Image[10];
        private readonly Image[] levelRouteLinks = new Image[9];
        private readonly Text[] levelRouteLabels = new Text[10];

        private Text loadoutWeapon;
        private Text loadoutWeaponDescription;
        private Text loadoutWeaponState;
        private Text loadoutSuit;
        private Text loadoutSuitState;
        private Text loadoutCredits;
        private Text loadoutSound;
        private Text loadoutMusic;
        private Text loadoutHaptics;
        private Text loadoutCaptions;
        private Text loadoutHighContrast;
        private Text loadoutReducedMotion;
        private Text audioCaption;
        private float audioCaptionTimer;
        private float gameplayTextRefreshTimer;
        private readonly Dictionary<Button, ColorBlock> defaultButtonColors = new Dictionary<Button, ColorBlock>();
        private readonly Dictionary<Text, Color> defaultTextColors = new Dictionary<Text, Color>();

        public void Initialize(NodnarbGame owner)
        {
            game = owner;
            canvasRoot = UiFactory.CreateCanvas(owner.transform);
            BuildAllScreens();
        }

        public IEnumerator InitializeRoutine(NodnarbGame owner)
        {
            game = owner;
            canvasRoot = UiFactory.CreateCanvas(owner.transform);

            // Android must get a visible first screen before the remaining
            // runtime-only UI is constructed.
            BuildTitle();
            SetTitleInteractive(false);
            yield return null;
        }

        public IEnumerator ContinueInitializeRoutine()
        {
            BuildStory();
            storyScreen.gameObject.SetActive(false);
            yield return null;
            BuildHud();
            gameHud.gameObject.SetActive(false);
            yield return null;
            BuildPause();
            pauseScreen.gameObject.SetActive(false);
            yield return null;
            BuildResult();
            resultScreen.gameObject.SetActive(false);
            yield return null;
            BuildLevelSelect();
            levelScreen.gameObject.SetActive(false);
            yield return null;
            BuildLoadout();
            loadoutScreen.gameObject.SetActive(false);
            HideAll();
        }

        private void BuildAllScreens()
        {
            BuildTitle();
            BuildStory();
            BuildHud();
            BuildPause();
            BuildResult();
            BuildLevelSelect();
            BuildLoadout();
            HideAll();
        }

        public void SetTitleInteractive(bool interactive)
        {
            if (titleInteraction == null)
            {
                return;
            }

            titleInteraction.interactable = interactive;
            titleInteraction.blocksRaycasts = interactive;
        }

        public void ShowTitle(ProgressData progress)
        {
            HideAll();
            titleScreen.gameObject.SetActive(true);
            titleContinueText.text = progress.PendingRun != null
                ? "RESUME SIGNAL"
                : progress.UnlockedLevel > 1 ? "CONTINUE // SECTOR " + progress.UnlockedLevel.ToString("00") : "BEGIN DISTRESS RUN";
            titleProgressText.text = progress.PendingRun != null
                ? "PAUSED RUN // TAP TO RESUME   //   CAMPAIGN " + progress.HighestCompletedLevel().ToString("00") + "/10"
                : "CAMPAIGN " + progress.HighestCompletedLevel().ToString("00") + "/10   //   RUNS "
                    + (progress.CampaignRuns + progress.EndlessRuns).ToString("000") + "   //   REPLAYS "
                    + progress.ReplayRuns.ToString("000");
            ApplyAccessibilitySettings();
        }

        public void ShowStory(LevelDefinition level, bool endless)
        {
            HideAll();
            storyScreen.gameObject.SetActive(true);
            storySector.text = endless ? "DEAD SIGNAL // ENDLESS" : "SECTOR " + level.Sector + " // " + level.Name.ToUpperInvariant();
            storyTitle.text = level.StoryTitle;
            storyRoute.text = "ROUTE // " + CampaignCatalog.RouteBrief(level.Route)
                + "\nPATH // " + CampaignCatalog.RouteBeat(level);
            storyBiome.text = "BIOME // " + CampaignCatalog.BiomeBrief(level.Biome)
                + "\nOBJECTIVE // " + CampaignCatalog.ObjectiveBrief(level)
                + "\n" + CampaignCatalog.EcologyBrief(level.Ecology)
                + "\n" + CampaignCatalog.BossBrief(level);
            StoryboardBeat beat = endless ? StoryboardCatalog.Endless : StoryboardCatalog.Get(level.Index);
            storyMessage.text = "MILESTONE // " + CampaignCatalog.MilestoneBrief(level)
                + "\n\n" + beat.FrameOne
                + "\n" + level.RadioMessage;
            BiomePalette palette = GameTheme.GetBiome(level.Biome);
            string frameCode = endless ? "ENDLESS" : level.Index.ToString("00");
            RefreshStoryboardPanel(storyFrameOnePanel, palette, false, "FRAME 01", beat.FrameOne, frameCode);
            RefreshStoryboardPanel(storyFrameTwoPanel, palette, true, "FRAME 02", beat.FrameTwo, frameCode);
            ApplyAccessibilitySettings();
        }

        public void ShowGameplay(bool showOnboarding)
        {
            HideAll();
            gameHud.gameObject.SetActive(true);
            hitOverlayAlpha = 0f;
            SetOverlayAlpha(0f);
            HidePickupFeedback();
            HideCombatWarning();
            onboardingTimer = showOnboarding ? OnboardingDuration : 0f;
            onboardingStep = 0;
            onboardingHint.gameObject.SetActive(showOnboarding);
            cardChoiceHint.gameObject.SetActive(false);
            cardChoiceBand.gameObject.SetActive(false);
            audioCaptionTimer = 0f;
            audioCaption.gameObject.SetActive(false);
            gameplayTextRefreshTimer = 0f;
            ApplyAccessibilitySettings();
        }

        public bool NoteOnboardingAction(int action)
        {
            if (onboardingTimer <= 0f)
            {
                return false;
            }

            if (action == 0 && onboardingStep == 0)
            {
                onboardingStep = 1;
            }
            else if (action == 1 && onboardingStep <= 1)
            {
                onboardingStep = 2;
            }
            else if (action == 2 && onboardingStep >= 2)
            {
                onboardingStep = 3;
                onboardingTimer = 0f;
                onboardingHint.gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        public void ShowPause()
        {
            pauseScreen.gameObject.SetActive(true);
        }

        public void HidePause()
        {
            pauseScreen.gameObject.SetActive(false);
        }

        public void ShowResult(ResultViewData data)
        {
            HideAll();
            resultScreen.gameObject.SetActive(true);
            resultStatus.text = data.Victory ? "EXTRACTION SECURED" : data.Endless ? "SIGNAL ENDED" : "RUN FAILED";
            resultStatus.color = data.Victory ? GameTheme.AccessibleSignal : GameTheme.AccessibleDanger;
            resultReason.text = ReasonCopy(data.Reason, data.Endless);
            resultTime.text = FormatTime(data.Elapsed);
            resultKills.text = data.Kills.ToString("000");
            resultSquad.text = data.Squad.ToString("00") + (data.Overflow > 0 ? " +" + data.Overflow : string.Empty);
            resultScore.text = data.Score.ToString("000000");
            resultSalvage.text = "+" + data.Salvage.ToString("000");
            resultPickups.text = "CREW " + data.RecruitPickups.ToString("00")
                + " // WEAPON " + data.WeaponPickups.ToString("00");
            resultNextButton.onClick.RemoveAllListeners();
            if (data.CanAdvance)
            {
                resultNextButton.onClick.AddListener(game.AdvanceAfterResult);
                resultNextButton.gameObject.SetActive(true);
                resultNextText.text = "NEXT SECTOR";
            }
            else if (!data.Victory)
            {
                resultNextButton.onClick.AddListener(game.RetryRun);
                resultNextButton.gameObject.SetActive(true);
                resultNextText.text = data.Endless ? "RETRY SIGNAL" : "RETRY SECTOR";
            }
            else
            {
                resultNextButton.gameObject.SetActive(false);
                resultNextText.text = data.Level >= 10 ? "FINAL SIGNAL" : "NEXT SECTOR";
            }
            ApplyAccessibilitySettings();
        }

        public void ShowLevelSelect(ProgressData progress, int selectedLevel)
        {
            HideAll();
            levelScreen.gameObject.SetActive(true);
            RefreshLevelSelect(progress, selectedLevel);
            ApplyAccessibilitySettings();
        }

        public void RefreshLevelSelect(ProgressData progress, int selectedLevel)
        {
            LevelDefinition level = CampaignCatalog.Get(selectedLevel);
            bool unlocked = selectedLevel <= progress.UnlockedLevel;
            bool complete = progress.IsLevelCompleted(selectedLevel);
            levelSector.text = "SECTOR " + level.Sector;
            levelName.text = level.Name.ToUpperInvariant();
            levelState.text = !unlocked ? "LOCKED // CLEAR THE PREVIOUS SECTOR" : complete ? "CLEARED // REPLAY AVAILABLE" : "DISTRESS ROUTE OPEN";
            levelState.color = !unlocked ? GameTheme.Muted : complete ? GameTheme.Signal : GameTheme.Text;
            levelBest.text = "BEST SCORE   " + progress.BestScores[selectedLevel - 1].ToString("000000") + "\nRUN LENGTH   " + Mathf.RoundToInt(level.DurationSeconds) + " SEC\nTHREAT       " + Mathf.RoundToInt(level.Difficulty * 100f).ToString("00") + "%";
            levelBrief.text = "PATH // " + CampaignCatalog.RouteBeat(level) + "\n" + CampaignCatalog.BossBrief(level);
            levelDeployButton.interactable = unlocked;
            levelRouteCaption.text = "ESCAPE ROUTE  //  " + progress.HighestCompletedLevel().ToString("00") + "/10 SECURED  //  SELECTED N-" + selectedLevel.ToString("00");

            for (int index = 0; index < levelRouteNodes.Length; index++)
            {
                int routeLevel = index + 1;
                bool routeUnlocked = routeLevel <= progress.UnlockedLevel;
                bool routeComplete = progress.IsLevelCompleted(routeLevel);
                bool routeSelected = routeLevel == selectedLevel;
                Image node = levelRouteNodes[index];
                float center = 0.08f + index * (0.84f / 9f);
                float routeY = CampaignCatalog.RouteMapY(CampaignCatalog.Get(routeLevel).Route);
                node.rectTransform.anchorMin = new Vector2(center - 0.018f, routeY - 0.06f);
                node.rectTransform.anchorMax = new Vector2(center + 0.018f, routeY + 0.06f);
                node.color = routeComplete ? GameTheme.Signal : routeUnlocked ? GameTheme.Weapon : GameTheme.Rule;
                node.rectTransform.localScale = routeSelected ? Vector3.one * 1.14f : Vector3.one;
                levelRouteLabels[index].color = routeSelected ? GameTheme.Text : routeUnlocked ? GameTheme.Muted : GameTheme.Rule;
                levelRouteLabels[index].text = routeLevel.ToString("00");
                levelRouteLabels[index].rectTransform.anchorMin = new Vector2(center - 0.036f, routeY - 0.20f);
                levelRouteLabels[index].rectTransform.anchorMax = new Vector2(center + 0.036f, routeY - 0.08f);
                if (index < levelRouteLinks.Length)
                {
                    levelRouteLinks[index].color = progress.IsLevelCompleted(routeLevel) ? GameTheme.Signal : GameTheme.Rule;
                }
            }

            PositionRouteLinks();
        }

        public void ShowLoadout(ProgressData progress)
        {
            HideAll();
            loadoutScreen.gameObject.SetActive(true);
            RefreshLoadout(progress);
            RefreshSettings();
        }

        public void RefreshLoadout(ProgressData progress)
        {
            int highest = progress.HighestCompletedLevel();
            WeaponDefinition weapon = LoadoutCatalog.Weapons[progress.SelectedWeapon];
            SuitDefinition suit = LoadoutCatalog.Suits[progress.SelectedSuit];
            loadoutWeapon.text = weapon.Name.ToUpperInvariant();
            loadoutWeaponDescription.text = weapon.Description.ToUpperInvariant() + "\nDMG " + Mathf.RoundToInt(weapon.BaseDamage * 100f) + "   //   RATE " + Mathf.RoundToInt(weapon.FireRateMultiplier * 100f);
            loadoutWeaponState.text = LoadoutCatalog.IsWeaponUnlocked(progress.SelectedWeapon, highest) ? "EQUIPPED" : "CLEAR LEVEL " + weapon.UnlockAfterLevel;
            loadoutWeaponState.color = LoadoutCatalog.IsWeaponUnlocked(progress.SelectedWeapon, highest) ? GameTheme.Signal : GameTheme.Muted;
            loadoutSuit.text = suit.Name.ToUpperInvariant();
            loadoutSuitState.text = LoadoutCatalog.IsSuitUnlocked(progress.SelectedSuit, highest) ? "EQUIPPED" : "CLEAR LEVEL " + suit.UnlockAfterLevel;
            loadoutSuitState.color = LoadoutCatalog.IsSuitUnlocked(progress.SelectedSuit, highest) ? GameTheme.Signal : GameTheme.Muted;
            loadoutCredits.text = "LOCAL SALVAGE   " + progress.Credits.ToString("0000") + "\nPOWER IS EARNED, NEVER SOLD";
        }

        public void RefreshSettings()
        {
            if (loadoutSound == null)
            {
                return;
            }

            loadoutSound.text = NodnarbSettings.Indicator("SFX", NodnarbSettings.SoundEnabled);
            loadoutMusic.text = NodnarbSettings.Indicator("MUSIC", NodnarbSettings.MusicEnabled);
            loadoutHaptics.text = NodnarbSettings.Indicator("HAPTICS", NodnarbSettings.HapticsEnabled);
            loadoutCaptions.text = NodnarbSettings.Indicator("CAPTIONS", NodnarbSettings.CaptionsEnabled);
            loadoutHighContrast.text = NodnarbSettings.Indicator("CONTRAST", NodnarbSettings.HighContrastEnabled);
            loadoutReducedMotion.text = NodnarbSettings.Indicator("MOTION", !NodnarbSettings.ReducedMotionEnabled);
            loadoutReducedMotion.color = NodnarbSettings.ReducedMotionEnabled ? GameTheme.Muted : GameTheme.Text;
            if (audioCaption != null && !NodnarbSettings.CaptionsEnabled)
            {
                audioCaptionTimer = 0f;
                audioCaption.gameObject.SetActive(false);
            }

            ApplyAccessibilitySettings();
        }

        private void ApplyAccessibilitySettings()
        {
            if (canvasRoot == null)
            {
                return;
            }

            bool highContrast = NodnarbSettings.HighContrastEnabled;
            Button[] buttons = canvasRoot.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                if (button == null)
                {
                    continue;
                }

                if (!defaultButtonColors.ContainsKey(button))
                {
                    defaultButtonColors.Add(button, button.colors);
                }

                ColorBlock colors = defaultButtonColors[button];
                if (highContrast)
                {
                    colors.normalColor = GameTheme.AccessibleSignal;
                    colors.highlightedColor = GameTheme.AccessibleSignalBright;
                    colors.pressedColor = GameTheme.AccessibleSignalBright;
                    colors.selectedColor = GameTheme.Text;
                    colors.disabledColor = GameTheme.Rule;
                    colors.colorMultiplier = 1f;
                }
                button.colors = highContrast ? colors : defaultButtonColors[button];
            }

            Text[] texts = canvasRoot.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                Text text = texts[index];
                if (text == null)
                {
                    continue;
                }

                if (!defaultTextColors.ContainsKey(text))
                {
                    defaultTextColors.Add(text, text.color);
                }

                text.color = highContrast ? GameTheme.Text : defaultTextColors[text];
            }

            if (hudHealthFill != null)
            {
                hudHealthFill.color = GameTheme.AccessibleSignal;
            }
            if (hudThreat != null && hudThreat.gameObject.activeSelf)
            {
                hudThreat.color = GameTheme.AccessibleDanger;
            }
            if (cardChoiceHint != null && cardChoiceHint.gameObject.activeSelf)
            {
                cardChoiceHint.color = GameTheme.AccessibleSignalBright;
            }
            if (cardWeaponAccent != null)
            {
                cardWeaponAccent.color = highContrast ? GameTheme.Text : GameTheme.WeaponUpgrade;
            }
            if (cardCrewAccent != null)
            {
                cardCrewAccent.color = highContrast ? Color.white : GameTheme.SignalBright;
            }
            if (cardChoiceCenterRule != null)
            {
                cardChoiceCenterRule.color = highContrast ? Color.white : GameTheme.Rule;
            }
            if (combatWarning != null && combatWarning.gameObject.activeSelf)
            {
                combatWarning.color = GameTheme.AccessibleDanger;
            }
            if (audioCaption != null && audioCaption.gameObject.activeSelf)
            {
                audioCaption.color = GameTheme.AccessibleSignalBright;
            }
            if (abilityButton != null && abilityButton.image != null && !abilityButton.interactable)
            {
                abilityButton.image.color = highContrast ? GameTheme.Text : GameTheme.SurfaceRaised;
            }
        }

        public void UpdateGameplay(RunModel model, LevelDefinition level, float elapsed, bool endless, bool bossActive, int volleyCount, bool cardChoiceActive, float relativeX, float cardTimer)
        {
            hudHealthFill.fillAmount = Mathf.Clamp01(model.CaptainHealth / RunModel.MaxCaptainHealth);
            hudHealthFill.color = model.CaptainHealth > 30f ? GameTheme.AccessibleSignal : GameTheme.AccessibleDanger;

            gameplayTextRefreshTimer -= Time.unscaledDeltaTime;
            bool refreshGameplayText = gameplayTextRefreshTimer <= 0f;
            if (refreshGameplayText)
            {
                gameplayTextRefreshTimer = 0.10f;
                hudHealth.text = "CAPTAIN " + Mathf.CeilToInt(model.CaptainHealth).ToString("000");
                hudSector.text = endless ? "DEAD SIGNAL" : level.Sector + " // " + level.Name.ToUpperInvariant();
                hudTimer.text = endless ? "T+ " + FormatTime(elapsed) : FormatTime(Mathf.Max(0f, level.DurationSeconds - elapsed));
                hudSquad.text = "SQUAD " + model.SoldierCount.ToString("00") + "/12" + (model.OverflowRecruits > 0 ? "   OVERCHARGE +" + model.OverflowRecruits : string.Empty);
                hudPower.text = "PWR " + (model.WeaponLevel + 1).ToString("00") + "   //   RATE " + model.ShotsPerSecond.ToString("0.0") + "/S";
                hudScore.text = "SCORE " + model.Score.ToString("000000");
                string cardHint = cardChoiceActive
                    ? "SELECT A SIGNAL"
                    : cardTimer <= 1.8f
                        ? "CARD WINDOW IN " + Mathf.CeilToInt(Mathf.Max(0f, cardTimer)).ToString("00") + "s"
                        : "DRAG TO MOVE";
                inputHint.text = "VOLLEY " + Mathf.Max(0, volleyCount).ToString("00")
                    + "  //  AUTO-FIRE  //  " + cardHint;
                hudThreat.text = bossActive ? "BOSS CONTACT // " + level.BossName.ToUpperInvariant() : string.Empty;
                hudThreat.color = bossActive ? GameTheme.AccessibleDanger : GameTheme.Muted;
            }

            if (cardChoiceActive)
            {
                string hint = CardChoice.Hint(relativeX);
                if (cardChoiceHint.text != hint)
                {
                    cardChoiceHint.text = hint;
                }
                cardChoiceHint.color = relativeX <= -CardChoice.SideThreshold
                    ? GameTheme.AccessibleWeapon
                    : relativeX >= CardChoice.SideThreshold ? GameTheme.AccessibleSignalBright : GameTheme.Text;
                bool weaponFocused = relativeX <= -CardChoice.SideThreshold;
                bool crewFocused = relativeX >= CardChoice.SideThreshold;
                if (cardWeaponPanel != null)
                {
                    cardWeaponPanel.color = UiFactory.Alpha(
                        weaponFocused ? Color.Lerp(GameTheme.SurfaceChrome, GameTheme.WeaponUpgrade, 0.42f) : GameTheme.SurfaceChrome,
                        weaponFocused ? 0.98f : 0.86f);
                }
                if (cardCrewPanel != null)
                {
                    cardCrewPanel.color = UiFactory.Alpha(
                        crewFocused ? Color.Lerp(GameTheme.SurfaceChrome, GameTheme.Signal, 0.42f) : GameTheme.SurfaceChrome,
                        crewFocused ? 0.98f : 0.86f);
                }
                if (cardWeaponTitle != null)
                {
                    cardWeaponTitle.color = weaponFocused ? GameTheme.AccessibleWeapon : GameTheme.WeaponUpgrade;
                }
                if (cardCrewTitle != null)
                {
                    cardCrewTitle.color = crewFocused ? GameTheme.AccessibleSignalBright : GameTheme.SignalBright;
                }
                cardChoiceHint.gameObject.SetActive(true);
                cardChoiceBand.gameObject.SetActive(true);
                cardWeaponAccent.gameObject.SetActive(true);
                cardCrewAccent.gameObject.SetActive(true);
                inputHint.gameObject.SetActive(false);
                leftCardRail.gameObject.SetActive(false);
                rightCardRail.gameObject.SetActive(false);
            }
            else
            {
                cardChoiceHint.gameObject.SetActive(false);
                cardChoiceBand.gameObject.SetActive(false);
                cardWeaponAccent.gameObject.SetActive(false);
                cardCrewAccent.gameObject.SetActive(false);
                inputHint.gameObject.SetActive(true);
                leftCardRail.gameObject.SetActive(true);
                rightCardRail.gameObject.SetActive(true);
            }

            if (onboardingTimer > 0f && onboardingStep < 3)
            {
                onboardingTimer = Mathf.Max(0f, onboardingTimer - Time.unscaledDeltaTime);
                onboardingHint.text = onboardingStep == 0
                    ? "MOVE  //  DRAG CAPTAIN LEFT OR RIGHT"
                    : onboardingStep == 1
                        ? "AUTO-FIRE  //  LINE UP ENEMIES OR ONE CARD"
                        : "OVERDRIVE  //  TAP WHEN THE LINE GETS HOT";
                onboardingHint.gameObject.SetActive(onboardingTimer > 0f);
            }
            else if (onboardingHint != null)
            {
                onboardingHint.gameObject.SetActive(false);
            }

            if (model.RapidFireActive)
            {
                if (refreshGameplayText)
                {
                    abilityText.text = "OVERDRIVE  " + model.RapidFireRemaining.ToString("0.0") + "s";
                }
                abilityButton.interactable = false;
                abilityButton.image.color = NodnarbSettings.HighContrastEnabled ? GameTheme.AccessibleSignalBright : GameTheme.SignalButtonBright;
            }
            else if (model.RapidFireReady)
            {
                abilityText.text = "OVERDRIVE  READY";
                abilityButton.interactable = true;
                abilityButton.image.color = NodnarbSettings.HighContrastEnabled ? GameTheme.AccessibleSignal : GameTheme.SignalButton;
            }
            else
            {
                abilityText.text = "RECHARGE  " + Mathf.CeilToInt(model.RapidFireCooldownRemaining).ToString("00") + "s";
                abilityButton.interactable = false;
                abilityButton.image.color = NodnarbSettings.HighContrastEnabled ? GameTheme.Text : GameTheme.SurfaceRaised;
            }

            if (hitOverlayAlpha > 0f)
            {
                hitOverlayAlpha = Mathf.Max(0f, hitOverlayAlpha - Time.unscaledDeltaTime * 2.8f);
                SetOverlayAlpha(hitOverlayAlpha);
            }

            if (pickupFeedbackTimer > 0f)
            {
                pickupFeedbackTimer = Mathf.Max(0f, pickupFeedbackTimer - Time.unscaledDeltaTime);
                Color color = pickupFeedbackColor;
                color.a = Mathf.Clamp01(pickupFeedbackTimer / 0.40f);
                pickupFeedback.color = color;
                if (pickupFeedbackTimer <= 0f)
                {
                    pickupFeedback.gameObject.SetActive(false);
                }
            }

            if (combatWarningTimer > 0f)
            {
                combatWarningTimer = Mathf.Max(0f, combatWarningTimer - Time.unscaledDeltaTime);
                Color color = combatWarning.color;
                color.a = Mathf.Clamp01(combatWarningTimer / 0.28f);
                combatWarning.color = color;
                if (combatWarningTimer <= 0f)
                {
                    HideCombatWarning();
                }
            }

            if (audioCaptionTimer > 0f)
            {
                audioCaptionTimer = Mathf.Max(0f, audioCaptionTimer - Time.unscaledDeltaTime);
                Color color = GameTheme.AccessibleSignalBright;
                color.a = Mathf.Clamp01(audioCaptionTimer / 0.30f);
                audioCaption.color = color;
                if (audioCaptionTimer <= 0f)
                {
                    audioCaption.gameObject.SetActive(false);
                }
            }
        }

        public void ShowPickup(CardKind kind, RunModel model)
        {
            gameplayTextRefreshTimer = 0f;
            pickupFeedbackColor = kind == CardKind.Recruit ? GameTheme.AccessibleSignalBright : GameTheme.AccessibleWeapon;
            pickupFeedback.text = kind == CardKind.Recruit
                ? "CREW +1 // SQUAD " + model.SoldierCount.ToString("00") + "/12 // FIREPOWER UP"
                : "WEAPON +1 // PWR " + (model.WeaponLevel + 1).ToString("00") + " // RATE " + model.ShotsPerSecond.ToString("0.0") + "/S";
            pickupFeedback.fontSize = 22;
            pickupFeedbackTimer = 1.8f;
            pickupFeedback.color = pickupFeedbackColor;
            pickupFeedback.gameObject.SetActive(true);
        }

        public void PulseDamage()
        {
            hitOverlayAlpha = 0.32f;
            SetOverlayAlpha(hitOverlayAlpha);
        }

        public void ShowAudioCaption(string caption)
        {
            if (!NodnarbSettings.CaptionsEnabled || audioCaption == null || string.IsNullOrEmpty(caption))
            {
                return;
            }

            audioCaption.text = "CAPTION // " + caption;
            audioCaption.color = GameTheme.AccessibleSignalBright;
            audioCaptionTimer = 1.40f;
            audioCaption.gameObject.SetActive(true);
        }

        public void ShowBossTelegraph(string bossName)
        {
            gameplayTextRefreshTimer = 0f;
            combatWarning.text = "INCOMING VOLLEY // " + bossName.ToUpperInvariant();
            combatWarningTimer = 0.72f;
            combatWarning.color = GameTheme.AccessibleDanger;
            combatWarning.gameObject.SetActive(true);
        }

        public void HideAll()
        {
            if (titleScreen != null) titleScreen.gameObject.SetActive(false);
            if (storyScreen != null) storyScreen.gameObject.SetActive(false);
            if (gameHud != null) gameHud.gameObject.SetActive(false);
            if (pauseScreen != null) pauseScreen.gameObject.SetActive(false);
            if (resultScreen != null) resultScreen.gameObject.SetActive(false);
            if (loadoutScreen != null) loadoutScreen.gameObject.SetActive(false);
            if (levelScreen != null) levelScreen.gameObject.SetActive(false);
            HideCombatWarning();
            if (cardChoiceHint != null)
            {
                cardChoiceHint.gameObject.SetActive(false);
            }

            if (cardChoiceBand != null)
            {
                cardChoiceBand.gameObject.SetActive(false);
            }

            if (cardWeaponAccent != null)
            {
                cardWeaponAccent.gameObject.SetActive(false);
            }

            if (cardCrewAccent != null)
            {
                cardCrewAccent.gameObject.SetActive(false);
            }

            if (onboardingHint != null)
            {
                onboardingHint.gameObject.SetActive(false);
            }

            onboardingTimer = 0f;
            onboardingStep = 0;
        }

        private void HidePickupFeedback()
        {
            pickupFeedbackTimer = 0f;
            if (pickupFeedback != null)
            {
                pickupFeedback.gameObject.SetActive(false);
            }
        }

        private void HideCombatWarning()
        {
            combatWarningTimer = 0f;
            if (combatWarning != null)
            {
                combatWarning.gameObject.SetActive(false);
            }
        }

        private void BuildTitle()
        {
            titleScreen = UiFactory.Panel(canvasRoot, "TitleScreen", GameTheme.Void);
            titleInteraction = titleScreen.gameObject.AddComponent<CanvasGroup>();
            if (!BuildTitleArtwork(titleScreen))
            {
                BuildSignalIllustration(titleScreen);
            }

            RectTransform top = UiFactory.Panel(titleScreen, "SignalHeader", UiFactory.Alpha(GameTheme.Surface, 0.94f));
            UiFactory.Anchor(top, new Vector2(0f, 0.925f), Vector2.one, new Vector2(32f, 0f), new Vector2(-32f, 0f));
            Text signal = UiFactory.Label(top, "Signal", "NODNARB // DISTRESS", 27, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Stretch(signal.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));

            RectTransform lower = UiFactory.Panel(titleScreen, "TitleConsole", GameTheme.Surface);
            UiFactory.Anchor(lower, new Vector2(0.085f, 0.040f), new Vector2(0.915f, 0.235f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            Image consoleSignal = UiFactory.Rule(lower, "ConsoleSignal", GameTheme.Signal);
            UiFactory.Anchor(consoleSignal.rectTransform, new Vector2(0f, 0.98f), Vector2.one, Vector2.zero, Vector2.zero);

            titleProgressText = UiFactory.Label(lower, "Progress", string.Empty, 19, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(titleProgressText.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 0.96f), new Vector2(18f, 0f), new Vector2(-18f, 0f));

            Button continueButton = UiFactory.ActionButton(lower, "Continue", "BEGIN DISTRESS RUN", game.ContinueCampaign, true, 28);
            titleContinueText = continueButton.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)continueButton.transform, new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.74f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            Button levelsButton = UiFactory.ActionButton(lower, "Levels", "MAP", game.OpenLevelSelect, false, 20);
            UiFactory.Anchor((RectTransform)levelsButton.transform, new Vector2(0.05f, 0.12f), new Vector2(0.31f, 0.34f), Vector2.zero, Vector2.zero);
            Button endlessButton = UiFactory.ActionButton(lower, "Endless", "ENDLESS", game.OpenEndlessStory, false, 20);
            UiFactory.Anchor((RectTransform)endlessButton.transform, new Vector2(0.37f, 0.12f), new Vector2(0.63f, 0.34f), Vector2.zero, Vector2.zero);
            Button loadoutButton = UiFactory.ActionButton(lower, "Loadout", "LOADOUT", game.OpenLoadout, false, 20);
            UiFactory.Anchor((RectTransform)loadoutButton.transform, new Vector2(0.69f, 0.12f), new Vector2(0.95f, 0.34f), Vector2.zero, Vector2.zero);
        }

        private static bool BuildTitleArtwork(RectTransform parent)
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/title-screen");
            if (texture == null)
            {
                return false;
            }

            GameObject artworkObject = new GameObject("TitleArtwork", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            artworkObject.transform.SetParent(parent, false);
            RectTransform artwork = artworkObject.GetComponent<RectTransform>();
            UiFactory.Stretch(artwork);
            RawImage image = artworkObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            AspectRatioFitter fitter = artworkObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
            return true;
        }

        private void BuildSignalIllustration(RectTransform parent)
        {
            RectTransform art = new GameObject("SignalIllustration", typeof(RectTransform)).GetComponent<RectTransform>();
            art.SetParent(parent, false);
            UiFactory.Anchor(art, new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Image horizon = UiFactory.Rule(art, "Horizon", UiFactory.Alpha(GameTheme.Rule, 0.8f));
            UiFactory.Anchor(horizon.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.285f), Vector2.zero, Vector2.zero);
            Image signal = UiFactory.Rule(art, "Beacon", GameTheme.Signal);
            UiFactory.Anchor(signal.rectTransform, new Vector2(0.49f, 0.16f), new Vector2(0.51f, 0.83f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                Image bar = UiFactory.Rule(art, "SignalBar" + i, UiFactory.Alpha(GameTheme.Signal, 0.8f - i * 0.18f));
                float width = 0.10f + i * 0.09f;
                float y = 0.66f - i * 0.12f;
                UiFactory.Anchor(bar.rectTransform, new Vector2(0.5f - width, y), new Vector2(0.5f + width, y + 0.012f), Vector2.zero, Vector2.zero);
            }
            Image wreck = UiFactory.Rule(art, "WreckVector", GameTheme.Weapon);
            UiFactory.Anchor(wreck.rectTransform, new Vector2(0.08f, 0.21f), new Vector2(0.39f, 0.25f), Vector2.zero, Vector2.zero);
            wreck.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -9f);
        }

        private void BuildStory()
        {
            storyScreen = UiFactory.Panel(canvasRoot, "StoryScreen", UiFactory.Alpha(GameTheme.Void, 0.46f));
            storySector = UiFactory.Label(storyScreen, "Sector", string.Empty, 25, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(storySector.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.94f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            storyTitle = UiFactory.Label(storyScreen, "StoryTitle", string.Empty, 68, GameTheme.Text, TextAnchor.LowerLeft);
            UiFactory.Anchor(storyTitle.rectTransform, new Vector2(0f, 0.67f), new Vector2(1f, 0.88f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            storyRoute = UiFactory.Label(storyScreen, "Route", string.Empty, 21, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(storyRoute.rectTransform, new Vector2(0f, 0.53f), new Vector2(1f, 0.61f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            storyBiome = UiFactory.Label(storyScreen, "Biome", string.Empty, 17, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(storyBiome.rectTransform, new Vector2(0f, 0.44f), new Vector2(1f, 0.53f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Image storyTrim = UiFactory.Rule(storyScreen, "StoryTrim", UiFactory.Alpha(GameTheme.Signal, 0.82f));
            UiFactory.Anchor(storyTrim.rectTransform, new Vector2(0.05f, 0.635f), new Vector2(0.95f, 0.642f), Vector2.zero, Vector2.zero);

            storyFrameOnePanel = BuildStoryboardPanel(storyScreen, "FrameOnePanel");
            UiFactory.Anchor(storyFrameOnePanel, new Vector2(0.05f, 0.28f), new Vector2(0.475f, 0.45f), Vector2.zero, Vector2.zero);
            storyFrameTwoPanel = BuildStoryboardPanel(storyScreen, "FrameTwoPanel");
            UiFactory.Anchor(storyFrameTwoPanel, new Vector2(0.525f, 0.28f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);

            RectTransform radio = UiFactory.Panel(storyScreen, "Radio", UiFactory.Alpha(GameTheme.Surface, 0.96f));
            UiFactory.Anchor(radio, new Vector2(0f, 0.10f), new Vector2(1f, 0.28f), new Vector2(48f, 0f), new Vector2(-48f, 0f));
            Text radioLabel = UiFactory.Label(radio, "RadioLabel", "INCOMING RADIO", 22, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(radioLabel.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 0.92f), new Vector2(34f, 0f), new Vector2(-34f, 0f));
            storyMessage = UiFactory.Label(radio, "Message", string.Empty, 23, GameTheme.Text, TextAnchor.UpperLeft);
            storyMessage.lineSpacing = 1.0f;
            UiFactory.Anchor(storyMessage.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.70f), new Vector2(34f, 0f), new Vector2(-34f, 0f));

            Button deploy = UiFactory.ActionButton(storyScreen, "Deploy", "DEPLOY SQUAD", game.BeginPendingRun, true);
            UiFactory.Anchor((RectTransform)deploy.transform, new Vector2(0f, 0.045f), new Vector2(1f, 0.09f), new Vector2(48f, 0f), new Vector2(-48f, 0f));
            Button back = UiFactory.ActionButton(storyScreen, "Back", "BACK", game.ShowTitle, false);
            UiFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 0.012f), new Vector2(1f, 0.036f), new Vector2(48f, 0f), new Vector2(-48f, 0f));
        }

        private static RectTransform BuildStoryboardPanel(Transform parent, string name)
        {
            RectTransform panel = UiFactory.Panel(parent, name, UiFactory.Alpha(GameTheme.Surface, 0.96f), false);
            Image inner = UiFactory.Rule(panel, "PanelInner", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.78f));
            UiFactory.Anchor(inner.rectTransform, new Vector2(0.025f, 0.055f), new Vector2(0.975f, 0.95f), Vector2.zero, Vector2.zero);
            Image rim = UiFactory.Rule(panel, "PanelRim", GameTheme.Rule);
            UiFactory.Anchor(rim.rectTransform, Vector2.zero, new Vector2(1f, 0.035f), Vector2.zero, Vector2.zero);
            Image horizon = UiFactory.Rule(panel, "PanelHorizon", GameTheme.CanyonHighlight);
            UiFactory.Anchor(horizon.rectTransform, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);
            Image ground = UiFactory.Rule(panel, "PanelGround", GameTheme.Void);
            UiFactory.Anchor(ground.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.20f), Vector2.zero, Vector2.zero);
            Image terrainLeft = UiFactory.Rule(panel, "PanelTerrainLeft", GameTheme.CanyonHighlight);
            UiFactory.Anchor(terrainLeft.rectTransform, new Vector2(0.04f, 0.20f), new Vector2(0.28f, 0.58f), Vector2.zero, Vector2.zero);
            terrainLeft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Image terrainRight = UiFactory.Rule(panel, "PanelTerrainRight", GameTheme.AlienViolet);
            UiFactory.Anchor(terrainRight.rectTransform, new Vector2(0.72f, 0.20f), new Vector2(0.96f, 0.67f), Vector2.zero, Vector2.zero);
            terrainRight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Image terrainCut = UiFactory.Rule(panel, "PanelTerrainCut", GameTheme.SurfaceGlassDeep);
            UiFactory.Anchor(terrainCut.rectTransform, new Vector2(0.18f, 0.22f), new Vector2(0.38f, 0.48f), Vector2.zero, Vector2.zero);
            terrainCut.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            Image threat = UiFactory.Rule(panel, "PanelThreat", GameTheme.AlienGlow);
            UiFactory.Anchor(threat.rectTransform, new Vector2(0.40f, 0.29f), new Vector2(0.62f, 0.62f), Vector2.zero, Vector2.zero);
            threat.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image threatCore = UiFactory.Rule(panel, "PanelThreatCore", GameTheme.AlienGlow);
            UiFactory.Anchor(threatCore.rectTransform, new Vector2(0.46f, 0.40f), new Vector2(0.56f, 0.50f), Vector2.zero, Vector2.zero);
            threatCore.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image signal = UiFactory.Rule(panel, "PanelSignal", GameTheme.SignalBright);
            UiFactory.Anchor(signal.rectTransform, new Vector2(0.48f, 0.22f), new Vector2(0.52f, 0.82f), Vector2.zero, Vector2.zero);
            Image signalTop = UiFactory.Rule(panel, "PanelSignalTop", GameTheme.SignalBright);
            UiFactory.Anchor(signalTop.rectTransform, new Vector2(0.34f, 0.76f), new Vector2(0.66f, 0.80f), Vector2.zero, Vector2.zero);
            Image signalCore = UiFactory.Rule(panel, "PanelSignalCore", GameTheme.SignalBright);
            UiFactory.Anchor(signalCore.rectTransform, new Vector2(0.455f, 0.72f), new Vector2(0.545f, 0.81f), Vector2.zero, Vector2.zero);
            signalCore.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image scan = UiFactory.Rule(panel, "PanelScan", UiFactory.Alpha(GameTheme.SignalBright, 0.44f));
            UiFactory.Anchor(scan.rectTransform, new Vector2(0.06f, 0.31f), new Vector2(0.94f, 0.325f), Vector2.zero, Vector2.zero);
            scan.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            Image foreground = UiFactory.Rule(panel, "PanelForeground", GameTheme.Void);
            UiFactory.Anchor(foreground.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.18f), Vector2.zero, Vector2.zero);
            foreground.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            Image dustOne = UiFactory.Rule(panel, "PanelDustOne", GameTheme.SignalBright);
            UiFactory.Anchor(dustOne.rectTransform, new Vector2(0.17f, 0.52f), new Vector2(0.20f, 0.55f), Vector2.zero, Vector2.zero);
            dustOne.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            Image dustTwo = UiFactory.Rule(panel, "PanelDustTwo", GameTheme.AlienGlow);
            UiFactory.Anchor(dustTwo.rectTransform, new Vector2(0.78f, 0.42f), new Vector2(0.805f, 0.445f), Vector2.zero, Vector2.zero);
            dustTwo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Text caption = UiFactory.Label(panel, "Caption", "FRAME", 13, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(caption.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            Text title = UiFactory.Label(panel, "Title", string.Empty, 15, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
            return panel;
        }

        private static void RefreshStoryboardPanel(RectTransform panel, BiomePalette palette, bool threatFrame, string frameLabel, string title, string frameCode)
        {
            if (panel == null)
            {
                return;
            }

            panel.GetComponent<Image>().color = UiFactory.Alpha(Color.Lerp(palette.Sky, palette.Ground, 0.32f), 0.96f);
            SetPanelImage(panel, "PanelInner", UiFactory.Alpha(Color.Lerp(palette.Sky, GameTheme.Void, 0.72f), 0.84f));
            SetPanelImage(panel, "PanelRim", threatFrame ? GameTheme.AlienGlow : GameTheme.Signal);
            SetPanelImage(panel, "PanelHorizon", palette.Terrain);
            SetPanelImage(panel, "PanelGround", Color.Lerp(palette.Sky, GameTheme.Void, 0.38f));
            SetPanelImage(panel, "PanelTerrainLeft", palette.Terrain);
            SetPanelImage(panel, "PanelTerrainRight", threatFrame ? GameTheme.AlienViolet : palette.Detail);
            SetPanelImage(panel, "PanelTerrainCut", Color.Lerp(palette.Sky, palette.Terrain, 0.32f));
            SetPanelImage(panel, "PanelThreat", threatFrame ? GameTheme.AlienGlow : Color.Lerp(palette.Detail, GameTheme.AlienViolet, 0.42f));
            SetPanelImage(panel, "PanelThreatCore", threatFrame ? GameTheme.Danger : palette.Detail);
            SetPanelImage(panel, "PanelSignal", threatFrame ? GameTheme.Danger : GameTheme.SignalBright);
            SetPanelImage(panel, "PanelSignalTop", threatFrame ? GameTheme.Danger : GameTheme.SignalBright);
            SetPanelImage(panel, "PanelSignalCore", threatFrame ? GameTheme.Danger : GameTheme.SignalBright);
            SetPanelImage(panel, "PanelScan", UiFactory.Alpha(threatFrame ? GameTheme.Danger : GameTheme.SignalBright, 0.46f));
            SetPanelImage(panel, "PanelForeground", Color.Lerp(palette.Sky, GameTheme.Void, 0.72f));
            SetPanelImage(panel, "PanelDustOne", UiFactory.Alpha(threatFrame ? GameTheme.Danger : GameTheme.SignalBright, 0.72f));
            SetPanelImage(panel, "PanelDustTwo", UiFactory.Alpha(threatFrame ? GameTheme.AlienGlow : palette.Detail, 0.76f));
            Text caption = panel.Find("Caption").GetComponent<Text>();
            caption.text = frameLabel + "  //  " + frameCode;
            caption.color = threatFrame ? GameTheme.Danger : GameTheme.Signal;
            panel.Find("Title").GetComponent<Text>().text = title;
        }

        private static void SetPanelImage(Transform panel, string childName, Color color)
        {
            Transform child = panel.Find(childName);
            if (child != null)
            {
                child.GetComponent<Image>().color = color;
            }
        }

        private void BuildHud()
        {
            gameHud = UiFactory.Panel(canvasRoot, "GameHud", Color.clear, false);
            RectTransform top = UiFactory.Panel(gameHud, "TopRail", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.82f));
            UiFactory.Anchor(top, new Vector2(0f, 0.925f), Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            Image topSignal = UiFactory.Rule(top, "TopSignal", GameTheme.Signal);
            UiFactory.Anchor(topSignal.rectTransform, Vector2.zero, new Vector2(1f, 0.035f), Vector2.zero, Vector2.zero);
            hudHealth = UiFactory.Label(top, "Health", "CAPTAIN 100", 22, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(hudHealth.rectTransform, new Vector2(0f, 0.48f), new Vector2(0.34f, 1f), new Vector2(22f, 0f), Vector2.zero);
            Image healthBack = UiFactory.Rule(top, "HealthBack", GameTheme.Rule);
            UiFactory.Anchor(healthBack.rectTransform, new Vector2(0f, 0.20f), new Vector2(0.31f, 0.34f), new Vector2(22f, 0f), Vector2.zero);
            hudHealthFill = UiFactory.Rule(healthBack.transform, "HealthFill", GameTheme.Signal);
            UiFactory.Stretch(hudHealthFill.rectTransform);
            hudHealthFill.type = Image.Type.Filled;
            hudHealthFill.fillMethod = Image.FillMethod.Horizontal;
            hudHealthFill.fillOrigin = 0;
            hudSector = UiFactory.Label(top, "Sector", "N-01", 21, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(hudSector.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.64f, 1f), Vector2.zero, Vector2.zero);
            hudTimer = UiFactory.Label(top, "Timer", "01:06", 25, GameTheme.Text, TextAnchor.MiddleRight);
            UiFactory.Anchor(hudTimer.rectTransform, new Vector2(0.64f, 0f), new Vector2(0.82f, 1f), Vector2.zero, Vector2.zero);
            Button pause = UiFactory.ActionButton(top, "Pause", "PAUSE", game.TogglePause, false, 14);
            UiFactory.Anchor((RectTransform)pause.transform, new Vector2(0.875f, 0.31f), new Vector2(0.985f, 0.73f), Vector2.zero, Vector2.zero);

            RectTransform threatBand = UiFactory.Panel(gameHud, "ThreatBand", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.34f), false);
            UiFactory.Anchor(threatBand, new Vector2(0.06f, 0.862f), new Vector2(0.94f, 0.916f), Vector2.zero, Vector2.zero);
            hudThreat = UiFactory.Label(gameHud, "Threat", string.Empty, 22, GameTheme.Danger, TextAnchor.MiddleCenter);
            UiFactory.Anchor(hudThreat.rectTransform, new Vector2(0.08f, 0.868f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);
            RectTransform warningBand = UiFactory.Panel(gameHud, "WarningBand", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.40f), false);
            UiFactory.Anchor(warningBand, new Vector2(0.08f, 0.807f), new Vector2(0.92f, 0.854f), Vector2.zero, Vector2.zero);
            combatWarning = UiFactory.Label(gameHud, "CombatWarning", string.Empty, 18, GameTheme.Danger, TextAnchor.MiddleCenter);
            UiFactory.Anchor(combatWarning.rectTransform, new Vector2(0.10f, 0.812f), new Vector2(0.90f, 0.849f), Vector2.zero, Vector2.zero);
            HideCombatWarning();

            pickupFeedback = UiFactory.Label(gameHud, "PickupFeedback", string.Empty, 26, GameTheme.Signal, TextAnchor.MiddleCenter);
            pickupFeedback.rectTransform.localScale = Vector3.one;
            UiFactory.Anchor(pickupFeedback.rectTransform, new Vector2(0.06f, 0.745f), new Vector2(0.94f, 0.79f), Vector2.zero, Vector2.zero);
            HidePickupFeedback();

            leftCardRail = UiFactory.Label(gameHud, "LeftRail", "<  WEAPON +1", 18, GameTheme.WeaponUpgrade, TextAnchor.MiddleLeft);
            UiFactory.Anchor(leftCardRail.rectTransform, new Vector2(0.035f, 0.79f), new Vector2(0.35f, 0.83f), Vector2.zero, Vector2.zero);
            rightCardRail = UiFactory.Label(gameHud, "RightRail", "+1 CREW  >", 18, GameTheme.SignalBright, TextAnchor.MiddleRight);
            UiFactory.Anchor(rightCardRail.rectTransform, new Vector2(0.65f, 0.79f), new Vector2(0.965f, 0.83f), Vector2.zero, Vector2.zero);

            RectTransform actionRail = UiFactory.Panel(gameHud, "ActionRail", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.72f), false);
            UiFactory.Anchor(actionRail, new Vector2(0.015f, 0.012f), new Vector2(0.985f, 0.108f), Vector2.zero, Vector2.zero);
            Image actionRule = UiFactory.Rule(actionRail, "ActionRule", GameTheme.Rule);
            UiFactory.Anchor(actionRule.rectTransform, Vector2.zero, new Vector2(1f, 0.018f), Vector2.zero, Vector2.zero);

            RectTransform stats = UiFactory.Panel(gameHud, "RunStats", UiFactory.Alpha(GameTheme.SurfaceGlass, 0.78f));
            UiFactory.Anchor(stats, new Vector2(0.035f, 0.025f), new Vector2(0.555f, 0.085f), Vector2.zero, Vector2.zero);
            Image statsSignal = UiFactory.Rule(stats, "StatsSignal", GameTheme.Signal);
            UiFactory.Anchor(statsSignal.rectTransform, Vector2.zero, new Vector2(0.012f, 1f), Vector2.zero, Vector2.zero);
            hudSquad = UiFactory.Label(stats, "Squad", "SQUAD 02/12", 22, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(hudSquad.rectTransform, new Vector2(0f, 0.50f), Vector2.one, new Vector2(20f, 0f), Vector2.zero);
            hudPower = UiFactory.Label(stats, "Power", "PWR 01", 19, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(hudPower.rectTransform, Vector2.zero, new Vector2(1f, 0.50f), new Vector2(20f, 0f), Vector2.zero);
            hudScore = UiFactory.Label(gameHud, "Score", "SCORE 000000", 18, GameTheme.Muted, TextAnchor.MiddleRight);
            UiFactory.Anchor(hudScore.rectTransform, new Vector2(0.66f, 0.091f), new Vector2(0.965f, 0.119f), Vector2.zero, Vector2.zero);
            inputHint = UiFactory.Label(gameHud, "InputHint", "VOLLEY 03  //  AUTO-FIRE ACTIVE", 15, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(inputHint.rectTransform, new Vector2(0.08f, 0.116f), new Vector2(0.92f, 0.141f), Vector2.zero, Vector2.zero);

            cardChoiceBand = UiFactory.Panel(gameHud, "CardChoiceBand", UiFactory.Alpha(GameTheme.SurfaceGlassDeep, 0.88f), false);
            UiFactory.Anchor(cardChoiceBand, new Vector2(0.035f, 0.655f), new Vector2(0.965f, 0.765f), Vector2.zero, Vector2.zero);
            RectTransform weaponPanel = UiFactory.Panel(cardChoiceBand, "WeaponCard", UiFactory.Alpha(GameTheme.SurfaceChrome, 0.86f), false);
            UiFactory.Anchor(weaponPanel, new Vector2(0.025f, 0.10f), new Vector2(0.425f, 0.90f), Vector2.zero, Vector2.zero);
            cardWeaponPanel = weaponPanel.GetComponent<Image>();
            Outline weaponTrim = weaponPanel.gameObject.AddComponent<Outline>();
            weaponTrim.effectColor = UiFactory.Alpha(GameTheme.WeaponUpgrade, 0.72f);
            weaponTrim.effectDistance = new Vector2(1f, -1f);
            weaponTrim.useGraphicAlpha = true;
            cardWeaponTitle = UiFactory.Label(weaponPanel, "Title", "WEAPON  +1", 19, GameTheme.WeaponUpgrade, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cardWeaponTitle.rectTransform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
            Text weaponMeta = UiFactory.Label(weaponPanel, "Meta", "LEFT SIGNAL", 11, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(weaponMeta.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.29f), Vector2.zero, Vector2.zero);
            RectTransform crewPanel = UiFactory.Panel(cardChoiceBand, "CrewCard", UiFactory.Alpha(GameTheme.SurfaceChrome, 0.86f), false);
            UiFactory.Anchor(crewPanel, new Vector2(0.575f, 0.10f), new Vector2(0.975f, 0.90f), Vector2.zero, Vector2.zero);
            cardCrewPanel = crewPanel.GetComponent<Image>();
            Outline crewTrim = crewPanel.gameObject.AddComponent<Outline>();
            crewTrim.effectColor = UiFactory.Alpha(GameTheme.SignalBright, 0.72f);
            crewTrim.effectDistance = new Vector2(1f, -1f);
            crewTrim.useGraphicAlpha = true;
            cardCrewTitle = UiFactory.Label(crewPanel, "Title", "+1  CREW", 19, GameTheme.SignalBright, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cardCrewTitle.rectTransform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
            Text crewMeta = UiFactory.Label(crewPanel, "Meta", "RIGHT SIGNAL", 11, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(crewMeta.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.29f), Vector2.zero, Vector2.zero);
            cardChoiceCenterRule = UiFactory.Rule(cardChoiceBand, "ChoiceSpine", UiFactory.Alpha(GameTheme.Rule, 0.92f));
            UiFactory.Anchor(cardChoiceCenterRule.rectTransform, new Vector2(0.495f, 0.16f), new Vector2(0.505f, 0.84f), Vector2.zero, Vector2.zero);
            cardWeaponAccent = UiFactory.Rule(cardChoiceBand, "WeaponAccent", GameTheme.WeaponUpgrade);
            UiFactory.Anchor(cardWeaponAccent.rectTransform, new Vector2(0.008f, 0.10f), new Vector2(0.014f, 0.90f), Vector2.zero, Vector2.zero);
            cardCrewAccent = UiFactory.Rule(cardChoiceBand, "CrewAccent", GameTheme.SignalBright);
            UiFactory.Anchor(cardCrewAccent.rectTransform, new Vector2(0.986f, 0.10f), new Vector2(0.992f, 0.90f), Vector2.zero, Vector2.zero);
            cardChoiceHint = UiFactory.Label(gameHud, "CardChoiceHint", string.Empty, 16, GameTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cardChoiceHint.rectTransform, new Vector2(0.18f, 0.675f), new Vector2(0.82f, 0.745f), Vector2.zero, Vector2.zero);
            cardChoiceHint.gameObject.SetActive(false);
            cardChoiceBand.gameObject.SetActive(false);
            cardWeaponAccent.gameObject.SetActive(false);
            cardCrewAccent.gameObject.SetActive(false);

            audioCaption = UiFactory.Label(gameHud, "AudioCaption", string.Empty, 16, GameTheme.SignalBright, TextAnchor.MiddleCenter);
            UiFactory.Anchor(audioCaption.rectTransform, new Vector2(0.05f, 0.645f), new Vector2(0.95f, 0.675f), Vector2.zero, Vector2.zero);
            audioCaption.gameObject.SetActive(false);

            onboardingHint = UiFactory.Label(gameHud, "OnboardingHint", string.Empty, 23, GameTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.Anchor(onboardingHint.rectTransform, new Vector2(0.06f, 0.585f), new Vector2(0.94f, 0.645f), Vector2.zero, Vector2.zero);
            onboardingHint.gameObject.SetActive(false);

            abilityButton = UiFactory.ActionButton(gameHud, "RapidFire", "OVERDRIVE  READY", game.ActivateRapidFire, true);
            abilityText = abilityButton.GetComponentInChildren<Text>();
            abilityText.fontSize = 18;
            UiFactory.Anchor((RectTransform)abilityButton.transform, new Vector2(0.60f, 0.028f), new Vector2(0.965f, 0.078f), Vector2.zero, Vector2.zero);

            hitOverlay = UiFactory.Rule(gameHud, "DamageOverlay", Color.clear);
            UiFactory.Stretch(hitOverlay.rectTransform);
            hitOverlay.raycastTarget = false;
        }

        private void BuildPause()
        {
            pauseScreen = UiFactory.Panel(canvasRoot, "PauseScreen", UiFactory.Alpha(GameTheme.Void, 0.82f));
            RectTransform console = UiFactory.Panel(pauseScreen, "PauseConsole", GameTheme.Surface);
            UiFactory.Anchor(console, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
            Text title = UiFactory.Label(console, "Title", "RUN PAUSED", 58, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(title.rectTransform, new Vector2(0f, 0.74f), new Vector2(1f, 0.94f), new Vector2(38f, 0f), new Vector2(-38f, 0f));
            Text note = UiFactory.Label(console, "Note", "LOCAL RUN // NO DATA LEAVES DEVICE", 22, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(note.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.74f), new Vector2(38f, 0f), new Vector2(-38f, 0f));
            Button resume = UiFactory.ActionButton(console, "Resume", "RESUME", game.TogglePause, true);
            UiFactory.Anchor((RectTransform)resume.transform, new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), new Vector2(38f, 0f), new Vector2(-38f, 0f));
            Button retry = UiFactory.ActionButton(console, "Retry", "RESTART SECTOR", game.RetryRun, false);
            UiFactory.Anchor((RectTransform)retry.transform, new Vector2(0f, 0.23f), new Vector2(1f, 0.39f), new Vector2(38f, 0f), new Vector2(-38f, 0f));
            Button abort = UiFactory.ActionButton(console, "Abort", "RETURN TO SIGNAL", game.AbortRun, false);
            UiFactory.Anchor((RectTransform)abort.transform, new Vector2(0f, 0.04f), new Vector2(1f, 0.20f), new Vector2(38f, 0f), new Vector2(-38f, 0f));
        }

        private void BuildResult()
        {
            resultScreen = UiFactory.Panel(canvasRoot, "ResultScreen", UiFactory.Alpha(GameTheme.Void, 0.94f));
            resultStatus = UiFactory.Label(resultScreen, "Status", "RUN FAILED", 61, GameTheme.Danger, TextAnchor.LowerLeft);
            UiFactory.Anchor(resultStatus.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.91f), new Vector2(58f, 0f), new Vector2(-58f, 0f));
            resultReason = UiFactory.Label(resultScreen, "Reason", "FRONT LINE BREACHED", 25, GameTheme.Muted, TextAnchor.UpperLeft);
            UiFactory.Anchor(resultReason.rectTransform, new Vector2(0f, 0.70f), new Vector2(1f, 0.78f), new Vector2(58f, 0f), new Vector2(-58f, 0f));

            RectTransform sheet = UiFactory.Panel(resultScreen, "SpecSheet", GameTheme.Surface);
            UiFactory.Anchor(sheet, new Vector2(0f, 0.36f), new Vector2(1f, 0.68f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            resultTime = AddResultRow(sheet, 0, "SURVIVAL", "00:00");
            resultKills = AddResultRow(sheet, 1, "CONTACTS", "000");
            resultSquad = AddResultRow(sheet, 2, "FINAL SQUAD", "00");
            resultScore = AddResultRow(sheet, 3, "SCORE", "000000");
            resultSalvage = AddResultRow(sheet, 4, "SALVAGE", "+000");
            resultPickups = AddResultRow(sheet, 5, "PICKUPS", "CREW 00 // WEAPON 00");

            resultNextButton = UiFactory.ActionButton(resultScreen, "Next", "NEXT SECTOR", game.AdvanceAfterResult, true);
            resultNextText = resultNextButton.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)resultNextButton.transform, new Vector2(0f, 0.23f), new Vector2(1f, 0.31f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Button replay = UiFactory.ActionButton(resultScreen, "Replay", "REPLAY", game.ReplayAfterResult, false);
            UiFactory.Anchor((RectTransform)replay.transform, new Vector2(0f, 0.13f), new Vector2(1f, 0.21f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Button menu = UiFactory.ActionButton(resultScreen, "Menu", "RETURN TO SIGNAL", game.ShowTitle, false);
            UiFactory.Anchor((RectTransform)menu.transform, new Vector2(0f, 0.03f), new Vector2(1f, 0.11f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
        }

        private void BuildLevelSelect()
        {
            levelScreen = UiFactory.Panel(canvasRoot, "LevelSelect", UiFactory.Alpha(GameTheme.Void, 0.92f));
            Text header = UiFactory.Label(levelScreen, "Header", "SECTOR MAP", 28, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, 0.89f), new Vector2(1f, 0.96f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            levelSector = UiFactory.Label(levelScreen, "Sector", "SECTOR N-01", 30, GameTheme.Muted, TextAnchor.LowerLeft);
            UiFactory.Anchor(levelSector.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 0.81f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            levelName = UiFactory.Label(levelScreen, "Name", "IMPACT SHELF", 66, GameTheme.Text, TextAnchor.UpperLeft);
            UiFactory.Anchor(levelName.rectTransform, new Vector2(0f, 0.60f), new Vector2(1f, 0.72f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            levelState = UiFactory.Label(levelScreen, "State", string.Empty, 26, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(levelState.rectTransform, new Vector2(0f, 0.53f), new Vector2(1f, 0.60f), new Vector2(54f, 0f), new Vector2(-54f, 0f));

            RectTransform routeMap = UiFactory.Panel(levelScreen, "RouteMap", UiFactory.Alpha(GameTheme.Surface, 0.98f));
            levelRouteMap = routeMap;
            UiFactory.Anchor(routeMap, new Vector2(0f, 0.34f), new Vector2(1f, 0.53f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            levelRouteCaption = UiFactory.Label(routeMap, "RouteCaption", "ESCAPE ROUTE  //  00/10 SECURED", 20, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(levelRouteCaption.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            levelBrief = UiFactory.Label(routeMap, "RouteBrief", string.Empty, 15, GameTheme.Signal, TextAnchor.UpperLeft);
            levelBrief.lineSpacing = 1.05f;
            UiFactory.Anchor(levelBrief.rectTransform, new Vector2(0f, 0.59f), new Vector2(1f, 0.77f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            Image routeTrack = UiFactory.Rule(routeMap, "RouteTrack", GameTheme.Rule);
            UiFactory.Anchor(routeTrack.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.46f), Vector2.zero, Vector2.zero);
            const float firstNodeCenter = 0.08f;
            const float nodeStep = 0.84f / 9f;
            for (int index = 0; index < levelRouteNodes.Length; index++)
            {
                float center = firstNodeCenter + index * nodeStep;
                float routeY = CampaignCatalog.RouteMapY(CampaignCatalog.Get(index + 1).Route);
                Image node = UiFactory.Rule(routeMap, "Node" + (index + 1).ToString("00"), GameTheme.Rule);
                UiFactory.Anchor(node.rectTransform, new Vector2(center - 0.018f, routeY - 0.06f), new Vector2(center + 0.018f, routeY + 0.06f), Vector2.zero, Vector2.zero);
                levelRouteNodes[index] = node;
                Text label = UiFactory.Label(routeMap, "NodeLabel" + (index + 1).ToString("00"), (index + 1).ToString("00"), 18, GameTheme.Muted, TextAnchor.MiddleCenter);
                UiFactory.Anchor(label.rectTransform, new Vector2(center - 0.036f, routeY - 0.20f), new Vector2(center + 0.036f, routeY - 0.08f), Vector2.zero, Vector2.zero);
                levelRouteLabels[index] = label;
                if (index < levelRouteLinks.Length)
                {
                    Image link = UiFactory.Rule(routeMap, "Link" + (index + 1).ToString("00"), GameTheme.Rule);
                    UiFactory.Anchor(link.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    levelRouteLinks[index] = link;
                }
            }

            RectTransform stats = UiFactory.Panel(levelScreen, "Stats", GameTheme.Surface);
            UiFactory.Anchor(stats, new Vector2(0f, 0.22f), new Vector2(1f, 0.32f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            levelBest = UiFactory.Label(stats, "Best", string.Empty, 31, GameTheme.Text, TextAnchor.MiddleLeft);
            levelBest.lineSpacing = 1.25f;
            UiFactory.Stretch(levelBest.rectTransform, new Vector2(32f, 20f), new Vector2(-32f, -20f));
            Button previous = UiFactory.ActionButton(levelScreen, "Previous", "< PREV", game.SelectPreviousLevel, false);
            UiFactory.Anchor((RectTransform)previous.transform, new Vector2(0f, 0.15f), new Vector2(0.48f, 0.20f), new Vector2(54f, 0f), new Vector2(-8f, 0f));
            Button next = UiFactory.ActionButton(levelScreen, "Next", "NEXT >", game.SelectNextLevel, false);
            UiFactory.Anchor((RectTransform)next.transform, new Vector2(0.52f, 0.15f), new Vector2(1f, 0.20f), new Vector2(8f, 0f), new Vector2(-54f, 0f));
            levelDeployButton = UiFactory.ActionButton(levelScreen, "Deploy", "OPEN STORY SIGNAL", game.OpenSelectedStory, true);
            UiFactory.Anchor((RectTransform)levelDeployButton.transform, new Vector2(0f, 0.08f), new Vector2(1f, 0.14f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Button back = UiFactory.ActionButton(levelScreen, "Back", "BACK", game.ShowTitle, false);
            UiFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 0.02f), new Vector2(1f, 0.07f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
        }

        private void PositionRouteLinks()
        {
            if (levelRouteMap == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            for (int index = 0; index < levelRouteLinks.Length; index++)
            {
                RectTransform first = levelRouteNodes[index].rectTransform;
                RectTransform second = levelRouteNodes[index + 1].rectTransform;
                Vector2 firstLocal = WorldToLocal(levelRouteMap, first.TransformPoint(first.rect.center));
                Vector2 secondLocal = WorldToLocal(levelRouteMap, second.TransformPoint(second.rect.center));
                Vector2 delta = secondLocal - firstLocal;
                RectTransform link = levelRouteLinks[index].rectTransform;
                link.anchorMin = new Vector2(0.5f, 0.5f);
                link.anchorMax = new Vector2(0.5f, 0.5f);
                link.pivot = new Vector2(0.5f, 0.5f);
                link.anchoredPosition = (firstLocal + secondLocal) * 0.5f;
                link.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(2f, levelRouteMap.rect.height * 0.012f));
                link.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private static Vector2 WorldToLocal(RectTransform parent, Vector3 worldPoint)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out localPoint);
            return localPoint;
        }

        private void BuildLoadout()
        {
            loadoutScreen = UiFactory.Panel(canvasRoot, "Loadout", UiFactory.Alpha(GameTheme.Void, 0.94f));
            Text header = UiFactory.Label(loadoutScreen, "Header", "FIELD LOADOUT", 30, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, 0.89f), new Vector2(1f, 0.96f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Button sound = UiFactory.ActionButton(loadoutScreen, "SoundSetting", string.Empty, game.ToggleSound, false, 13);
            loadoutSound = sound.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)sound.transform, new Vector2(0.52f, 0.895f), new Vector2(0.66f, 0.955f), Vector2.zero, Vector2.zero);
            Button music = UiFactory.ActionButton(loadoutScreen, "MusicSetting", string.Empty, game.ToggleMusic, false, 13);
            loadoutMusic = music.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)music.transform, new Vector2(0.67f, 0.895f), new Vector2(0.81f, 0.955f), Vector2.zero, Vector2.zero);
            Button haptics = UiFactory.ActionButton(loadoutScreen, "HapticsSetting", string.Empty, game.ToggleHaptics, false, 13);
            loadoutHaptics = haptics.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)haptics.transform, new Vector2(0.82f, 0.895f), new Vector2(0.985f, 0.955f), Vector2.zero, Vector2.zero);
            Button captions = UiFactory.ActionButton(loadoutScreen, "CaptionsSetting", string.Empty, game.ToggleCaptions, false, 13);
            loadoutCaptions = captions.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)captions.transform, new Vector2(0.05f, 0.835f), new Vector2(0.35f, 0.885f), Vector2.zero, Vector2.zero);
            Button highContrast = UiFactory.ActionButton(loadoutScreen, "HighContrastSetting", string.Empty, game.ToggleHighContrast, false, 13);
            loadoutHighContrast = highContrast.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)highContrast.transform, new Vector2(0.36f, 0.835f), new Vector2(0.65f, 0.885f), Vector2.zero, Vector2.zero);
            Button reducedMotion = UiFactory.ActionButton(loadoutScreen, "ReducedMotionSetting", string.Empty, game.ToggleReducedMotion, false, 13);
            loadoutReducedMotion = reducedMotion.GetComponentInChildren<Text>();
            UiFactory.Anchor((RectTransform)reducedMotion.transform, new Vector2(0.66f, 0.835f), new Vector2(0.95f, 0.885f), Vector2.zero, Vector2.zero);

            RectTransform weaponPanel = UiFactory.Panel(loadoutScreen, "WeaponPanel", GameTheme.Surface);
            UiFactory.Anchor(weaponPanel, new Vector2(0f, 0.52f), new Vector2(1f, 0.83f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Text weaponLabel = UiFactory.Label(weaponPanel, "Label", "PRIMARY WEAPON", 22, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(weaponLabel.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.94f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            loadoutWeapon = UiFactory.Label(weaponPanel, "Weapon", string.Empty, 48, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(loadoutWeapon.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.78f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            loadoutWeaponDescription = UiFactory.Label(weaponPanel, "Description", string.Empty, 25, GameTheme.Muted, TextAnchor.UpperLeft);
            UiFactory.Anchor(loadoutWeaponDescription.rectTransform, new Vector2(0f, 0.27f), new Vector2(1f, 0.56f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            loadoutWeaponState = UiFactory.Label(weaponPanel, "State", string.Empty, 23, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(loadoutWeaponState.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.24f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            Button prevWeapon = UiFactory.ActionButton(loadoutScreen, "PrevWeapon", "< WEAPON", game.SelectPreviousWeapon, false);
            UiFactory.Anchor((RectTransform)prevWeapon.transform, new Vector2(0f, 0.44f), new Vector2(0.48f, 0.50f), new Vector2(54f, 0f), new Vector2(-8f, 0f));
            Button nextWeapon = UiFactory.ActionButton(loadoutScreen, "NextWeapon", "WEAPON >", game.SelectNextWeapon, false);
            UiFactory.Anchor((RectTransform)nextWeapon.transform, new Vector2(0.52f, 0.44f), new Vector2(1f, 0.50f), new Vector2(8f, 0f), new Vector2(-54f, 0f));

            RectTransform suitPanel = UiFactory.Panel(loadoutScreen, "SuitPanel", GameTheme.Surface);
            UiFactory.Anchor(suitPanel, new Vector2(0f, 0.25f), new Vector2(1f, 0.40f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Text suitLabel = UiFactory.Label(suitPanel, "Label", "SUIT SIGNAL", 22, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(suitLabel.rectTransform, new Vector2(0f, 0.66f), new Vector2(1f, 0.94f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            loadoutSuit = UiFactory.Label(suitPanel, "Suit", string.Empty, 40, GameTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.Anchor(loadoutSuit.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 0.68f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            loadoutSuitState = UiFactory.Label(suitPanel, "State", string.Empty, 22, GameTheme.Signal, TextAnchor.MiddleLeft);
            UiFactory.Anchor(loadoutSuitState.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.30f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            Button prevSuit = UiFactory.ActionButton(loadoutScreen, "PrevSuit", "< SUIT", game.SelectPreviousSuit, false);
            UiFactory.Anchor((RectTransform)prevSuit.transform, new Vector2(0f, 0.17f), new Vector2(0.48f, 0.23f), new Vector2(54f, 0f), new Vector2(-8f, 0f));
            Button nextSuit = UiFactory.ActionButton(loadoutScreen, "NextSuit", "SUIT >", game.SelectNextSuit, false);
            UiFactory.Anchor((RectTransform)nextSuit.transform, new Vector2(0.52f, 0.17f), new Vector2(1f, 0.23f), new Vector2(8f, 0f), new Vector2(-54f, 0f));

            loadoutCredits = UiFactory.Label(loadoutScreen, "Credits", string.Empty, 22, GameTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.Anchor(loadoutCredits.rectTransform, new Vector2(0f, 0.09f), new Vector2(1f, 0.16f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
            Button back = UiFactory.ActionButton(loadoutScreen, "Back", "BACK", game.ShowTitle, false);
            UiFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 0.02f), new Vector2(1f, 0.08f), new Vector2(54f, 0f), new Vector2(-54f, 0f));
        }

        private static Text AddResultRow(RectTransform parent, int index, string label, string initialValue)
        {
            const int rows = 6;
            float rowHeight = 1f / rows;
            float top = 1f - index * rowHeight;
            float bottom = top - rowHeight;
            if (index > 0)
            {
                Image rule = UiFactory.Rule(parent, "Rule" + index, GameTheme.Rule);
                UiFactory.Anchor(rule.rectTransform, new Vector2(0f, top - 0.004f), new Vector2(1f, top), new Vector2(26f, 0f), new Vector2(-26f, 0f));
            }

            Text key = UiFactory.Label(parent, "Key" + index, label, 24, GameTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.Anchor(key.rectTransform, new Vector2(0f, bottom), new Vector2(0.55f, top), new Vector2(28f, 0f), Vector2.zero);
            Text value = UiFactory.Label(parent, "Value" + index, initialValue, 31, GameTheme.Text, TextAnchor.MiddleRight);
            UiFactory.Anchor(value.rectTransform, new Vector2(0.55f, bottom), new Vector2(1f, top), Vector2.zero, new Vector2(-28f, 0f));
            return value;
        }

        private void SetOverlayAlpha(float alpha)
        {
            hitOverlay.color = UiFactory.Alpha(GameTheme.Danger, alpha);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static string ReasonCopy(RunEndReason reason, bool endless)
        {
            if (endless && reason != RunEndReason.ExtractionSecured)
            {
                return "DEAD SIGNAL SCORE BANKED LOCALLY";
            }

            switch (reason)
            {
                case RunEndReason.CaptainDown:
                    return "CAPTAIN DOWN // SQUAD SIGNAL LOST";
                case RunEndReason.FrontLineBreached:
                    return "FRONT LINE BREACHED // POSITION OVERRUN";
                case RunEndReason.ExtractionSecured:
                    return "RESCUE VECTOR HELD // CREW RECOVERED";
                default:
                    return "RUN CLOSED";
            }
        }
    }
}
