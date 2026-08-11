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
    }
}
