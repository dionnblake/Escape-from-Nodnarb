using NUnit.Framework;
using UnityEngine;

namespace EscapeFromNodnarb.Tests
{
    public sealed class LocalProgressTests
    {
        private string savedProgress;
        private string savedProgressBackup;
        private string savedPreviousProgress;
        private string savedPreviousProgressBackup;
        private string savedLegacyProgress;
        private int savedSoundSetting;
        private int savedMusicSetting;
        private int savedHapticsSetting;
        private int savedCaptionsSetting;
        private int savedHighContrastSetting;
        private int savedReducedMotionSetting;

        [SetUp]
        public void PreserveLocalState()
        {
            savedProgress = PlayerPrefs.GetString(LocalProgress.SaveKey, string.Empty);
            savedProgressBackup = PlayerPrefs.GetString(LocalProgress.BackupSaveKey, string.Empty);
            savedPreviousProgress = PlayerPrefs.GetString(LocalProgress.PreviousSaveKey, string.Empty);
            savedPreviousProgressBackup = PlayerPrefs.GetString(LocalProgress.PreviousBackupSaveKey, string.Empty);
            savedLegacyProgress = PlayerPrefs.GetString("EscapeFromNodnarb.Save.v1", string.Empty);
            savedSoundSetting = PlayerPrefs.GetInt(NodnarbSettings.SoundKey, -1);
            savedMusicSetting = PlayerPrefs.GetInt(NodnarbSettings.MusicKey, -1);
            savedHapticsSetting = PlayerPrefs.GetInt(NodnarbSettings.HapticsKey, -1);
            savedCaptionsSetting = PlayerPrefs.GetInt(NodnarbSettings.CaptionsKey, -1);
            savedHighContrastSetting = PlayerPrefs.GetInt(NodnarbSettings.HighContrastKey, -1);
            savedReducedMotionSetting = PlayerPrefs.GetInt(NodnarbSettings.ReducedMotionKey, -1);
            PlayerPrefs.DeleteKey(LocalProgress.SaveKey);
            PlayerPrefs.DeleteKey(LocalProgress.BackupSaveKey);
            PlayerPrefs.DeleteKey(LocalProgress.PreviousSaveKey);
            PlayerPrefs.DeleteKey(LocalProgress.PreviousBackupSaveKey);
            PlayerPrefs.DeleteKey("EscapeFromNodnarb.Save.v1");
            PlayerPrefs.DeleteKey(NodnarbSettings.SoundKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.MusicKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.HapticsKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.CaptionsKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.HighContrastKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.ReducedMotionKey);
        }

        [TearDown]
        public void TearDown()
        {
            RestoreString(LocalProgress.SaveKey, savedProgress);
            RestoreString(LocalProgress.BackupSaveKey, savedProgressBackup);
            RestoreString(LocalProgress.PreviousSaveKey, savedPreviousProgress);
            RestoreString(LocalProgress.PreviousBackupSaveKey, savedPreviousProgressBackup);
            RestoreString("EscapeFromNodnarb.Save.v1", savedLegacyProgress);
            RestoreSetting(NodnarbSettings.SoundKey, savedSoundSetting);
            RestoreSetting(NodnarbSettings.MusicKey, savedMusicSetting);
            RestoreSetting(NodnarbSettings.HapticsKey, savedHapticsSetting);
            RestoreSetting(NodnarbSettings.CaptionsKey, savedCaptionsSetting);
            RestoreSetting(NodnarbSettings.HighContrastKey, savedHighContrastSetting);
            RestoreSetting(NodnarbSettings.ReducedMotionKey, savedReducedMotionSetting);
            PlayerPrefs.Save();
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

        [Test]
        public void VictoryUnlocksNextLevelAndKeepsBestScore()
        {
            ProgressData data = new ProgressData();
            data.RecordCampaignResult(1, 120, 10, true);
            data.RecordCampaignResult(1, 90, 5, false);

            Assert.That(data.IsLevelCompleted(1), Is.True);
            Assert.That(data.UnlockedLevel, Is.EqualTo(2));
            Assert.That(data.BestScores[0], Is.EqualTo(120));
            Assert.That(data.Credits, Is.EqualTo(15));
        }

        [Test]
        public void CampaignVictoriesUnlockOnlyTheNextSequentialStage()
        {
            ProgressData data = new ProgressData();

            for (int level = 1; level <= CampaignCatalog.All.Count; level++)
            {
                data.RecordCampaignResult(level, level * 100, level, true);

                Assert.That(data.IsLevelCompleted(level), Is.True);
                Assert.That(data.UnlockedLevel, Is.EqualTo(Mathf.Min(CampaignCatalog.All.Count, level + 1)));
            }

            Assert.That(data.CompletedLevelMask, Is.EqualTo((1 << CampaignCatalog.All.Count) - 1));
            Assert.That(data.HighestCompletedLevel(), Is.EqualTo(CampaignCatalog.All.Count));
        }

        [Test]
        public void EndlessResultDoesNotUnlockCampaignStages()
        {
            ProgressData data = new ProgressData();
            data.RecordEndlessResult(900, 12);

            Assert.That(data.UnlockedLevel, Is.EqualTo(1));
            Assert.That(data.CompletedLevelMask, Is.Zero);
            Assert.That(data.EndlessBest, Is.EqualTo(900));
            Assert.That(data.Credits, Is.EqualTo(12));
        }

        [Test]
        public void LocalRunCountersTrackReplaySignalWithoutExternalTelemetry()
        {
            ProgressData data = new ProgressData();
            data.RecordRunStart(false, false);
            data.RecordRunStart(true, true);
            data.RecordCampaignResult(1, 100, 3, true);

            Assert.That(data.CampaignRuns, Is.EqualTo(1));
            Assert.That(data.EndlessRuns, Is.EqualTo(1));
            Assert.That(data.ReplayRuns, Is.EqualTo(1));
            Assert.That(data.CampaignVictories, Is.EqualTo(1));
            Assert.That(data.IsValidSerialized(true), Is.True);
        }

        [Test]
        public void JsonRoundTripPreservesProgress()
        {
            ProgressData original = new ProgressData();
            original.OnboardingComplete = true;
            original.RecordCampaignResult(3, 777, 21, true);
            string json = JsonUtility.ToJson(original);
            ProgressData restored = JsonUtility.FromJson<ProgressData>(json);
            restored.Sanitize();

            Assert.That(restored.IsLevelCompleted(3), Is.True);
            Assert.That(restored.BestScores[2], Is.EqualTo(777));
            Assert.That(restored.Credits, Is.EqualTo(21));
            Assert.That(restored.OnboardingComplete, Is.True);
        }

        [Test]
        public void VersionTwoProgressMigratesToTheCurrentSaveKeys()
        {
            ProgressData previous = new ProgressData
            {
                Version = 2,
                UnlockedLevel = 4,
                Credits = 27,
                CompletedLevelMask = (1 << 3) - 1
            };
            string previousJson = JsonUtility.ToJson(previous);
            PlayerPrefs.SetString(LocalProgress.PreviousSaveKey, previousJson);
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.Version, Is.EqualTo(ProgressData.CurrentVersion));
            Assert.That(restored.UnlockedLevel, Is.EqualTo(4));
            Assert.That(restored.Credits, Is.EqualTo(27));
            Assert.That(PlayerPrefs.HasKey(LocalProgress.SaveKey), Is.True);
            Assert.That(PlayerPrefs.HasKey(LocalProgress.BackupSaveKey), Is.True);
            Assert.That(restored.IsValidSerialized(true), Is.True);
        }

        [Test]
        public void VersionOneProgressMigratesWithoutLosingUnlocksOrCredits()
        {
            ProgressData previous = new ProgressData
            {
                Version = 1,
                UnlockedLevel = 3,
                Credits = 14,
                CompletedLevelMask = (1 << 2) - 1
            };
            PlayerPrefs.SetString("EscapeFromNodnarb.Save.v1", JsonUtility.ToJson(previous));
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.Version, Is.EqualTo(ProgressData.CurrentVersion));
            Assert.That(restored.UnlockedLevel, Is.EqualTo(3));
            Assert.That(restored.Credits, Is.EqualTo(14));
            Assert.That(restored.CompletedLevelMask, Is.EqualTo((1 << 2) - 1));
            Assert.That(PlayerPrefs.HasKey(LocalProgress.SaveKey), Is.True);
            Assert.That(PlayerPrefs.HasKey(LocalProgress.BackupSaveKey), Is.True);
        }

        [Test]
        public void VersionTwoPendingRunMigratesAndRemainsResumable()
        {
            ProgressData previous = new ProgressData
            {
                Version = 2,
                UnlockedLevel = 2,
                PendingRun = new PausedRunData
                {
                    LevelIndex = 1,
                    CaptainHealth = 82f,
                    SoldierCount = 4,
                    SelectedWeapon = 0,
                    SelectedSuit = 0,
                    Elapsed = 18f,
                    Score = 90,
                    Kills = 2,
                    Salvage = 3,
                    ResumePaused = true
                }
            };
            PlayerPrefs.SetString(LocalProgress.PreviousSaveKey, JsonUtility.ToJson(previous));
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.Version, Is.EqualTo(ProgressData.CurrentVersion));
            Assert.That(restored.PendingRun, Is.Not.Null);
            Assert.That(restored.PendingRun.LevelIndex, Is.EqualTo(1));
            Assert.That(restored.PendingRun.CaptainHealth, Is.EqualTo(82f));
            Assert.That(restored.PendingRun.ResumePaused, Is.True);
            Assert.That(restored.PendingRun.IsValid(), Is.True);
        }

        [Test]
        public void PausedRunRoundTripPreservesCardPayoffCounters()
        {
            PausedRunData original = new PausedRunData
            {
                LevelIndex = 1,
                CaptainHealth = 100f,
                SoldierCount = 2,
                SelectedWeapon = 0,
                SelectedSuit = 0,
                RecruitPickups = 2,
                WeaponPickups = 1,
                CardChoices = 3,
                LastCardActivationSource = CardActivationSource.Touch,
                LastCardSelectionReport = "NODNARB_CARD pair=7 kind=Recruit source=Touch"
            };

            PausedRunData restored = JsonUtility.FromJson<PausedRunData>(JsonUtility.ToJson(original));

            Assert.That(restored.IsValid(), Is.True);
            Assert.That(restored.RecruitPickups, Is.EqualTo(2));
            Assert.That(restored.WeaponPickups, Is.EqualTo(1));
            Assert.That(restored.CardChoices, Is.EqualTo(3));
            Assert.That(restored.LastCardActivationSource, Is.EqualTo(CardActivationSource.Touch));
            Assert.That(restored.LastCardSelectionReport, Does.Contain("source=Touch"));
        }

        [Test]
        public void LockedLoadoutSelectionFallsBackDuringSanitize()
        {
            ProgressData data = new ProgressData { SelectedWeapon = 2, SelectedSuit = 2 };
            data.Sanitize();
            Assert.That(data.SelectedWeapon, Is.Zero);
            Assert.That(data.SelectedSuit, Is.Zero);
        }

        [Test]
        public void LoadoutProgressionIsEarnedByCompletedSectors()
        {
            Assert.That(LoadoutCatalog.IsWeaponUnlocked(0, 0), Is.True);
            Assert.That(LoadoutCatalog.IsWeaponUnlocked(1, 2), Is.False);
            Assert.That(LoadoutCatalog.IsWeaponUnlocked(1, 3), Is.True);
            Assert.That(LoadoutCatalog.IsWeaponUnlocked(2, 5), Is.False);
            Assert.That(LoadoutCatalog.IsWeaponUnlocked(2, 6), Is.True);
            Assert.That(LoadoutCatalog.IsSuitUnlocked(1, 3), Is.False);
            Assert.That(LoadoutCatalog.IsSuitUnlocked(1, 4), Is.True);
            Assert.That(LoadoutCatalog.IsSuitUnlocked(2, 7), Is.False);
            Assert.That(LoadoutCatalog.IsSuitUnlocked(2, 8), Is.True);
        }

        [Test]
        public void LocalFeedbackSettingsDefaultOnAndPersistOffState()
        {
            PlayerPrefs.DeleteKey(NodnarbSettings.SoundKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.MusicKey);
            PlayerPrefs.DeleteKey(NodnarbSettings.HapticsKey);

            Assert.That(NodnarbSettings.SoundEnabled, Is.True);
            Assert.That(NodnarbSettings.MusicEnabled, Is.True);
            Assert.That(NodnarbSettings.HapticsEnabled, Is.True);
            Assert.That(NodnarbSettings.CaptionsEnabled, Is.True);
            Assert.That(NodnarbSettings.HighContrastEnabled, Is.False);
            Assert.That(NodnarbSettings.ReducedMotionEnabled, Is.False);

            NodnarbSettings.SoundEnabled = false;
            NodnarbSettings.MusicEnabled = false;
            NodnarbSettings.HapticsEnabled = false;
            NodnarbSettings.CaptionsEnabled = false;
            NodnarbSettings.HighContrastEnabled = false;
            NodnarbSettings.ReducedMotionEnabled = true;

            Assert.That(NodnarbSettings.SoundEnabled, Is.False);
            Assert.That(NodnarbSettings.MusicEnabled, Is.False);
            Assert.That(NodnarbSettings.HapticsEnabled, Is.False);
            Assert.That(NodnarbSettings.CaptionsEnabled, Is.False);
            Assert.That(NodnarbSettings.HighContrastEnabled, Is.False);
            Assert.That(NodnarbSettings.ReducedMotionEnabled, Is.True);
            Assert.That(NodnarbSettings.Indicator("SFX", false), Is.EqualTo("SFX // OFF"));
        }

        [Test]
        public void CorruptLocalSaveFallsBackToDefaults()
        {
            PlayerPrefs.SetString(LocalProgress.SaveKey, "{ definitely-not-json");

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.UnlockedLevel, Is.EqualTo(1));
            Assert.That(restored.Credits, Is.Zero);
            Assert.That(restored.BestScores, Has.Length.EqualTo(10));
        }

        [Test]
        public void CorruptPrimarySaveRecoversFromBackup()
        {
            ProgressData saved = new ProgressData();
            saved.RecordCampaignResult(3, 777, 21, true);
            LocalProgress.Save(saved);
            PlayerPrefs.SetString(LocalProgress.SaveKey, "{ definitely-not-json");
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.IsLevelCompleted(3), Is.True);
            Assert.That(restored.BestScores[2], Is.EqualTo(777));
            Assert.That(restored.Credits, Is.EqualTo(21));
        }

        [Test]
        public void IncompletePrimarySaveRecoversFromBackup()
        {
            ProgressData saved = new ProgressData();
            saved.RecordCampaignResult(4, 888, 34, true);
            LocalProgress.Save(saved);
            PlayerPrefs.SetString(LocalProgress.SaveKey, "{}");
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.IsLevelCompleted(4), Is.True);
            Assert.That(restored.BestScores[3], Is.EqualTo(888));
            Assert.That(restored.Credits, Is.EqualTo(34));
        }

        [Test]
        public void CorruptBackupIsRepairedWhenPrimaryIsValid()
        {
            ProgressData saved = new ProgressData();
            saved.RecordCampaignResult(2, 444, 19, true);
            LocalProgress.Save(saved);
            PlayerPrefs.SetString(LocalProgress.BackupSaveKey, "{ definitely-not-json");
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.IsLevelCompleted(2), Is.True);
            Assert.That(PlayerPrefs.GetString(LocalProgress.BackupSaveKey), Is.EqualTo(PlayerPrefs.GetString(LocalProgress.SaveKey)));
            ProgressData repairedBackup = JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(LocalProgress.BackupSaveKey));
            Assert.That(repairedBackup.IsValidSerialized(true), Is.True);
        }

        [Test]
        public void InvalidPendingRunIsRemovedDuringSanitize()
        {
            ProgressData data = new ProgressData
            {
                PendingRun = new PausedRunData
                {
                    LevelIndex = 99,
                    CaptainHealth = 100f,
                    SoldierCount = 2,
                    SelectedWeapon = 0,
                    SelectedSuit = 0
                }
            };

            data.Sanitize();

            Assert.That(data.PendingRun, Is.Null);
            Assert.That(data.Version, Is.EqualTo(ProgressData.CurrentVersion));
        }

        [Test]
        public void PausedRunRejectsOverBudgetEnemyComposition()
        {
            PausedRunData snapshot = new PausedRunData
            {
                LevelIndex = 1,
                CaptainHealth = 100f,
                SoldierCount = 2,
                SelectedWeapon = 0,
                SelectedSuit = 0,
                Enemies = new PausedEnemyData[25]
            };

            for (int index = 0; index < snapshot.Enemies.Length; index++)
            {
                snapshot.Enemies[index] = new PausedEnemyData
                {
                    Kind = EnemyKind.Melee,
                    BossBehavior = BossBehavior.Crusher,
                    HealthRatio = 1f
                };
            }

            Assert.That(snapshot.IsValid(), Is.False);

            snapshot.Enemies = new PausedEnemyData[2]
            {
                new PausedEnemyData { Kind = EnemyKind.Boss, BossBehavior = BossBehavior.Crusher, HealthRatio = 1f },
                new PausedEnemyData { Kind = EnemyKind.Boss, BossBehavior = BossBehavior.Crusher, HealthRatio = 1f }
            };

            Assert.That(snapshot.IsValid(), Is.False);
        }

        [Test]
        public void PausedRunRejectsMalformedCardPair()
        {
            PausedRunData snapshot = new PausedRunData
            {
                LevelIndex = 1,
                CaptainHealth = 100f,
                SoldierCount = 2,
                SelectedWeapon = 0,
                SelectedSuit = 0,
                Cards = new PausedCardData[2]
                {
                    new PausedCardData { PairId = 7, Kind = CardKind.Weapon, HealthRatio = 1f },
                    new PausedCardData { PairId = 7, Kind = CardKind.Weapon, HealthRatio = 1f }
                }
            };

            Assert.That(snapshot.IsValid(), Is.False);

            snapshot.Cards[1].Kind = CardKind.Recruit;
            Assert.That(snapshot.IsValid(), Is.True);

            snapshot.Cards[1].PairId = 8;
            Assert.That(snapshot.IsValid(), Is.False);
        }

        [Test]
        public void LoadDropsInvalidPendingRunWithoutLosingProgress()
        {
            ProgressData saved = new ProgressData();
            saved.RecordCampaignResult(5, 777, 21, true);
            saved.PendingRun = new PausedRunData
            {
                LevelIndex = 5,
                CaptainHealth = 100f,
                SoldierCount = 2,
                SelectedWeapon = 0,
                SelectedSuit = 0,
                Enemies = new PausedEnemyData[25]
            };
            for (int index = 0; index < saved.PendingRun.Enemies.Length; index++)
            {
                saved.PendingRun.Enemies[index] = new PausedEnemyData
                {
                    Kind = EnemyKind.Melee,
                    BossBehavior = BossBehavior.Crusher,
                    HealthRatio = 1f
                };
            }

            string json = JsonUtility.ToJson(saved);
            PlayerPrefs.SetString(LocalProgress.SaveKey, json);
            PlayerPrefs.SetString(LocalProgress.BackupSaveKey, json);
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.PendingRun, Is.Null);
            Assert.That(restored.IsLevelCompleted(5), Is.True);
            Assert.That(restored.UnlockedLevel, Is.EqualTo(6));
            Assert.That(restored.BestScores[4], Is.EqualTo(777));
            Assert.That(restored.Credits, Is.EqualTo(21));
            ProgressData persisted = JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(LocalProgress.SaveKey));
            Assert.That(persisted.PendingRun == null || persisted.PendingRun.IsEmptySerializedNull(), Is.True);
        }

        [Test]
        public void SerializedEmptyPendingRunIsTreatedAsAbsent()
        {
            string placeholder = JsonUtility.ToJson(new ProgressData
            {
                PendingRun = new PausedRunData()
            });
            PlayerPrefs.SetString(LocalProgress.SaveKey, placeholder);
            PlayerPrefs.SetString(LocalProgress.BackupSaveKey, placeholder);
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.PendingRun, Is.Null);
        }

        [Test]
        public void CurrentVersionSanitizeIsRewrittenToBothCopies()
        {
            ProgressData stale = new ProgressData
            {
                UnlockedLevel = 1,
                CompletedLevelMask = 1 << 4,
                SelectedWeapon = 2
            };
            string staleJson = JsonUtility.ToJson(stale);
            PlayerPrefs.SetString(LocalProgress.SaveKey, staleJson);
            PlayerPrefs.SetString(LocalProgress.BackupSaveKey, staleJson);
            PlayerPrefs.Save();

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.UnlockedLevel, Is.EqualTo(6));
            Assert.That(restored.SelectedWeapon, Is.EqualTo(0));
            string primary = PlayerPrefs.GetString(LocalProgress.SaveKey);
            string backup = PlayerPrefs.GetString(LocalProgress.BackupSaveKey);
            Assert.That(primary, Is.EqualTo(backup));
            ProgressData persisted = JsonUtility.FromJson<ProgressData>(primary);
            Assert.That(persisted.UnlockedLevel, Is.EqualTo(6));
            Assert.That(persisted.SelectedWeapon, Is.EqualTo(0));
        }

        [Test]
        public void ValidPendingRunSurvivesJsonRoundTrip()
        {
            ProgressData original = new ProgressData
            {
                PendingRun = new PausedRunData
                {
                    LevelIndex = 4,
                    CaptainHealth = 73f,
                    SoldierCount = 5,
                    SelectedWeapon = 0,
                    SelectedSuit = 0,
                    Elapsed = 22f,
                    Score = 120,
                    Kills = 4,
                    Salvage = 8
                }
            };

            ProgressData restored = JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(original));
            restored.Sanitize();

            Assert.That(restored.PendingRun, Is.Not.Null);
            Assert.That(restored.PendingRun.LevelIndex, Is.EqualTo(4));
            Assert.That(restored.PendingRun.CaptainHealth, Is.EqualTo(73f));
        }
    }
}
