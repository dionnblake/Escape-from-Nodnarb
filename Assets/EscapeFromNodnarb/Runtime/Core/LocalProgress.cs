using System;
using UnityEngine;

namespace EscapeFromNodnarb
{
    [Serializable]
    public sealed class PausedEnemyData
    {
        public EnemyKind Kind;
        public BossBehavior BossBehavior;
        public float LaneX;
        public float PositionX;
        public float PositionZ;
        public float HealthRatio;
        public float Health;
        public float MaxHealth;
        public float Speed;
        public float RangedTimer;
        public float BossTelegraphTimer;
        public bool BossTelegraphArmed;
        public bool ExactState;

        public bool IsValid()
        {
            return Kind >= EnemyKind.Melee
                && Kind <= EnemyKind.Boss
                && BossBehavior >= BossBehavior.Crusher
                && BossBehavior <= BossBehavior.Carrier
                && !float.IsNaN(LaneX)
                && !float.IsInfinity(LaneX)
                && !float.IsNaN(PositionX)
                && !float.IsInfinity(PositionX)
                && !float.IsNaN(PositionZ)
                && !float.IsInfinity(PositionZ)
                && HealthRatio > 0f
                && HealthRatio <= 1f
                && RangedTimer >= 0f
                && (!ExactState || (Health > 0f && !float.IsNaN(Health) && !float.IsInfinity(Health)
                    && MaxHealth >= Health && !float.IsNaN(MaxHealth) && !float.IsInfinity(MaxHealth)
                    && Speed > 0f && !float.IsNaN(Speed) && !float.IsInfinity(Speed)
                    && BossTelegraphTimer >= 0f));
        }
    }

    [Serializable]
    public sealed class PausedCardData
    {
        public int PairId;
        public CardKind Kind;
        public float PositionX;
        public float PositionZ;
        public float HealthRatio;
        public float Health;
        public float MaxHealth;
        public float Speed;
        public bool ExactState;

        public bool IsValid()
        {
            return PairId > 0
                && (Kind == CardKind.Weapon || Kind == CardKind.Recruit)
                && !float.IsNaN(PositionX)
                && !float.IsInfinity(PositionX)
                && !float.IsNaN(PositionZ)
                && !float.IsInfinity(PositionZ)
                && HealthRatio > 0f
                && HealthRatio <= 1f
                && (!ExactState || (Health > 0f && !float.IsNaN(Health) && !float.IsInfinity(Health)
                    && MaxHealth >= Health && !float.IsNaN(MaxHealth) && !float.IsInfinity(MaxHealth)
                    && Speed > 0f && !float.IsNaN(Speed) && !float.IsInfinity(Speed)));
        }
    }

    [Serializable]
    public sealed class PausedRunData
    {
        public int LevelIndex;
        public bool Endless;
        // Background saves must reopen behind the pause screen so a lifecycle
        // transition never resumes combat without an explicit player action.
        public bool ResumePaused;
        public float Elapsed;
        public float CaptainHealth;
        public float SquadRelativeX;
        public float SquadTargetX;
        public int SoldierCount;
        public int OverflowRecruits;
        public int WeaponLevel;
        public int SelectedWeapon;
        public int SelectedSuit;
        public int Score;
        public int Kills;
        public int Salvage;
        public int RecruitPickups;
        public int WeaponPickups;
        public int CardChoices;
        public CardActivationSource LastCardActivationSource;
        public string LastCardSelectionReport;
        public float RapidFireRemaining;
        public float RapidFireCooldownRemaining;
        public bool ComebackAssistUsed;
        public int LevelSeed;
        public int RandomSeed;
        public int RandomDrawCount;
        public int PairSequence;
        public int WaveSequence;
        public int EndlessBossCycle;
        public float SpawnTimer;
        public float CardTimer;
        public float FireTimer;
        public float HitAudioTimer;
        public bool BossSpawned;
        public PausedEnemyData[] Enemies;
        public PausedCardData[] Cards;

        public bool IsValid()
        {
            bool validLevel = Endless ? LevelIndex == 0 : LevelIndex >= 1 && LevelIndex <= 10;
            return validLevel
                && Elapsed >= 0f
                && CaptainHealth > 0f
                && CaptainHealth <= RunModel.MaxCaptainHealth
                && SquadRelativeX >= -(GameTheme.ArenaHalfWidth - 0.35f)
                && SquadRelativeX <= GameTheme.ArenaHalfWidth - 0.35f
                && SquadTargetX >= -(GameTheme.ArenaHalfWidth - 0.35f)
                && SquadTargetX <= GameTheme.ArenaHalfWidth - 0.35f
                && SoldierCount >= 0
                && SoldierCount <= RunModel.VisibleSoldierCap
                && OverflowRecruits >= 0
                && OverflowRecruits <= RunModel.MaxOverflowRecruits
                && WeaponLevel >= 0
                && WeaponLevel <= RunModel.MaxWeaponLevel
                && SelectedWeapon >= 0
                && SelectedWeapon < LoadoutCatalog.Weapons.Length
                && SelectedSuit >= 0
                && SelectedSuit < LoadoutCatalog.Suits.Length
                && Score >= 0
                && Kills >= 0
                && Salvage >= 0
                && RecruitPickups >= 0
                && WeaponPickups >= 0
                && CardChoices >= 0
                && (LastCardActivationSource == CardActivationSource.AutoFire
                    || LastCardActivationSource == CardActivationSource.Touch)
                && RapidFireRemaining >= 0f
                && RapidFireCooldownRemaining >= 0f
                && RandomDrawCount >= 0
                && PairSequence >= 0
                && WaveSequence >= 0
                && EndlessBossCycle >= 0
                && SpawnTimer >= 0f
                && CardTimer >= 0f
                && FireTimer >= 0f
                && HitAudioTimer >= 0f
                && (Enemies == null || Enemies.Length <= 25)
                && (Cards == null || Cards.Length <= 2)
                && AreEnemiesCompositionValid()
                && AreEnemiesValid()
                && AreCardsCompositionValid()
                && AreCardsValid();
        }

        public bool IsEmptySerializedNull()
        {
            return LevelIndex == 0
                && !Endless
                && Elapsed == 0f
                && CaptainHealth == 0f
                && SquadRelativeX == 0f
                && SquadTargetX == 0f
                && SoldierCount == 0
                && OverflowRecruits == 0
                && WeaponLevel == 0
                && SelectedWeapon == 0
                && SelectedSuit == 0
                && Score == 0
                && Kills == 0
                && Salvage == 0
                && RecruitPickups == 0
                && WeaponPickups == 0
                && CardChoices == 0
                && LastCardActivationSource == CardActivationSource.AutoFire
                && string.IsNullOrEmpty(LastCardSelectionReport)
                && RapidFireRemaining == 0f
                && RapidFireCooldownRemaining == 0f
                && LevelSeed == 0
                && RandomSeed == 0
                && RandomDrawCount == 0
                && PairSequence == 0
                && WaveSequence == 0
                && EndlessBossCycle == 0
                && SpawnTimer == 0f
                && CardTimer == 0f
                && FireTimer == 0f
                && HitAudioTimer == 0f
                && !BossSpawned
                && (Enemies == null || Enemies.Length == 0)
                && (Cards == null || Cards.Length == 0);
        }

        private bool AreEnemiesValid()
        {
            if (Enemies == null)
            {
                return true;
            }

            for (int index = 0; index < Enemies.Length; index++)
            {
                if (Enemies[index] == null || !Enemies[index].IsValid())
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreEnemiesCompositionValid()
        {
            if (Enemies == null)
            {
                return true;
            }

            int bossCount = 0;
            int normalCount = 0;
            for (int index = 0; index < Enemies.Length; index++)
            {
                if (Enemies[index] != null && Enemies[index].Kind == EnemyKind.Boss)
                {
                    bossCount++;
                }
                else
                {
                    normalCount++;
                }
            }

            return normalCount <= 24 && bossCount <= 1;
        }

        private bool AreCardsValid()
        {
            if (Cards == null)
            {
                return true;
            }

            for (int index = 0; index < Cards.Length; index++)
            {
                if (Cards[index] == null || !Cards[index].IsValid())
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreCardsCompositionValid()
        {
            if (Cards == null || Cards.Length < 2)
            {
                return true;
            }

            return Cards[0] != null
                && Cards[1] != null
                && Cards[0].PairId == Cards[1].PairId
                && Cards[0].Kind != Cards[1].Kind;
        }
    }

    [Serializable]
    public sealed class ProgressData
    {
        public const int CurrentVersion = 3;
        public int Version = CurrentVersion;
        public int UnlockedLevel = 1;
        public int CompletedLevelMask;
        public int Credits;
        public int SelectedWeapon;
        public int SelectedSuit;
        public int EndlessBest;
        public bool OnboardingComplete;
        public int CampaignRuns;
        public int EndlessRuns;
        public int ReplayRuns;
        public int CampaignVictories;
        public int[] BestScores = new int[10];
        public PausedRunData PendingRun;

        public bool IsValidSerialized(bool requireCurrentVersion)
        {
            return IsValidSerializedCore(requireCurrentVersion, true);
        }

        internal bool IsValidSerializedForLoad(bool requireCurrentVersion)
        {
            return IsValidSerializedCore(requireCurrentVersion, false);
        }

        private bool IsValidSerializedCore(bool requireCurrentVersion, bool validatePendingRun)
        {
            return (requireCurrentVersion ? Version == CurrentVersion : Version >= 1 && Version <= CurrentVersion)
                && UnlockedLevel >= 1
                && UnlockedLevel <= 10
                && CompletedLevelMask >= 0
                && (CompletedLevelMask & ~((1 << 10) - 1)) == 0
                && Credits >= 0
                && SelectedWeapon >= 0
                && SelectedWeapon < LoadoutCatalog.Weapons.Length
                && SelectedSuit >= 0
                && SelectedSuit < LoadoutCatalog.Suits.Length
                && EndlessBest >= 0
                && CampaignRuns >= 0
                && EndlessRuns >= 0
                && ReplayRuns >= 0
                && CampaignVictories >= 0
                && BestScores != null
                && BestScores.Length == 10
                && AreBestScoresValid()
                && (!validatePendingRun || PendingRun == null || PendingRun.IsEmptySerializedNull() || PendingRun.IsValid());
        }

        private bool AreBestScoresValid()
        {
            for (int index = 0; index < BestScores.Length; index++)
            {
                if (BestScores[index] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Sanitize()
        {
            string before = JsonUtility.ToJson(this);
            Version = CurrentVersion;
            UnlockedLevel = Math.Max(1, Math.Min(10, UnlockedLevel));
            Credits = Math.Max(0, Credits);
            SelectedWeapon = LoadoutCatalog.ClampWeapon(SelectedWeapon);
            SelectedSuit = LoadoutCatalog.ClampSuit(SelectedSuit);
            EndlessBest = Math.Max(0, EndlessBest);
            CampaignRuns = Math.Max(0, CampaignRuns);
            EndlessRuns = Math.Max(0, EndlessRuns);
            ReplayRuns = Math.Max(0, ReplayRuns);
            CampaignVictories = Math.Max(0, CampaignVictories);
            if (BestScores == null || BestScores.Length != 10)
            {
                int[] repaired = new int[10];
                if (BestScores != null)
                {
                    Array.Copy(BestScores, repaired, Math.Min(BestScores.Length, repaired.Length));
                }

                BestScores = repaired;
            }

            int highestCompleted = HighestCompletedLevel();
            UnlockedLevel = Math.Max(UnlockedLevel, Math.Min(10, highestCompleted + 1));
            if (!LoadoutCatalog.IsWeaponUnlocked(SelectedWeapon, highestCompleted))
            {
                SelectedWeapon = 0;
            }

            if (!LoadoutCatalog.IsSuitUnlocked(SelectedSuit, highestCompleted))
            {
                SelectedSuit = 0;
            }

            if (PendingRun != null && PendingRun.IsEmptySerializedNull())
            {
                PendingRun = null;
            }
            else if (PendingRun != null && !PendingRun.IsValid())
            {
                PendingRun = null;
            }

            return !string.Equals(before, JsonUtility.ToJson(this), StringComparison.Ordinal);
        }

        public bool IsLevelCompleted(int level)
        {
            if (level < 1 || level > 10)
            {
                return false;
            }

            return (CompletedLevelMask & (1 << (level - 1))) != 0;
        }

        public int HighestCompletedLevel()
        {
            for (int level = 10; level >= 1; level--)
            {
                if (IsLevelCompleted(level))
                {
                    return level;
                }
            }

            return 0;
        }

        public void RecordCampaignResult(int level, int score, int salvage, bool victory)
        {
            if (level < 1 || level > 10)
            {
                return;
            }

            BestScores[level - 1] = Math.Max(BestScores[level - 1], Math.Max(0, score));
            Credits += Math.Max(0, salvage);
            if (victory)
            {
                CompletedLevelMask |= 1 << (level - 1);
                UnlockedLevel = Math.Max(UnlockedLevel, Math.Min(10, level + 1));
                CampaignVictories++;
            }

            Sanitize();
        }

        public void RecordEndlessResult(int score, int salvage)
        {
            EndlessBest = Math.Max(EndlessBest, Math.Max(0, score));
            Credits += Math.Max(0, salvage);
            Sanitize();
        }

        public void RecordRunStart(bool isEndless, bool replay)
        {
            if (isEndless)
            {
                EndlessRuns++;
            }
            else
            {
                CampaignRuns++;
            }

            if (replay)
            {
                ReplayRuns++;
            }

            Sanitize();
        }
    }

    [Serializable]
    public sealed class WeaponDefinition
    {
        public string Name;
        public string Description;
        public int UnlockAfterLevel;
        public float BaseDamage;
        public float FireRateMultiplier;

        public WeaponDefinition(string name, string description, int unlockAfterLevel, float baseDamage, float fireRateMultiplier)
        {
            Name = name;
            Description = description;
            UnlockAfterLevel = unlockAfterLevel;
            BaseDamage = baseDamage;
            FireRateMultiplier = fireRateMultiplier;
        }
    }

    [Serializable]
    public sealed class SuitDefinition
    {
        public string Name;
        public int UnlockAfterLevel;

        public SuitDefinition(string name, int unlockAfterLevel)
        {
            Name = name;
            UnlockAfterLevel = unlockAfterLevel;
        }
    }

    public static class LoadoutCatalog
    {
        public static readonly WeaponDefinition[] Weapons =
        {
            new WeaponDefinition("Pulse Carbine", "Balanced salvage rifle", 0, 1f, 1f),
            new WeaponDefinition("Breaker Rail", "Hard hits, slower cycle", 3, 1.25f, 0.82f),
            new WeaponDefinition("Arc Repeater", "Fast cycle, lighter bolts", 6, 0.82f, 1.32f)
        };

        public static readonly SuitDefinition[] Suits =
        {
            new SuitDefinition("Impact Orange", 0),
            new SuitDefinition("Rescue Signal", 4),
            new SuitDefinition("Void White", 8)
        };

        public static int ClampWeapon(int index)
        {
            return Math.Max(0, Math.Min(Weapons.Length - 1, index));
        }

        public static int ClampSuit(int index)
        {
            return Math.Max(0, Math.Min(Suits.Length - 1, index));
        }

        public static bool IsWeaponUnlocked(int index, int highestCompletedLevel)
        {
            index = ClampWeapon(index);
            return highestCompletedLevel >= Weapons[index].UnlockAfterLevel;
        }

        public static bool IsSuitUnlocked(int index, int highestCompletedLevel)
        {
            index = ClampSuit(index);
            return highestCompletedLevel >= Suits[index].UnlockAfterLevel;
        }
    }

    public static class LocalProgress
    {
        public const string SaveKey = "EscapeFromNodnarb.Save.v3";
        public const string BackupSaveKey = "EscapeFromNodnarb.Save.v3.Backup";
        public const string PreviousSaveKey = "EscapeFromNodnarb.Save.v2";
        public const string PreviousBackupSaveKey = "EscapeFromNodnarb.Save.v2.Backup";
        private const string LegacySaveKey = "EscapeFromNodnarb.Save.v1";

        public static ProgressData Load()
        {
            ProgressData data;
            ProgressData backupData;
            bool recovered = false;
            bool migrated = false;
            bool backupValid = TryRead(BackupSaveKey, out backupData, true);
            if (!TryRead(SaveKey, out data, true))
            {
                recovered = backupValid;
                if (recovered)
                {
                    data = backupData;
                }
                if (!recovered)
                {
                    migrated = TryRead(PreviousSaveKey, out data, false);
                    if (!migrated)
                    {
                        migrated = TryRead(PreviousBackupSaveKey, out data, false);
                    }
                    if (!migrated)
                    {
                        migrated = TryRead(LegacySaveKey, out data, false);
                    }
                }
            }

            if (data == null)
            {
                data = new ProgressData();
            }

            bool sanitized = data.Sanitize();
            if (migrated || recovered || sanitized || !backupValid)
            {
                Save(data);
            }

            Debug.Log("NODNARB_SAVE version=" + data.Version
                + " migrated=" + migrated
                + " recovered_backup=" + recovered
                + " pending_run=" + (data.PendingRun != null));

            return data;
        }

        private static bool TryRead(string key, out ProgressData data, bool requireCurrentVersion)
        {
            data = null;
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(json) || !LooksComplete(json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<ProgressData>(json);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Escape from Nodnarb ignored an unreadable local save in " + key + ".");
                return false;
            }

            if (data == null)
            {
                Debug.LogWarning("Escape from Nodnarb ignored an empty local save in " + key + ".");
                return false;
            }

            return data.IsValidSerializedForLoad(requireCurrentVersion);
        }

        private static bool LooksComplete(string json)
        {
            return json.IndexOf("\"Version\"", StringComparison.Ordinal) >= 0
                && json.IndexOf("\"UnlockedLevel\"", StringComparison.Ordinal) >= 0
                && json.IndexOf("\"BestScores\"", StringComparison.Ordinal) >= 0;
        }

        public static void Save(ProgressData data)
        {
            if (data == null)
            {
                return;
            }

            data.Sanitize();
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(BackupSaveKey, json);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
