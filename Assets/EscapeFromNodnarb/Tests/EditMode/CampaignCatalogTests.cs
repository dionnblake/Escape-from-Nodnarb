using System.Linq;
using NUnit.Framework;

namespace EscapeFromNodnarb.Tests
{
    public sealed class CampaignCatalogTests
    {
        [Test]
        public void CampaignHasTenOrderedAuthoredStages()
        {
            Assert.That(CampaignCatalog.All.Count, Is.EqualTo(10));
            for (int index = 0; index < CampaignCatalog.All.Count; index++)
            {
                LevelDefinition level = CampaignCatalog.All[index];
                Assert.That(level.Index, Is.EqualTo(index + 1));
                Assert.That(level.DurationSeconds, Is.InRange(60f, 90f));
                Assert.That(level.Name, Is.Not.Empty);
                Assert.That(level.RadioMessage, Is.Not.Empty);
                Assert.That(level.BossName, Is.Not.Empty);
            }
        }

        [Test]
        public void CampaignUsesUniqueSeedsAndNames()
        {
            Assert.That(CampaignCatalog.All.Select(level => level.Seed).Distinct().Count(), Is.EqualTo(10));
            Assert.That(CampaignCatalog.All.Select(level => level.Name).Distinct().Count(), Is.EqualTo(10));
        }

        [Test]
        public void CampaignAuthorsVariedWavesCardPathsAndBossBehaviors()
        {
            Assert.That(CampaignCatalog.All.Select(level => level.Pattern).Distinct().Count(), Is.EqualTo(5));
            Assert.That(CampaignCatalog.All.Select(level => level.CardPath).Distinct().Count(), Is.EqualTo(4));
            Assert.That(CampaignCatalog.All.Select(level => level.BossBehavior).Distinct().Count(), Is.EqualTo(6));
        }

        [Test]
        public void CampaignUsesConfirmedEscapeJourneyAndVariedRoutes()
        {
            Assert.That(CampaignCatalog.Get(1).Name, Does.Contain("Wreck"));
            Assert.That(CampaignCatalog.Get(2).Name, Does.Contain("Crash"));
            Assert.That(CampaignCatalog.Get(3).Name, Does.Contain("Unknown"));
            Assert.That(CampaignCatalog.Get(4).Name, Does.Contain("Whiteglass"));
            Assert.That(CampaignCatalog.Get(10).Route, Is.EqualTo(RouteShape.ExtractionRing));
            Assert.That(CampaignCatalog.All.Select(level => level.Route).Distinct().Count(), Is.EqualTo(10));
            Assert.That(CampaignCatalog.All.Count(level => level.Route == RouteShape.CanyonBend
                || level.Route == RouteShape.UnknownWinding
                || level.Route == RouteShape.SnowSwitchback), Is.EqualTo(3));
        }

        [Test]
        public void CampaignRoutesHaveDistinctPortraitMapHeights()
        {
            float[] heights = CampaignCatalog.All.Select(level => CampaignCatalog.RouteMapY(level.Route)).ToArray();
            Assert.That(heights.Distinct().Count(), Is.EqualTo(10));
            Assert.That(heights.All(height => height >= 0.30f && height <= 0.68f), Is.True);
            Assert.That(CampaignCatalog.RouteMapY(RouteShape.UnknownWinding), Is.LessThan(CampaignCatalog.RouteMapY(RouteShape.CanyonBend)));
            Assert.That(CampaignCatalog.RouteMapY(RouteShape.ExtractionRing), Is.GreaterThan(CampaignCatalog.RouteMapY(RouteShape.HiveRun)));
        }

        [Test]
        public void EndlessDefinitionHasNoFixedDuration()
        {
            LevelDefinition endless = CampaignCatalog.CreateEndless(42);
            Assert.That(endless.Index, Is.Zero);
            Assert.That(endless.DurationSeconds, Is.Zero);
            Assert.That(endless.Seed, Is.EqualTo(42));
        }

        [Test]
        public void EndlessBossBehaviorsRotateDeterministically()
        {
            Assert.That(CampaignCatalog.GetEndlessBossBehavior(1), Is.EqualTo(BossBehavior.Barrager));
            Assert.That(CampaignCatalog.GetEndlessBossBehavior(6), Is.EqualTo(BossBehavior.Crusher));
            Assert.That(CampaignCatalog.GetEndlessBossBehavior(7), Is.EqualTo(BossBehavior.Barrager));
        }

        [Test]
        public void EveryCampaignRouteHasAPlayerFacingBrief()
        {
            foreach (LevelDefinition level in CampaignCatalog.All)
            {
                string brief = CampaignCatalog.RouteBrief(level.Route);
                Assert.That(brief, Is.Not.Empty);
                Assert.That(brief, Does.Contain("//"));
            }

            Assert.That(CampaignCatalog.RouteBrief(RouteShape.UnknownWinding), Does.Contain("WINDING"));
            Assert.That(CampaignCatalog.RouteBrief(RouteShape.SnowSwitchback), Does.Contain("SWITCHBACK"));
            Assert.That(CampaignCatalog.RouteBrief(RouteShape.ExtractionRing), Does.Contain("HOLD THE LOOP"));
        }

        [Test]
        public void EveryCampaignBiomeHasAUniquePlayerFacingBrief()
        {
            string[] briefs = CampaignCatalog.All.Select(level => CampaignCatalog.BiomeBrief(level.Biome)).ToArray();
            Assert.That(briefs.Distinct().Count(), Is.EqualTo(CampaignCatalog.All.Count));
            Assert.That(briefs.All(brief => brief.Contains("//")), Is.True);
            Assert.That(CampaignCatalog.BiomeBrief(BiomeId.Snowline), Does.Contain("ICE"));
            Assert.That(CampaignCatalog.BiomeBrief(BiomeId.HiveTrench), Does.Contain("BROOD"));
            Assert.That(CampaignCatalog.BiomeBrief(BiomeId.ExtractionRing), Does.Contain("RESCUE"));
        }

        [Test]
        public void EveryCampaignStageHasAnObjectiveAndRescueMilestone()
        {
            foreach (LevelDefinition level in CampaignCatalog.All)
            {
                Assert.That(CampaignCatalog.ObjectiveBrief(level), Is.Not.Empty);
                Assert.That(CampaignCatalog.MilestoneBrief(level), Is.Not.Empty);
                Assert.That(CampaignCatalog.ObjectiveBrief(level), Does.Contain("//"));
                Assert.That(CampaignCatalog.MilestoneBrief(level), Does.Contain("//"));
            }

            Assert.That(CampaignCatalog.ObjectiveBrief(CampaignCatalog.Get(1)), Does.Contain("WRECK"));
            Assert.That(CampaignCatalog.ObjectiveBrief(CampaignCatalog.Get(4)), Does.Contain("WHITEGLASS"));
            Assert.That(CampaignCatalog.MilestoneBrief(CampaignCatalog.Get(5)), Does.Contain("RELAY"));
            Assert.That(CampaignCatalog.MilestoneBrief(CampaignCatalog.Get(10)), Does.Contain("EXTRACTION"));
        }

        [Test]
        public void EveryCampaignStageHasAPathBeatAndBossBeat()
        {
            foreach (LevelDefinition level in CampaignCatalog.All)
            {
                Assert.That(CampaignCatalog.RouteBeat(level), Is.Not.Empty);
                Assert.That(CampaignCatalog.RouteBeat(level), Does.Contain("//"));
                Assert.That(CampaignCatalog.BossBrief(level), Does.Contain(level.BossName.ToUpperInvariant()));
            }

            Assert.That(CampaignCatalog.RouteBeat(CampaignCatalog.Get(3)), Does.Contain("SPORE"));
            Assert.That(CampaignCatalog.RouteBeat(CampaignCatalog.Get(4)), Does.Contain("SWITCHBACK"));
            Assert.That(CampaignCatalog.BossBrief(CampaignCatalog.Get(5)), Does.Contain("ARMORED"));
        }
    }
}
