using NUnit.Framework;
using UnityEngine;

namespace EscapeFromNodnarb.Tests
{
    public sealed class LocalProgressTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(LocalProgress.SaveKey);
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
        public void JsonRoundTripPreservesProgress()
        {
            ProgressData original = new ProgressData();
            original.RecordCampaignResult(3, 777, 21, true);
            string json = JsonUtility.ToJson(original);
            ProgressData restored = JsonUtility.FromJson<ProgressData>(json);
            restored.Sanitize();

            Assert.That(restored.IsLevelCompleted(3), Is.True);
            Assert.That(restored.BestScores[2], Is.EqualTo(777));
            Assert.That(restored.Credits, Is.EqualTo(21));
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
        public void CorruptLocalSaveFallsBackToDefaults()
        {
            PlayerPrefs.SetString(LocalProgress.SaveKey, "{ definitely-not-json");

            ProgressData restored = LocalProgress.Load();

            Assert.That(restored.UnlockedLevel, Is.EqualTo(1));
            Assert.That(restored.Credits, Is.Zero);
            Assert.That(restored.BestScores, Has.Length.EqualTo(10));
        }
    }
}
