using NUnit.Framework;

namespace EscapeFromNodnarb.Tests
{
    public sealed class RunModelTests
    {
        [Test]
        public void RecruitCapsVisibleSquadAndConvertsExtrasToOvercharge()
        {
            RunModel model = new RunModel(2, 0);
            for (int i = 0; i < 13; i++)
            {
                model.Recruit();
            }

            Assert.That(model.SoldierCount, Is.EqualTo(RunModel.VisibleSoldierCap));
            Assert.That(model.OverflowRecruits, Is.EqualTo(3));
            Assert.That(model.VisibleShooterCount, Is.EqualTo(13));
        }

        [Test]
        public void WeaponAndOverflowIncreaseDamageAndRate()
        {
            RunModel model = new RunModel(12, 0);
            float baseDamage = model.ShotDamage;
            float baseInterval = model.FireInterval;
            float baseShotsPerSecond = model.ShotsPerSecond;
            model.UpgradeWeapon();
            model.Recruit();

            Assert.That(model.ShotDamage, Is.GreaterThan(baseDamage));
            Assert.That(model.FireInterval, Is.LessThan(baseInterval));
            Assert.That(model.ShotsPerSecond, Is.GreaterThan(baseShotsPerSecond));
        }

        [Test]
        public void RapidFireRunsForFourSecondsAndRequiresCooldown()
        {
            RunModel model = new RunModel(2, 0);
            float baseInterval = model.FireInterval;
            Assert.That(model.ActivateRapidFire(), Is.True);
            Assert.That(model.FireInterval, Is.LessThan(baseInterval));
            model.Tick(RunModel.RapidFireDuration + 0.01f);
            Assert.That(model.RapidFireActive, Is.False);
            Assert.That(model.ActivateRapidFire(), Is.False);
            model.Tick(RunModel.RapidFireCooldown);
            Assert.That(model.ActivateRapidFire(), Is.True);
        }

        [Test]
        public void CaptainDeathAndBreachAreDistinctTerminalStates()
        {
            RunModel captainDown = new RunModel(2, 0);
            captainDown.DamageCaptain(1000f);
            Assert.That(captainDown.EndReason, Is.EqualTo(RunEndReason.CaptainDown));

            RunModel breached = new RunModel(2, 0);
            breached.BreachFrontLine();
            Assert.That(breached.EndReason, Is.EqualTo(RunEndReason.FrontLineBreached));
        }

        [Test]
        public void LowHealthActivatesTheComebackWindowWithoutChangingTerminalRules()
        {
            RunModel model = new RunModel(RunModel.StartingSoldiers, 0);

            Assert.That(model.ComebackActive, Is.False);
            model.DamageCaptain(66f);

            Assert.That(model.CaptainHealth, Is.EqualTo(34f));
            Assert.That(model.ComebackActive, Is.True);
            Assert.That(model.IsEnded, Is.False);
        }

        [Test]
        public void ComebackAssistIsOneShotAndPersistsThroughPauseRestore()
        {
            RunModel original = new RunModel(RunModel.StartingSoldiers, 0);
            original.DamageCaptain(66f);

            Assert.That(original.TryConsumeComebackAssist(), Is.True);
            Assert.That(original.TryConsumeComebackAssist(), Is.False);

            PausedRunData snapshot = original.CreateSnapshot(1, false, 2.5f);
            RunModel restored = new RunModel(RunModel.StartingSoldiers, 0);

            Assert.That(restored.Restore(snapshot), Is.True);
            Assert.That(restored.ComebackAssistUsed, Is.True);
            Assert.That(restored.TryConsumeComebackAssist(), Is.False);
        }

        [Test]
        public void PausedSnapshotRestoresCombatState()
        {
            RunModel original = new RunModel(2, 0);
            original.Recruit();
            original.UpgradeWeapon();
            original.RegisterKill(42, 3);
            original.ActivateRapidFire();
            original.Tick(0.75f);

            PausedRunData snapshot = original.CreateSnapshot(4, false, 19.5f);
            RunModel restored = new RunModel(2, 0);

            Assert.That(restored.Restore(snapshot), Is.True);
            Assert.That(restored.SoldierCount, Is.EqualTo(original.SoldierCount));
            Assert.That(restored.WeaponLevel, Is.EqualTo(original.WeaponLevel));
            Assert.That(restored.Score, Is.EqualTo(original.Score));
            Assert.That(restored.Kills, Is.EqualTo(original.Kills));
            Assert.That(restored.RapidFireRemaining, Is.EqualTo(original.RapidFireRemaining).Within(0.001f));
        }

        [Test]
        public void InvalidPausedSnapshotIsRejected()
        {
            PausedRunData invalid = new PausedRunData
            {
                LevelIndex = 99,
                CaptainHealth = 100f,
                SoldierCount = 2,
                SelectedWeapon = 0,
                SelectedSuit = 0
            };

            Assert.That(invalid.IsValid(), Is.False);
            Assert.That(new RunModel(2, 0).Restore(invalid), Is.False);
        }

        [Test]
        public void PausedSnapshotRejectsUnboundedEconomyValues()
        {
            PausedRunData invalid = new PausedRunData
            {
                LevelIndex = 1,
                CaptainHealth = 100f,
                SoldierCount = 2,
                OverflowRecruits = RunModel.MaxOverflowRecruits + 1,
                WeaponLevel = RunModel.MaxWeaponLevel + 1,
                SelectedWeapon = 0,
                SelectedSuit = 0
            };

            Assert.That(invalid.IsValid(), Is.False);
        }

        [Test]
        public void EconomyCapsRemainStableAfterRepeatedUpgradeInputs()
        {
            RunModel model = new RunModel(RunModel.VisibleSoldierCap, 0);
            for (int index = 0; index < RunModel.MaxOverflowRecruits + 20; index++)
            {
                model.Recruit();
                model.UpgradeWeapon();
            }

            Assert.That(model.OverflowRecruits, Is.EqualTo(RunModel.MaxOverflowRecruits));
            Assert.That(model.WeaponLevel, Is.EqualTo(RunModel.MaxWeaponLevel));
        }

        [Test]
        public void CappedProgressionStaysFiniteAndMonotonic()
        {
            RunModel model = new RunModel(RunModel.VisibleSoldierCap, 0);
            float previousDamage = model.ShotDamage;
            float previousShotsPerSecond = model.ShotsPerSecond;

            for (int index = 0; index < RunModel.MaxWeaponLevel; index++)
            {
                model.UpgradeWeapon();
                Assert.That(model.ShotDamage, Is.GreaterThanOrEqualTo(previousDamage));
                Assert.That(model.ShotsPerSecond, Is.GreaterThanOrEqualTo(previousShotsPerSecond));
                Assert.That(float.IsNaN(model.ShotDamage) || float.IsInfinity(model.ShotDamage), Is.False);
                Assert.That(float.IsNaN(model.ShotsPerSecond) || float.IsInfinity(model.ShotsPerSecond), Is.False);
                previousDamage = model.ShotDamage;
                previousShotsPerSecond = model.ShotsPerSecond;
            }

            Assert.That(model.WeaponLevel, Is.EqualTo(RunModel.MaxWeaponLevel));
            Assert.That(model.OverflowRecruits, Is.Zero);
        }

        [Test]
        public void FailureDoesNotUnlockFutureCampaignProgress()
        {
            ProgressData data = new ProgressData();
            data.RecordCampaignResult(1, 100, 5, false);
            data.RecordCampaignResult(3, 300, 5, false);

            Assert.That(data.UnlockedLevel, Is.EqualTo(1));
            Assert.That(data.CompletedLevelMask, Is.Zero);
            Assert.That(data.CampaignVictories, Is.Zero);
            Assert.That(data.Credits, Is.EqualTo(10));
        }
    }
}
