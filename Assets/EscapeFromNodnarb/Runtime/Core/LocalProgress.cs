using System;
using UnityEngine;

namespace EscapeFromNodnarb
{
    [Serializable]
    public sealed class ProgressData
    {
        public int Version = 1;
        public int UnlockedLevel = 1;
        public int CompletedLevelMask;
        public int Credits;
        public int SelectedWeapon;
        public int SelectedSuit;
        public int EndlessBest;
        public int[] BestScores = new int[10];

        public void Sanitize()
        {
            Version = 1;
            UnlockedLevel = Math.Max(1, Math.Min(10, UnlockedLevel));
            Credits = Math.Max(0, Credits);
            SelectedWeapon = LoadoutCatalog.ClampWeapon(SelectedWeapon);
            SelectedSuit = LoadoutCatalog.ClampSuit(SelectedSuit);
            EndlessBest = Math.Max(0, EndlessBest);
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
            }

            Sanitize();
        }

        public void RecordEndlessResult(int score, int salvage)
        {
            EndlessBest = Math.Max(EndlessBest, Math.Max(0, score));
            Credits += Math.Max(0, salvage);
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
        public const string SaveKey = "EscapeFromNodnarb.Save.v1";

        public static ProgressData Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            ProgressData data;
            try
            {
                data = string.IsNullOrWhiteSpace(json) ? new ProgressData() : JsonUtility.FromJson<ProgressData>(json);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Escape from Nodnarb ignored an unreadable local save and loaded defaults.");
                data = new ProgressData();
            }

            if (data == null)
            {
                data = new ProgressData();
            }

            data.Sanitize();
            return data;
        }

        public static void Save(ProgressData data)
        {
            if (data == null)
            {
                return;
            }

            data.Sanitize();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
