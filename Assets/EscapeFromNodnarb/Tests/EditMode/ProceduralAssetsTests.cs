using NUnit.Framework;
using UnityEngine;

namespace EscapeFromNodnarb.Tests
{
    public sealed class ProceduralAssetsTests
    {
        [Test]
        public void ProceduralShaderIsAvailableFromResources()
        {
            Shader shader = Resources.Load<Shader>("NodnarbProceduralLit");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("EscapeFromNodnarb/ProceduralLit"));
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
            string[] names = { "CrashedEngine", "SnowArch", "RelayBeacon", "CanyonDebris", "CrystalCluster", "HiveGrowth", "RuinGate", "HiveObelisk", "ExtractionBeacon" };
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
