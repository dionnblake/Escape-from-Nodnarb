using NUnit.Framework;

namespace EscapeFromNodnarb.Tests
{
    public sealed class CardChoiceTests
    {
        [Test]
        public void LeftPositionFavorsWeaponCard()
        {
            Assert.That(CardChoice.TargetBias(CardKind.Weapon, -2.6f), Is.LessThan(CardChoice.TargetBias(CardKind.Recruit, -2.6f)));
            Assert.That(CardChoice.IsSideAligned(CardKind.Weapon, -2.6f), Is.True);
            Assert.That(CardChoice.IsSideAligned(CardKind.Recruit, -2.6f), Is.False);
        }

        [Test]
        public void RightPositionFavorsRecruitCard()
        {
            Assert.That(CardChoice.TargetBias(CardKind.Recruit, 2.6f), Is.LessThan(CardChoice.TargetBias(CardKind.Weapon, 2.6f)));
            Assert.That(CardChoice.IsSideAligned(CardKind.Recruit, 2.6f), Is.True);
            Assert.That(CardChoice.IsSideAligned(CardKind.Weapon, 2.6f), Is.False);
        }

        [Test]
        public void CenterPositionKeepsBothCardsFair()
        {
            Assert.That(CardChoice.TargetBias(CardKind.Weapon, 0f), Is.EqualTo(0f));
            Assert.That(CardChoice.TargetBias(CardKind.Recruit, 0f), Is.EqualTo(0f));
            Assert.That(CardChoice.Hint(0f), Does.Contain("CHOOSE ONE"));
        }
    }
}
