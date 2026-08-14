using NUnit.Framework;
using UnityEngine;

namespace EscapeFromNodnarb.Tests
{
    public sealed class ProceduralAssetsTests
    {
        [Test]
        public void VisualBudgetRejectsUnboundedRuntimeComposition()
        {
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(768, 64, 2, 1, 0), Is.True);
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(769, 64, 2, 1, 0), Is.False);
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(768, 65, 2, 1, 0), Is.False);
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(768, 64, 3, 1, 0), Is.False);
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(768, 64, 2, 2, 0), Is.False);
            Assert.That(NodnarbVisualBudget.IsWithinRuntimeBudget(768, 64, 2, 1, 1), Is.False);
        }

        [Test]
        public void VisualBudgetReportIncludesInspectableRuntimeCounters()
        {
            string report = NodnarbVisualBudget.FormatReport(12, 7, 2, 1, 0, 18, 3);

            Assert.That(report, Does.StartWith("NODNARB_VISUAL_BUDGET status=PASS"));
            Assert.That(report, Does.Contain("renderers=12/768"));
            Assert.That(report, Does.Contain("materials=7/64"));
            Assert.That(report, Does.Contain("projectiles=18/220"));
            Assert.That(report, Does.Contain("feedback=3/64"));
        }

        [Test]
        public void ProceduralMaterialsAreSharedAndInstanced()
        {
            Material first = PrimitiveFactory.Material(GameTheme.Signal);
            Material second = PrimitiveFactory.Material(GameTheme.Signal);

            Assert.That(first, Is.SameAs(second));
            Assert.That(first.enableInstancing, Is.True);
        }

        [Test]
        public void ProceduralShaderIsAvailableFromResources()
        {
            Shader shader = Resources.Load<Shader>("NodnarbProceduralLit");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("EscapeFromNodnarb/ProceduralLit"));
            Material material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_MainTex"), Is.True);
                Assert.That(material.HasProperty("_EmissionMap"), Is.True);
                Assert.That(material.HasProperty("_EmissionColor"), Is.True);
                Assert.That(material.HasProperty("_RimColor"), Is.True);
                Assert.That(material.HasProperty("_RimStrength"), Is.True);
                Assert.That(material.FindPass("ForwardBase"), Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GeneratedEnemyAndCrewPrefabsAreAvailableFromResources()
        {
            string[] names = { "Rusher", "Spitter", "Blocker", "Carrier", "CrewSoldier" };
            for (int index = 0; index < names.Length; index++)
            {
                GameObject prefab = Resources.Load<GameObject>("Enemies/" + names[index]);
                Assert.That(prefab, Is.Not.Null, names[index] + " resource missing");
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0), names[index] + " has no renderers");
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Debug.Log("NODNARB_ASSET_BOUNDS name=" + names[index] + " renderers=" + renderers.Length + " center=" + bounds.center + " size=" + bounds.size);
                Assert.That(bounds.size.y, Is.GreaterThan(0.2f), names[index] + " has an unreadable height");
            }
        }

        [Test]
        public void GeneratedWorldLandmarksAreAvailableFromResources()
        {
            string[] names = { "CrashedEngine", "SnowArch", "RelayBeacon", "CanyonDebris", "CrystalCluster", "HiveGrowth", "SporeArch", "RuinGate", "HiveObelisk", "ExtractionBeacon" };
            for (int index = 0; index < names.Length; index++)
            {
                GameObject prefab = Resources.Load<GameObject>("World/" + names[index]);
                Assert.That(prefab, Is.Not.Null, names[index] + " resource missing");
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0), names[index] + " has no renderers");
                Bounds bounds = renderers[0].bounds;
                for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }

                Debug.Log("NODNARB_LANDMARK_BOUNDS name=" + names[index] + " renderers=" + renderers.Length + " center=" + bounds.center + " size=" + bounds.size);
                Assert.That(bounds.size.y, Is.GreaterThan(0.4f), names[index] + " has an unreadable height");
            }
        }
    }
}
