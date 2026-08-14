using NUnit.Framework;
using UnityEngine;

namespace EscapeFromNodnarb.Tests
{
    public sealed class NodnarbInputPolicyTests
    {
        [Test]
        public void BottomActionRailUsesTheSafeAreaInsteadOfTheRawScreenBottom()
        {
            Rect safeArea = new Rect(0f, 80f, 1080f, 1760f);

            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 100f), safeArea, 1920), Is.True);
            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 320f), safeArea, 1920), Is.False);
        }

        [Test]
        public void UiOwnedAndBottomRailPointersCannotStartWorldGestures()
        {
            Rect safeArea = new Rect(0f, 80f, 1080f, 1760f);

            Assert.That(NodnarbInputPolicy.CanBeginWorldGesture(new Vector2(540f, 900f), safeArea, 1920, true), Is.False);
            Assert.That(NodnarbInputPolicy.CanBeginWorldGesture(new Vector2(540f, 120f), safeArea, 1920, false), Is.False);
            Assert.That(NodnarbInputPolicy.CanBeginWorldGesture(new Vector2(540f, 900f), safeArea, 1920, false), Is.True);
        }

        [Test]
        public void ZeroSafeAreaFallsBackToScreenHeightWithoutOpeningTheWholeRail()
        {
            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 100f), Rect.zero, 1920), Is.True);
            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 400f), Rect.zero, 1920), Is.False);
        }

        [Test]
        public void SafeAreaNormalizationClampsInvalidInsets()
        {
            Rect normalized = UiFactory.NormalizedSafeArea(new Rect(-40f, 80f, 1200f, 2000f), 1080, 1920);

            Assert.That(normalized.xMin, Is.EqualTo(0f));
            Assert.That(normalized.xMax, Is.EqualTo(1f));
            Assert.That(normalized.yMin, Is.EqualTo(80f / 1920f).Within(0.0001f));
            Assert.That(normalized.yMax, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GestureOwnershipUsesTheClampedSafeArea()
        {
            Rect invalidSafeArea = new Rect(-40f, -200f, 1200f, 2200f);

            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 100f), invalidSafeArea, 1920), Is.True);
            Assert.That(NodnarbInputPolicy.IsInBottomActionRail(new Vector2(540f, 300f), invalidSafeArea, 1920), Is.False);
            Assert.That(NodnarbInputPolicy.CanBeginWorldGesture(new Vector2(540f, 100f), invalidSafeArea, 1920, false), Is.False);
            Assert.That(NodnarbInputPolicy.CanBeginWorldGesture(new Vector2(540f, 300f), invalidSafeArea, 1920, false), Is.True);
        }
    }
}
